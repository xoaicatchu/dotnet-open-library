namespace DynamicRuleEngine.Api.Domain.UseCases;

/// <summary>
/// Ngữ cảnh đánh giá bảo hiểm y tế (HIS / BHYT).
/// Dữ liệu đầu vào thực tế từ hồ sơ bệnh nhân khi thanh toán viện phí.
/// </summary>
public class MedicalInsuranceContext
{
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public int Age { get; set; }
    public bool IsRightRoute { get; set; } // Khám đúng tuyến hay vượt tuyến / trái tuyến
    public string InsuranceCardCode { get; set; } = string.Empty; // Mã thẻ BHYT (ví dụ: "CC1", "TE1", "DN4", "GD4")
    public string DiagnosisCode { get; set; } = string.Empty; // Mã bệnh ICD-10 (ví dụ: "I10" - Tăng huyết áp, "E11" - Đái tháo đường)
    public decimal TotalMedicalBill { get; set; } // Tổng tiền viện phí
    public bool HasChronicDisease { get; set; } // Có bệnh mãn tính không
}
