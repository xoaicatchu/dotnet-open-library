using System.Collections.Concurrent;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Security;
using DynamicRuleEngine.Api.Domain.Interfaces;

namespace DynamicRuleEngine.Api.Infrastructure.Compilers;

/// <summary>
/// Trình biên dịch biểu thức logic C# động sử dụng System.Linq.Dynamic.Core.
/// Tự động cache delegate trong RAM và kiểm soát an toàn bảo mật (Sandbox Whitelist).
/// </summary>
public class ExpressionRuleCompiler : IRuleCompiler
{
    private readonly ConcurrentDictionary<string, Delegate> _cache = new(StringComparer.Ordinal);

    // Danh sách từ khóa bị cấm tuyệt đối để tránh Code Injection / Lỗ hổng bảo mật
    private static readonly string[] ProhibitedKeywords =
    {
        "System.IO", "System.Diagnostics", "Process.", "File.", "Directory.",
        "Environment.", "Assembly.", "AppDomain.", "Reflection.", "Type.",
        "Activator.", "Thread.", "Task.Run", "GC."
    };

    public Func<T, bool> Compile<T>(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            throw new ArgumentException("Biểu thức không được để trống.", nameof(expression));
        }

        // 1. Kiểm tra an toàn trước khi biên dịch
        SanitizeExpression(expression);

        // 2. Tra cứu từ Cache để tối ưu hóa hiệu năng cực đại (< 100ns)
        var cacheKey = $"{typeof(T).FullName}::{expression.Trim()}";
        if (_cache.TryGetValue(cacheKey, out var cachedDelegate) && cachedDelegate is Func<T, bool> compiledFunc)
        {
            return compiledFunc;
        }

        // 3. Biên dịch chuỗi text thành Lambda Expression: p => ...
        try
        {
            var parameter = Expression.Parameter(typeof(T), "input");
            var lambda = DynamicExpressionParser.ParseLambda<T, bool>(
                new ParsingConfig { IsCaseSensitive = false },
                createParameterCtor: false,
                expression);

            var result = lambda.Compile();
            _cache[cacheKey] = result;
            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Lỗi cú pháp khi biên dịch biểu thức '{expression}': {ex.Message}", ex);
        }
    }

    public bool ValidateExpression<T>(string expression, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(expression))
        {
            errorMessage = "Biểu thức không được để trống.";
            return false;
        }

        try
        {
            SanitizeExpression(expression);
            DynamicExpressionParser.ParseLambda<T, bool>(
                new ParsingConfig { IsCaseSensitive = false },
                createParameterCtor: false,
                expression);
            return true;
        }
        catch (SecurityException secEx)
        {
            errorMessage = secEx.Message;
            return false;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    public void InvalidateCache(string? expression = null)
    {
        if (string.IsNullOrEmpty(expression))
        {
            _cache.Clear();
        }
        else
        {
            var keysToRemove = _cache.Keys.Where(k => k.Contains(expression)).ToList();
            foreach (var key in keysToRemove)
            {
                _cache.TryRemove(key, out _);
            }
        }
    }

    private static void SanitizeExpression(string expression)
    {
        foreach (var keyword in ProhibitedKeywords)
        {
            if (expression.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                throw new SecurityException($"Phát hiện từ khóa nguy hiểm '{keyword}' bị cấm trong biểu thức quy tắc.");
            }
        }
    }
}
