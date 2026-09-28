using System.ComponentModel.DataAnnotations;

namespace Backend.Dtos;

public class PaymentSubmitDtos
{
    [Required, MaxLength(50)]
    public string TransactionId { get; set; } = string.Empty;

    [MaxLength(30)]
    public string PaymentMethod { get; set; } = "bKash";
}

public class PaymentStatusDtos
{
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
    public string TransactionId { get; set; } = string.Empty;
}

public class PaymentApprovalDtos
{
    public int UserId { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
