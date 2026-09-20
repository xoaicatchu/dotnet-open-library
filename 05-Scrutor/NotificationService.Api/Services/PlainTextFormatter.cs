using NotificationService.Api.Abstractions;

namespace NotificationService.Api.Services;

public class PlainTextFormatter : IMessageFormatter
{
    public string Format => "plain";

    public string FormatMessage(string template, Dictionary<string, string> variables)
    {
        var result = template;
        foreach (var (key, value) in variables)
        {
            result = result.Replace($"{{{key}}}", value);
        }
        return result;
    }
}
