namespace CustomerCareRavenDB.Models;

public class DashboardViewModel
{
    public bool IsConnected { get; set; }
    public string DatabaseName { get; set; } = string.Empty;
    public string RavenUrl { get; set; } = string.Empty;
    public string StudioUrl { get; set; } = string.Empty;
    public string ServerVersion { get; set; } = string.Empty;

    public int CustomersCount { get; set; }
    public int StaffsCount { get; set; }
    public int ServicesCount { get; set; }
    public int AppointmentsCount { get; set; }
    public int ServiceHistoriesCount { get; set; }
    public int FeedbacksCount { get; set; }

    public List<Customer> RecentCustomers { get; set; } = [];
    public List<Appointment> RecentAppointments { get; set; } = [];
    public List<ServiceHistory> RecentHistories { get; set; } = [];
    public List<Feedback> RecentFeedbacks { get; set; } = [];

    public string? StatusMessage { get; set; }
}
