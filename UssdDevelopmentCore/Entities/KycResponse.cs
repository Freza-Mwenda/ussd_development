using System.Text.Json.Serialization;

namespace UssdDevelopmentCore.Entities;

public class KycResponse
{
    [JsonPropertyName("StatusCode")] public int StatusCode { get; set; }

    [JsonPropertyName("first_name")] public string FirstName { get; set; }

    [JsonPropertyName("last_name")] public string LastName { get; set; }
}