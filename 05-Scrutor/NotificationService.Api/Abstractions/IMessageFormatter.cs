namespace NotificationService.Api.Abstractions;

public interface IMessageFormatter
{
    string Format { get; }
    string FormatMessage(string template, Dictionary<string, string> variables);
}
