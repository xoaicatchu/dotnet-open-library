using NotificationService.Api.Abstractions;

namespace NotificationService.Api.Services;

public class HtmlFormatter : IMessageFormatter
{
    public string Format => "html";

    public string FormatMessage(string template, Dictionary<string, string> variables)
    {
        var result = template;
        foreach (var (key, value) in variables)
        {
            result = result.Replace($"{{{key}}}", $"<b>{value}</b>");
        }
        return $"<p>{result}</p>";
    }
}
