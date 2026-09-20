namespace JobScheduler.Api.Services;

public interface IEmailService
{
    Task SendWelcomeEmail(string email, string name);
    Task SendReminderEmail(string email, string message);
}
