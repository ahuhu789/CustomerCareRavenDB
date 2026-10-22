#!/bin/bash
# Khởi động RavenDB nhúng (nếu EMBEDDED_RAVENDB=true), chờ sẵn sàng, rồi chạy ứng dụng.
# Một trong hai tiến trình dừng -> dừng tiến trình còn lại và thoát để Render khởi động lại container.
set -uo pipefail

log() { echo "[entrypoint] $*"; }

pids=()
shutdown() {
    log "Nhận tín hiệu dừng, đang tắt các tiến trình..."
    [ ${#pids[@]} -gt 0 ] && kill -TERM "${pids[@]}" 2>/dev/null
}
trap shutdown TERM INT

# Kiểm tra RavenDB bằng HTTP GET /build/version qua /dev/tcp (image runtime không có curl).
raven_ready() {
    local hostport="${RAVEN_ServerUrl#http://}"
    local host="${hostport%%:*}" port="${hostport##*:}"
    { exec 3<>"/dev/tcp/${host}/${port}"; } 2>/dev/null || return 1
    printf 'GET /build/version HTTP/1.0\r\nHost: %s\r\n\r\n' "$host" >&3
    local status
    IFS= read -r -t 5 status <&3 || status=""
    exec 3<&- 3>&-
    [[ "$status" == *" 200"* ]]
}

if [ "${EMBEDDED_RAVENDB:-true}" = "true" ]; then
    mkdir -p "$RAVEN_DataDir" "$RAVEN_Logs_Path"
    # RavenDB cần file settings.json ghi được; thư mục cài đặt /opt/ravendb là chỉ đọc.
    raven_settings="/var/lib/ravendb-data/settings.json"
    [ -f "$raven_settings" ] || echo '{}' > "$raven_settings"
    log "Khởi động RavenDB nhúng tại ${RAVEN_ServerUrl} (chỉ loopback)..."
    # Workstation GC cho RavenDB để tiết kiệm RAM (máy 512 MB, 0.1 CPU).
    DOTNET_gcServer=0 \
        /opt/ravendb/server/Raven.Server --non-interactive -c "$raven_settings" </dev/null &
    raven_pid=$!
    pids+=("$raven_pid")

    waited=0
    until raven_ready; do
        if ! kill -0 "$raven_pid" 2>/dev/null; then
            log "RavenDB đã thoát trước khi sẵn sàng."
            wait "$raven_pid"; exit 1
        fi
        if [ "$waited" -ge "${RAVEN_WAIT_SECONDS:-300}" ]; then
            log "RavenDB chưa sẵn sàng sau ${waited}s, dừng."
            kill -TERM "$raven_pid" 2>/dev/null; exit 1
        fi
        sleep 1; waited=$((waited + 1))
    done
    log "RavenDB sẵn sàng sau ~${waited}s."
else
    log "EMBEDDED_RAVENDB=false: dùng RavenDB bên ngoài (${RavenSettings__Urls__0:-chưa cấu hình})."
fi

log "Khởi động ứng dụng trên cổng ${PORT:-10000}..."
DOTNET_gcServer=0 dotnet /app/CustomerCareRavenDB.dll &
pids+=("$!")

# Chờ tiến trình đầu tiên thoát (hoặc tín hiệu dừng), rồi dừng tiến trình còn lại.
wait -n
code=$?
shutdown
wait
log "Đã dừng (exit code ${code})."
exit "$code"
