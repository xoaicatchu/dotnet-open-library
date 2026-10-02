using System.Security;
using DynamicRuleEngine.Api.Domain.UseCases;
using DynamicRuleEngine.Api.Infrastructure.Compilers;

namespace DynamicRuleEngine.Tests;

public class RuleCompilerTests
{
    private readonly ExpressionRuleCompiler _compiler = new();

    [Fact]
    public void Compile_ValidNumericComparison_ShouldEvaluateCorrectly()
    {
        // Arrange
        var expression = "Age >= 60 and TotalMedicalBill > 1000000";
        var patientOld = new MedicalInsuranceContext { Age = 65, TotalMedicalBill = 2000000 };
        var patientYoung = new MedicalInsuranceContext { Age = 30, TotalMedicalBill = 2000000 };

        // Act
        var func = _compiler.Compile<MedicalInsuranceContext>(expression);

        // Assert
        Assert.True(func(patientOld));
        Assert.False(func(patientYoung));
    }

    [Fact]
    public void Compile_StringFunctions_ShouldEvaluateCorrectly()
    {
        // Arrange
        var expression = "DiagnosisCode.StartsWith(\"I10\") or InsuranceCardCode == \"TE1\"";
        var patient1 = new MedicalInsuranceContext { DiagnosisCode = "I10.9", InsuranceCardCode = "DN4" };
        var patient2 = new MedicalInsuranceContext { DiagnosisCode = "K29.0", InsuranceCardCode = "TE1" };
        var patient3 = new MedicalInsuranceContext { DiagnosisCode = "K29.0", InsuranceCardCode = "DN4" };

        // Act
        var func = _compiler.Compile<MedicalInsuranceContext>(expression);

        // Assert
        Assert.True(func(patient1));
        Assert.True(func(patient2));
        Assert.False(func(patient3));
    }

    [Fact]
    public void Compile_ProhibitedKeyword_ShouldThrowSecurityException()
    {
        // Arrange: Cố tình tiêm System.IO hoặc Process
        var maliciousExpression1 = "System.IO.File.Exists(\"C:\\test.txt\")";
        var maliciousExpression2 = "Process.Start(\"calc.exe\") != null";

        // Act & Assert
        Assert.Throws<SecurityException>(() => _compiler.Compile<MedicalInsuranceContext>(maliciousExpression1));
        Assert.Throws<SecurityException>(() => _compiler.Compile<MedicalInsuranceContext>(maliciousExpression2));
    }

    [Fact]
    public void ValidateExpression_InvalidSyntax_ShouldReturnFalseWithErrorMessage()
    {
        // Arrange
        var invalidSyntax = "Age >>> 50 and and";

        // Act
        var isValid = _compiler.ValidateExpression<MedicalInsuranceContext>(invalidSyntax, out var errorMessage);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
    }

    [Fact]
    public void Compile_SecondCall_ShouldHitCacheWithoutRecompiling()
    {
        // Arrange
        var expression = "Age <= 6";

        // Act
        var func1 = _compiler.Compile<MedicalInsuranceContext>(expression);
        var func2 = _compiler.Compile<MedicalInsuranceContext>(expression);

        // Assert: Cùng tham chiếu delegate từ RAM Cache
        Assert.Same(func1, func2);
    }
}
