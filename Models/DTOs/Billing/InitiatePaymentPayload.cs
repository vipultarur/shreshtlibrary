namespace WebApplication1.Models.DTOs.Billing
{
    public class InitiatePaymentPayload
    {
        public int plan_id { get; set; }
        public string payment_mode { get; set; } = "UPI";
        public string? transaction_id { get; set; }
        public int duration_days { get; set; } = 30;
    }
}
