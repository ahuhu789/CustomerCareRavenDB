using System.Text.Json;
using CustomerCareRavenDB.Models;

namespace CustomerCareRavenDB.Tests;

public class ModelTests
{
    [Fact]
    public void Customer_WithEmbeddedAddress_SerializesCorrectly()
    {
        // Arrange
        var customer = new Customer
        {
            Id = "customers/001",
            FullName = "Nguyễn Văn An",
            Phone = "0901234567",
            Email = "nguyenvanan@example.com",
            Address = new CustomerAddress
            {
                Street = "123 Nguyễn Huệ",
                Ward = "Bến Nghé",
                District = "Quận 1",
                City = "TP. Hồ Chí Minh"
            },
            Birthday = new DateTime(1990, 5, 15, 0, 0, 0, DateTimeKind.Utc),
            Gender = "Nam",
            CreatedAt = DateTime.UtcNow,
            Status = "Active"
        };

        // Act
        var json = JsonSerializer.Serialize(customer);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Assert: Address is an embedded object, not a string ID reference
        Assert.True(root.TryGetProperty("Address", out var addressProp));
        Assert.Equal(JsonValueKind.Object, addressProp.ValueKind);
        Assert.Equal("123 Nguyễn Huệ", addressProp.GetProperty("Street").GetString());
        Assert.Equal("Bến Nghé", addressProp.GetProperty("Ward").GetString());
        Assert.Equal("Quận 1", addressProp.GetProperty("District").GetString());
        Assert.Equal("TP. Hồ Chí Minh", addressProp.GetProperty("City").GetString());

        // Check other fields
        Assert.Equal("customers/001", root.GetProperty("Id").GetString());
        Assert.Equal("Nguyễn Văn An", root.GetProperty("FullName").GetString());
        Assert.Equal("0901234567", root.GetProperty("Phone").GetString());
        Assert.Equal("Active", root.GetProperty("Status").GetString());
    }

    [Fact]
    public void DocumentReferences_StoreStringIds_NotEmbeddedObjects()
    {
        // Arrange
        var appointment = new Appointment
        {
            Id = "appointments/001",
            CustomerId = "customers/001",
            ServiceId = "services/001",
            AppointmentDate = DateTime.UtcNow.AddDays(1),
            Status = "Pending",
            Note = "Kiểm tra hệ thống tản nhiệt",
            CreatedAt = DateTime.UtcNow
        };

        var history = new ServiceHistory
        {
            Id = "servicehistories/001",
            AppointmentId = "appointments/001",
            CustomerId = "customers/001",
            ServiceId = "services/001",
            StaffId = "staffs/001",
            ServiceDate = DateTime.UtcNow,
            Description = "Bảo dưỡng chi tiết quạt tản nhiệt",
            Result = "Đạt tiêu chuẩn",
            Note = "Bàn giao khách hàng",
            CreatedAt = DateTime.UtcNow
        };

        var feedback = new Feedback
        {
            Id = "feedbacks/001",
            CustomerId = "customers/001",
            AppointmentId = "appointments/001",
            Rating = 5,
            Comment = "Rất hài lòng",
            CreatedAt = DateTime.UtcNow
        };

        // Act & Assert for Appointment
        var appointmentJson = JsonSerializer.Serialize(appointment);
        using var appDoc = JsonDocument.Parse(appointmentJson);
        Assert.Equal("customers/001", appDoc.RootElement.GetProperty("CustomerId").GetString());
        Assert.Equal("services/001", appDoc.RootElement.GetProperty("ServiceId").GetString());
        Assert.True(appDoc.RootElement.TryGetProperty("Note", out var noteProp)); // Exact field 'Note'
        Assert.Equal("Kiểm tra hệ thống tản nhiệt", noteProp.GetString());

        // Act & Assert for ServiceHistory
        var historyJson = JsonSerializer.Serialize(history);
        using var histDoc = JsonDocument.Parse(historyJson);
        Assert.Equal("appointments/001", histDoc.RootElement.GetProperty("AppointmentId").GetString());
        Assert.Equal("customers/001", histDoc.RootElement.GetProperty("CustomerId").GetString());
        Assert.Equal("services/001", histDoc.RootElement.GetProperty("ServiceId").GetString());
        Assert.Equal("staffs/001", histDoc.RootElement.GetProperty("StaffId").GetString());
        Assert.True(histDoc.RootElement.TryGetProperty("ServiceDate", out _)); // Exact field 'ServiceDate'

        // Act & Assert for Feedback
        var feedbackJson = JsonSerializer.Serialize(feedback);
        using var fbDoc = JsonDocument.Parse(feedbackJson);
        Assert.Equal("customers/001", fbDoc.RootElement.GetProperty("CustomerId").GetString());
        Assert.Equal("appointments/001", fbDoc.RootElement.GetProperty("AppointmentId").GetString()); // Must reference AppointmentId
        Assert.Equal(5, fbDoc.RootElement.GetProperty("Rating").GetInt32());
    }

    [Fact]
    public void Staff_And_Service_Properties_AreConsistent()
    {
        var staff = new Staff
        {
            Id = "staffs/001",
            FullName = "Trần Thị Mai",
            Position = "Kỹ thuật viên cao cấp",
            Phone = "0912345678",
            Email = "mai@centercare.vn",
            CreatedAt = DateTime.UtcNow
        };

        var service = new Service
        {
            Id = "services/001",
            Name = "Vệ sinh thiết bị",
            Description = "Vệ sinh toàn diện",
            Price = 350000m,
            Category = "Bảo dưỡng",
            CreatedAt = DateTime.UtcNow
        };

        Assert.Equal("staffs/001", staff.Id);
        Assert.Equal("services/001", service.Id);
        Assert.Equal(350000m, service.Price);
    }
}
