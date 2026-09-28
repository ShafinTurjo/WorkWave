using System.ComponentModel.DataAnnotations;

namespace Frontend.Models;

// Backend.Dtos-er mirror. Frontend (WASM) Backend project reference korte pare na,
// tai eikhane alada copy rakha hoyeche.
public class PaymentSubmitModel
{
    [Required(ErrorMessage = "Transaction ID din.")]
    [MaxLength(50)]
    public string TransactionId { get; set; } = string.Empty;

    public string PaymentMethod { get; set; } = "bKash";
}

public class PaymentStatusModel
{
    public string Status { get; set; } = "Pending";
    public string TransactionId { get; set; } = string.Empty;
}

public class PaymentApprovalModel
{
    public int UserId { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
