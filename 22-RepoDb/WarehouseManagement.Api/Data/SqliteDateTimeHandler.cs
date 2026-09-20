using System.Globalization;
using RepoDb;
using RepoDb.Interfaces;
using RepoDb.Options;

namespace WarehouseManagement.Api.Data;

public class SqliteDateTimeHandler : IPropertyHandler<string, DateTime>
{
    public DateTime Get(string input, PropertyHandlerGetOptions options)
    {
        if (DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
        {
            return dt;
        }
        return DateTime.Parse(input);
    }

    public string Set(DateTime input, PropertyHandlerSetOptions options)
    {
        return input.ToString("o");
    }
}
