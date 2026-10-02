using DynamicRuleEngine.Api.Domain.UseCases;
using DynamicRuleEngine.Api.Infrastructure.Compilers;
using DynamicRuleEngine.Api.Infrastructure.Repositories;
using DynamicRuleEngine.Api.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace DynamicRuleEngine.Tests;

public class MedicalInsuranceRulesTests
{
    private readonly RuleEvaluatorService _evaluator;

    public MedicalInsuranceRulesTests()
    {
        var repo = new InMemoryWorkflowRepository();
        var compiler = new ExpressionRuleCompiler();
        var logger = NullLogger<RuleEvaluatorService>.Instance;
        _evaluator = new RuleEvaluatorService(repo, compiler, logger);
    }

    [Fact]
    public async Task Evaluate_ChildUnder6_ShouldReceive100PercentCoverage_AndStopFurtherRules()
    {
        // Arrange: Bệnh nhân 4 tuổi, đi trái tuyến
        var child = new MedicalInsuranceContext
        {
            PatientId = "BN-001",
            PatientName = "Nguyễn Văn Bé",
            Age = 4,
            IsRightRoute = false,
            InsuranceCardCode = "TE1",
            DiagnosisCode = "J00",
            TotalMedicalBill = 500000
        };

        // Act
        var result = await _evaluator.EvaluateWorkflowAsync("medical-insurance-approval", child);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(100, Convert.ToInt32(result.Outputs["CoveragePercent"]));
        Assert.Equal("Approved", result.Outputs["ApprovalStatus"]?.ToString());

        // Nhờ cờ StopOnSuccess, chỉ có đúng 1 rule được duyệt qua
        Assert.Equal(1, result.ExecutedRulesCount);
        Assert.Equal(1, result.PassedRulesCount);
    }

    [Fact]
    public async Task Evaluate_ElderlyChronicPatient_RightRoute_ShouldReceive95PercentCoverage()
    {
        // Arrange: Cụ ông 70 tuổi, tăng huyết áp (I10), khám đúng tuyến
        var patient = new MedicalInsuranceContext
        {
            PatientId = "BN-002",
            PatientName = "Trần Văn Cụ",
            Age = 70,
            IsRightRoute = true,
            HasChronicDisease = true,
            DiagnosisCode = "I10",
            InsuranceCardCode = "GD4",
            TotalMedicalBill = 3500000
        };

        // Act
        var result = await _evaluator.EvaluateWorkflowAsync("medical-insurance-approval", patient);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(95, Convert.ToInt32(result.Outputs["CoveragePercent"]));
        Assert.True(Convert.ToBoolean(result.Outputs["IsPriorityCare"]));
        Assert.Equal(2, result.ExecutedRulesCount); // Bỏ qua rule 1 (trẻ em), khớp rule 2 rồi dừng
    }

    [Fact]
    public async Task Evaluate_StandardPatient_RightRoute_ShouldReceive80PercentCoverage()
    {
        // Arrange: Người lớn 35 tuổi, khám đúng tuyến
        var patient = new MedicalInsuranceContext
        {
            PatientId = "BN-003",
            PatientName = "Lê Thị Lan",
            Age = 35,
            IsRightRoute = true,
            HasChronicDisease = false,
            InsuranceCardCode = "DN4",
            TotalMedicalBill = 800000
        };

        // Act
        var result = await _evaluator.EvaluateWorkflowAsync("medical-insurance-approval", patient);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(80, Convert.ToInt32(result.Outputs["CoveragePercent"]));
        Assert.Equal(3, result.ExecutedRulesCount);
    }

    [Fact]
    public async Task Evaluate_Patient_WrongRoute_ShouldReceive40PercentCoverage()
    {
        // Arrange: Người lớn 35 tuổi, tự đi khám trái tuyến
        var patient = new MedicalInsuranceContext
        {
            PatientId = "BN-004",
            PatientName = "Hoàng Văn Tuấn",
            Age = 35,
            IsRightRoute = false,
            HasChronicDisease = false,
            InsuranceCardCode = "DN4",
            TotalMedicalBill = 2000000
        };

        // Act
        var result = await _evaluator.EvaluateWorkflowAsync("medical-insurance-approval", patient);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(40, Convert.ToInt32(result.Outputs["CoveragePercent"]));
        Assert.Equal("ApprovedWithCondition", result.Outputs["ApprovalStatus"]?.ToString());
        Assert.Equal(4, result.ExecutedRulesCount);
    }

    [Fact]
    public async Task Evaluate_BenchmarkExecutionTime_ShouldBeUnder1Millisecond()
    {
        // Arrange: Bệnh nhân test benchmark
        var patient = new MedicalInsuranceContext
        {
            PatientId = "BN-BENCH",
            Age = 45,
            IsRightRoute = true,
            HasChronicDisease = true
        };

        // Warm up cache
        await _evaluator.EvaluateWorkflowAsync("medical-insurance-approval", patient);

        // Act
        var result = await _evaluator.EvaluateWorkflowAsync("medical-insurance-approval", patient);

        // Assert: Thời gian thực thi toàn bộ workflow phải < 1000 microsecond (1ms)
        Assert.True(result.TotalExecutionTimeUs < 1000, 
            $"Thời gian chạy thực tế: {result.TotalExecutionTimeUs} us (kỳ vọng < 1000 us)");
    }
}
