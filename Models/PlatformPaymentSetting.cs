using System;

namespace WebApplication1.Models;

public partial class PlatformPaymentSetting
{
    public long Id { get; set; }
    public string MerchantName { get; set; } = null!;
    public string UpiId { get; set; } = null!;
    public string? QrCodePath { get; set; }
    public string? BankAccount { get; set; }
    public string? AccountHolder { get; set; }
    public string? Ifsc { get; set; }
    public string? PaymentInstructions { get; set; }
    public DateTime UpdatedAt { get; set; }
}
