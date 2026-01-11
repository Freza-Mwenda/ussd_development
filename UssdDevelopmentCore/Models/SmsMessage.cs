namespace UssdDevelopmentCore.Models;

public class SmsMessage : BaseEntity
{
    public string Text { get; set; } = "";
    public string Recipient { get; set; } = "";
    public string Sender { get; set; } = "";
}