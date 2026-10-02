namespace DynamicRuleEngine.Api.Domain.Interfaces;

/// <summary>
/// Trình biên dịch chuỗi biểu thức logic thành Delegate thực thi trực tiếp trên RAM.
/// </summary>
public interface IRuleCompiler
{
    /// <summary>
    /// Biên dịch biểu thức chuỗi thành Func&lt;T, bool&gt; có kiểu dữ liệu mạnh mẽ.
    /// Có cache tự động để tránh biên dịch lặp lại.
    /// </summary>
    Func<T, bool> Compile<T>(string expression);

    /// <summary>
    /// Kiểm tra tính hợp lệ về cú pháp của biểu thức trước khi lưu.
    /// </summary>
    bool ValidateExpression<T>(string expression, out string? errorMessage);

    /// <summary>
    /// Xóa cache của một biểu thức hoặc xóa toàn bộ cache khi rule được cập nhật.
    /// </summary>
    void InvalidateCache(string? expression = null);
}
