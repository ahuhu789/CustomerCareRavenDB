using CustomerCareRavenDB.Controllers;
using CustomerCareRavenDB.Data;
using CustomerCareRavenDB.Models;
using CustomerCareRavenDB.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CustomerCareRavenDB.Tests;

/// <summary>
/// Id thuộc collection khác (vd. /Customers/Details?id=services/001) phải trả về 404 ở mọi
/// Details/Edit/Delete (GET và POST), và POST không được ghi đè hay xóa document của collection khác.
/// </summary>
public class ForeignIdTests : TestDatabase
{
    private async Task<T?> Load<T>(string id) where T : class
    {
        using var session = Context.OpenAsyncSession();
        return await session.LoadAsync<T>(id);
    }

    [Fact]
    public async Task LoadFromCollection_ReturnsOnlyDocumentsOfExpectedCollection()
    {
        using var session = Context.OpenAsyncSession();
        Assert.NotNull(await session.LoadFromCollectionAsync<Customer>("customers/001"));
        Assert.Null(await session.LoadFromCollectionAsync<Customer>("services/001"));
        Assert.Null(await session.LoadFromCollectionAsync<ServiceHistory>("feedbacks/001"));
        Assert.Null(await session.LoadFromCollectionAsync<Customer>("customers/999"));
        Assert.Null(await session.LoadFromCollectionAsync<Customer>(null));
        Assert.Null(await session.LoadFromCollectionAsync<Customer>("  "));
        Assert.NotNull(await session.LoadFromCollectionAsync<ServiceHistory>("servicehistories/001"));
    }

    [Fact]
    public async Task LoadFromCollection_EvictsForeignDocument_SoSaveChangesCannotOverwriteIt()
    {
        using (var session = Context.OpenAsyncSession())
        {
            var raw = await session.LoadAsync<Customer>("services/001");
            Assert.Null(await session.LoadFromCollectionAsync<Customer>("services/001"));
            raw.FullName = "overwritten";
            await session.SaveChangesAsync();
        }

        Assert.Equal("Bảo dưỡng và vệ sinh thiết bị toàn diện", (await Load<Service>("services/001"))!.Name);
    }

    [Fact]
    public async Task Customers_ForeignId_Returns404_AndDoesNotTouchForeignDocument()
    {
        var c = new CustomersController(Context) { TempData = TestHelpers.TempData() };
        const string foreign = "services/001";

        Assert.IsType<NotFoundResult>(await c.Details(foreign));
        Assert.IsType<NotFoundResult>(await c.Edit(foreign));
        Assert.IsType<NotFoundResult>(await c.Delete(foreign));
        Assert.IsType<NotFoundResult>(await c.Edit(foreign, new Customer { Id = foreign, FullName = "Hacked", Phone = "0900000000" }));
        Assert.IsType<NotFoundResult>(await c.DeleteConfirmed(foreign));

        var service = await Load<Service>(foreign);
        Assert.NotNull(service);
        Assert.Equal("Bảo dưỡng và vệ sinh thiết bị toàn diện", service.Name);

        // Id đúng collection vẫn hoạt động.
        Assert.IsType<ViewResult>(await c.Details("customers/001"));
    }

    [Fact]
    public async Task Appointments_ForeignId_Returns404_AndDoesNotTouchForeignDocument()
    {
        var c = new AppointmentsController(Context, Validator) { TempData = TestHelpers.TempData() };
        const string foreign = "customers/001";

        Assert.IsType<NotFoundResult>(await c.Details(foreign));
        Assert.IsType<NotFoundResult>(await c.Edit(foreign));
        Assert.IsType<NotFoundResult>(await c.Delete(foreign));
        Assert.IsType<NotFoundResult>(await c.Edit(foreign, new Appointment
        {
            Id = foreign, CustomerId = "customers/001", ServiceId = "services/001",
            AppointmentDate = DateTime.UtcNow, Status = "Pending"
        }));
        Assert.IsType<NotFoundResult>(await c.DeleteConfirmed(foreign));

        var customer = await Load<Customer>(foreign);
        Assert.NotNull(customer);
        Assert.Equal("Nguyễn Văn An", customer.FullName);
        Assert.Equal("Active", customer.Status); // DeleteConfirmed đặt Status = "Cancelled" nếu không chặn

        Assert.IsType<ViewResult>(await c.Details("appointments/001"));
    }

    [Fact]
    public async Task Appointments_ValidatorRejectsIdsFromWrongCollection()
    {
        using var session = Context.OpenAsyncSession();
        var result = await Validator.ValidateAppointmentAsync(session, new Appointment
        {
            CustomerId = "services/001", // không phải khách hàng
            ServiceId = "customers/001"  // không phải dịch vụ
        });
        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public async Task Services_ForeignId_Returns404_AndDoesNotTouchForeignDocument()
    {
        var c = new ServicesController(Context) { TempData = TestHelpers.TempData() };
        const string foreign = "customers/002";

        Assert.IsType<NotFoundResult>(await c.Details(foreign));
        Assert.IsType<NotFoundResult>(await c.Edit(foreign));
        Assert.IsType<NotFoundResult>(await c.Edit(foreign, new ServiceFormViewModel { Name = "Hacked", Category = "X", Price = 1 }));
        Assert.IsType<NotFoundResult>(await c.Delete(foreign));

        var customer = await Load<Customer>(foreign);
        Assert.NotNull(customer);
        Assert.Equal("Lê Thị Bích Ngọc", customer.FullName);

        Assert.IsType<ViewResult>(await c.Details("services/001"));
    }

    [Fact]
    public async Task ServiceHistories_ForeignId_Returns404_AndDoesNotTouchForeignDocument()
    {
        var c = new ServiceHistoriesController(Context, Validator) { TempData = TestHelpers.TempData() };
        const string foreign = "feedbacks/001";

        Assert.IsType<NotFoundResult>(await c.Details(foreign));
        Assert.IsType<NotFoundResult>(await c.Edit(foreign));
        Assert.IsType<NotFoundResult>(await c.Edit(foreign, new ServiceHistoryFormViewModel
        {
            AppointmentId = "appointments/001", StaffId = "staffs/001",
            ServiceDate = DateTime.UtcNow, Description = "Hacked"
        }));
        Assert.IsType<NotFoundResult>(await c.Delete(foreign));

        var feedback = await Load<Feedback>(foreign);
        Assert.NotNull(feedback);
        Assert.Equal(5, feedback.Rating);

        Assert.IsType<ViewResult>(await c.Details("servicehistories/001"));
    }

    [Fact]
    public async Task Feedbacks_ForeignId_Returns404_AndDoesNotTouchForeignDocument()
    {
        var c = new FeedbacksController(Context, Validator) { TempData = TestHelpers.TempData() };
        const string foreign = "appointments/001";

        Assert.IsType<NotFoundResult>(await c.Details(foreign));
        Assert.IsType<NotFoundResult>(await c.Edit(foreign));
        Assert.IsType<NotFoundResult>(await c.Edit(foreign, new FeedbackFormViewModel
        {
            AppointmentId = "appointments/001", Rating = 1, Comment = "Hacked"
        }));
        Assert.IsType<NotFoundResult>(await c.Delete(foreign));

        var appointment = await Load<Appointment>(foreign);
        Assert.NotNull(appointment);
        Assert.Equal("Completed", appointment.Status);

        Assert.IsType<ViewResult>(await c.Details("feedbacks/001"));
    }
}
