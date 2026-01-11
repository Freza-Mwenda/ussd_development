using System.Text.Json.Serialization;

namespace UssdDevelopmentCore.Entities;

public class Customer
{
    [JsonPropertyName("fullName")] public string FullName { get; set; }

    [JsonPropertyName("phoneNumber")] public string PhoneNumber { get; set; }

    [JsonPropertyName("email")] public string Email { get; set; }
}