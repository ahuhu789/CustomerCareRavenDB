using CustomerCareRavenDB.Controllers;
using CustomerCareRavenDB.Models;
using Microsoft.AspNetCore.Mvc;

namespace CustomerCareRavenDB.Tests;

public class CustomerGenderTests
{
    [Theory]
    [InlineData("Male", "Male")]
    [InlineData("male", "Male")]
    [InlineData("Nam", "Male")]
    [InlineData(" nam ", "Male")]
    [InlineData("Female", "Female")]
    [InlineData("Nữ", "Female")]
    [InlineData("nu", "Female")]
    [InlineData("Other", "Other")]
    [InlineData("Khác", "Other")]
    [InlineData("", "")]
    [InlineData(null, "")]
    [InlineData("Unknown", "Unknown")]
    public void Normalize_MapsCanonicalAndLegacyValues(string? input, string expected)
        => Assert.Equal(expected, CustomerGender.Normalize(input));

    [Theory]
    [InlineData("Male", "Nam")]
    [InlineData("Nam", "Nam")]
    [InlineData("Female", "Nữ")]
    [InlineData("Nữ", "Nữ")]
    [InlineData("Other", "Khác")]
    [InlineData("", "Chưa cập nhật")]
    [InlineData("Unknown", "Chưa cập nhật")]
    public void ToDisplay_ReturnsVietnameseLabel(string? input, string expected)
        => Assert.Equal(expected, CustomerGender.ToDisplay(input));

    [Fact]
    public void Options_UseCanonicalValues()
        => Assert.Equal(["Male", "Female", "Other"], CustomerGender.Options.Select(o => o.Value));
}

public class CustomerGenderIntegrationTests : TestDatabase
{
    [Fact]
    public async Task Seeder_StoresCanonicalGenderValues()
    {
        using var session = Context.OpenAsyncSession();
        Assert.Equal(CustomerGender.Male, (await session.LoadAsync<Customer>("customers/001")).Gender);
        Assert.Equal(CustomerGender.Female, (await session.LoadAsync<Customer>("customers/002")).Gender);
    }

    [Fact]
    public async Task EditGet_NormalizesLegacyGender_SoFormPreselectsIt()
    {
        using (var session = Context.OpenAsyncSession())
        {
            (await session.LoadAsync<Customer>("customers/001")).Gender = "Nam"; // dữ liệu cũ
            await session.SaveChangesAsync();
        }

        var controller = new CustomersController(Context);
        var view = Assert.IsType<ViewResult>(await controller.Edit("customers/001"));
        Assert.Equal(CustomerGender.Male, Assert.IsType<Customer>(view.Model).Gender);

        // GET không được ghi đè dữ liệu.
        using var check = Context.OpenAsyncSession();
        Assert.Equal("Nam", (await check.LoadAsync<Customer>("customers/001")).Gender);
    }

    [Fact]
    public async Task EditPost_StoresCanonicalGender()
    {
        Customer input;
        using (var session = Context.OpenAsyncSession())
            input = await session.LoadAsync<Customer>("customers/002");
        input.Gender = "Nữ";

        var controller = new CustomersController(Context) { TempData = TestHelpers.TempData() };
        Assert.IsType<RedirectToActionResult>(await controller.Edit(input.Id, input));

        using var check = Context.OpenAsyncSession();
        Assert.Equal(CustomerGender.Female, (await check.LoadAsync<Customer>("customers/002")).Gender);
    }
}
