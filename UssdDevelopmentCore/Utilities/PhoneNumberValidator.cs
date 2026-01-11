using System.Text;
using System.Text.RegularExpressions;

namespace UssdDevelopmentCore.Utilities;

public interface IPhoneNumberValidator
{
    bool ValidatePhoneNumber(string phoneNumber);
    PhoneNumberResponse AddCountryCode(string phoneNumber);
}

public class PhoneNumberValidator : IPhoneNumberValidator
{
    public bool ValidatePhoneNumber(string phoneNumber)
    {
        string pattern = @"^\+?(260|0)[7|9][5-7][0-9]{7}$";
        return Regex.IsMatch(phoneNumber, pattern);
    }

    public PhoneNumberResponse AddCountryCode(string phoneNumber)
    {
        var isValid = ValidatePhoneNumber(phoneNumber);
        
        if (isValid == false)
            return new PhoneNumberResponse
            {
                IsValid = false,
                PhoneNumber = phoneNumber
            };
        
        var phoneNumberArray = phoneNumber.ToCharArray();

        if (phoneNumberArray[0] == '+' || phoneNumberArray[0] == '2')
            return new PhoneNumberResponse
            {
                IsValid = true,
                PhoneNumber = phoneNumber,
                Message = "The phone number is valid"
            };

        var builder = new StringBuilder();
        builder.Append("26");
        builder.Append(phoneNumberArray);

        return new PhoneNumberResponse
        {
            IsValid = true,
            PhoneNumber = builder.ToString(),
            Message = "The phone number is valid"
        };
    }
}

public class PhoneNumberResponse
{
    public bool IsValid { get; set; }
    public required string PhoneNumber { get; set; }
    public string Message { get; set; } = "The phone number is not valid";
}