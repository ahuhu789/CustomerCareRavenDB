# syntax=docker/dockerfile:1
# ==============================================================================
# Image "tất cả trong một" cho Render Free: RavenDB Server + ứng dụng ASP.NET Core
# trong CÙNG một container.
#   - RavenDB chạy không bảo mật nhưng CHỈ bind 127.0.0.1:8080 (không EXPOSE, không công khai).
#   - App lắng nghe 0.0.0.0:$PORT (Render đặt PORT=10000) và kết nối http://127.0.0.1:8080.
#   - Không có persistent disk: dữ liệu mất khi restart/redeploy/spin-down; app tự seed lại mỗi lần khởi động.
#
# Build: docker build -t customercare-ravendb .
# Run:   docker run --rm -p 10000:10000 --memory=512m customercare-ravendb
#        -> http://localhost:10000  (health check: /healthz)
# Dùng RavenDB bên ngoài thay cho RavenDB nhúng:
#        docker run ... -e EMBEDDED_RAVENDB=false -e RavenSettings__Urls__0=https://... \
#                       -e RavenSettings__CertificateBase64=... -e RavenSettings__StudioEnabled=true ...
# ==============================================================================

ARG RAVENDB_IMAGE=ravendb/ravendb:7.2.7-ubuntu.24.04-x64

# ---------- RavenDB Server (bản self-contained, lấy từ image chính thức) ----------
FROM ${RAVENDB_IMAGE} AS ravendb

# ---------- Build ứng dụng ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY CustomerCareRavenDB/CustomerCareRavenDB.csproj CustomerCareRavenDB/
RUN dotnet restore CustomerCareRavenDB/CustomerCareRavenDB.csproj
COPY CustomerCareRavenDB/ CustomerCareRavenDB/
RUN dotnet publish CustomerCareRavenDB/CustomerCareRavenDB.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- Runtime (Ubuntu 24.04, cùng hệ điều hành với image RavenDB) ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# RavenDB Server (đã kèm .NET runtime riêng) - chỉ đọc, thuộc root.
COPY --from=ravendb /usr/lib/ravendb/server /opt/ravendb/server

# Thư mục dữ liệu RavenDB (tạm thời) thuộc user không phải root của image (app, UID $APP_UID).
RUN mkdir -p /var/lib/ravendb-data && chown -R $APP_UID:$APP_UID /var/lib/ravendb-data

WORKDIR /app
COPY --from=build /app/publish .
COPY --chmod=755 docker/entrypoint.sh /usr/local/bin/entrypoint.sh

# ----- Ứng dụng -----
# PORT: Render tự đặt (mặc định 10000). Không dùng 8080 vì cổng này dành cho RavenDB nội bộ.
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    ASPNETCORE_HTTP_PORTS=10000 \
    PORT=10000 \
    DOTNET_NOLOGO=true \
    RavenSettings__Urls__0=http://127.0.0.1:8080 \
    RavenSettings__DatabaseName=CustomerCareDB \
    RavenSettings__StudioEnabled=false \
    Seed__OnStartup=true

# ----- RavenDB nhúng (khóa cấu hình RavenDB dạng RAVEN_<Section>_<Key>) -----
# Chỉ nghe trên loopback; truy cập không bảo mật chỉ cho phép từ local.
# Ngưỡng bộ nhớ hạ thấp cho máy 512 MB.
ENV EMBEDDED_RAVENDB=true \
    RAVEN_WAIT_SECONDS=300 \
    RAVEN_Setup_Mode=None \
    RAVEN_License_Eula_Accepted=true \
    RAVEN_ServerUrl=http://127.0.0.1:8080 \
    RAVEN_ServerUrl_Tcp=tcp://127.0.0.1:38888 \
    RAVEN_Security_UnsecuredAccessAllowed=Local \
    RAVEN_DataDir=/var/lib/ravendb-data/data \
    RAVEN_Logs_Path=/var/lib/ravendb-data/logs \
    RAVEN_Security_AuditLog_FolderPath=/var/lib/ravendb-data/audit \
    RAVEN_Indexing_NugetPackagesPath=/var/lib/ravendb-data/packages/nuget/indexing \
    RAVEN_Logs_NuGet_PackagesPath=/var/lib/ravendb-data/packages/nuget/logging \
    RAVEN_Memory_LowMemoryLimitInMb=64 \
    RAVEN_Memory_LowMemoryCommitLimitInMb=64

USER $APP_UID
EXPOSE 10000
ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]
