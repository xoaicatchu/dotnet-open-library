using System.Data;
using System.Globalization;
using Dapper;

namespace ProductCatalog.Api.Data;

public class SqliteDecimalHandler : SqlMapper.TypeHandler<decimal>
{
    public override void SetValue(IDbDataParameter parameter, decimal value)
    {
        parameter.Value = value;
    }

    public override decimal Parse(object value)
    {
        return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
    }
}
