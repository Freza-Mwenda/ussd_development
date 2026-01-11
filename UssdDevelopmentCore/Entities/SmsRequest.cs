namespace UssdDevelopmentCore.Entities;

public class SmsRequest
{
    public required string Text { get; set; }
    public required string Msisdn { get; set; }
}
