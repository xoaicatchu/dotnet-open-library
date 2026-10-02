namespace StockFlow.Api.Common;

public sealed class StockFlowOperationException(int statusCode, string code, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Code { get; } = code;
}
