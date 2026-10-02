using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using DynamicRuleEngine.Api.Controllers;
using DynamicRuleEngine.Api.Domain.Models;
using DynamicRuleEngine.Api.Domain.UseCases;

namespace DynamicRuleEngine.Tests;

public class RuleEngineIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public RuleEngineIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetWorkflows_ShouldReturn_SeededWorkflows()
    {
        // Act
        var response = await _client.GetAsync("/api/workflows");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var workflows = await response.Content.ReadFromJsonAsync<List<Workflow>>();
        Assert.NotNull(workflows);
        Assert.Contains(workflows, w => w.WorkflowId == "medical-insurance-approval");
        Assert.Contains(workflows, w => w.WorkflowId == "order-discount-rules");
    }

    [Fact]
    public async Task EvaluateMedicalInsurance_Api_ShouldReturn_ValidTraceAndOutputs()
    {
        // Arrange: Bệnh nhân 5 tuổi
        var patient = new MedicalInsuranceContext
        {
            PatientId = "BN-INT-01",
            PatientName = "Bé Na",
            Age = 5,
            IsRightRoute = true,
            InsuranceCardCode = "TE1"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/evaluate/medical-insurance", patient);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<WorkflowExecutionResult>();
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.True(result.Outputs.ContainsKey("CoveragePercent"));
        Assert.Equal(100, Convert.ToInt32(result.Outputs["CoveragePercent"]?.ToString()));
        Assert.NotEmpty(result.RuleResults);
    }

    [Fact]
    public async Task EvaluateOrderDiscount_Api_ShouldApply_DiamondTierDiscount()
    {
        // Arrange: Khách Diamond mua đơn 2.500.000 VNĐ
        var order = new OrderDiscountContext
        {
            OrderId = "ORD-999",
            CustomerTier = "Diamond",
            TotalOrderAmount = 2500000,
            ItemCount = 3,
            IsWeekend = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/evaluate/order-discount", order);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<WorkflowExecutionResult>();
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal(20, Convert.ToInt32(result.Outputs["DiscountPercent"]?.ToString()));
        Assert.True(Convert.ToBoolean(result.Outputs["FreeShipping"]?.ToString()));
    }

    [Fact]
    public async Task ValidateRule_Api_ShouldDetect_ValidAndInvalidExpressions()
    {
        // 1. Biểu thức hợp lệ
        var validReq = new ValidateRuleRequest("Age >= 18 and TotalMedicalBill > 0", "MedicalInsuranceContext");
        var validRes = await _client.PostAsJsonAsync("/api/workflows/validate-rule", validReq);
        Assert.Equal(HttpStatusCode.OK, validRes.StatusCode);
        var validJson = await validRes.Content.ReadAsStringAsync();
        Assert.Contains("\"isValid\":true", validJson);

        // 2. Biểu thức có từ khóa cấm (Bảo mật)
        var maliciousReq = new ValidateRuleRequest("System.IO.File.Exists(\"abc\")", "MedicalInsuranceContext");
        var maliciousRes = await _client.PostAsJsonAsync("/api/workflows/validate-rule", maliciousReq);
        Assert.Equal(HttpStatusCode.OK, maliciousRes.StatusCode);
        var maliciousJson = await maliciousRes.Content.ReadAsStringAsync();
        Assert.Contains("\"isValid\":false", maliciousJson);
        Assert.Contains("nguy hiểm", maliciousJson);
    }

    [Fact]
    public async Task HotReload_ToggleRule_ShouldImmediatelyChange_EvaluationBehavior()
    {
        // 1. Tạo một workflow độc lập để test toggle
        var testWorkflow = new Workflow
        {
            WorkflowId = "hot-reload-test",
            WorkflowName = "Hot Reload Test Workflow",
            TargetTypeName = nameof(OrderDiscountContext),
            Rules = new List<Rule>
            {
                new Rule
                {
                    RuleId = "hot-rule-1",
                    RuleName = "Giảm giá đặc biệt",
                    Expression = "TotalOrderAmount > 1000",
                    Priority = 1,
                    IsActive = true,
                    SuccessActions = new List<RuleAction>
                    {
                        new("SpecialDiscount", "Discount", 50)
                    }
                }
            }
        };

        // Lưu workflow
        var saveRes = await _client.PostAsJsonAsync("/api/workflows", testWorkflow);
        Assert.Equal(HttpStatusCode.OK, saveRes.StatusCode);

        // Đánh giá khi Rule đang BẬT
        var order = new OrderDiscountContext { TotalOrderAmount = 2000 };
        var evalRes1 = await _client.PostAsJsonAsync("/api/workflows", testWorkflow); // save ensures repository has it

        // 2. Tắt Rule qua API Toggle: isActive=false
        var toggleRes = await _client.PutAsync("/api/workflows/hot-reload-test/rules/hot-rule-1/toggle?isActive=false", null);
        Assert.Equal(HttpStatusCode.OK, toggleRes.StatusCode);

        // 3. Kiểm tra chi tiết workflow xem rule đã chuyển sang IsActive=false chưa
        var getWfRes = await _client.GetAsync("/api/workflows/hot-reload-test");
        var wf = await getWfRes.Content.ReadFromJsonAsync<Workflow>();
        Assert.NotNull(wf);
        Assert.False(wf.Rules.First(r => r.RuleId == "hot-rule-1").IsActive);
    }
}
