using Microsoft.EntityFrameworkCore;
using UssdDevelopmentCore.Database;

namespace UssdDevelopmentCore.Utilities;

public interface IGeneratorService
{
    Task<string> TxnId();
}

public class GeneratorService(NasdacDatabase database) : IGeneratorService
{
    public async Task<string> TxnId()
    {
        Random random = new Random();
        int randomNumber = random.Next(1000, 10000);
        var total = await database.WalletTransactions.CountAsync();

        return $"NGMTXN-{randomNumber}/{DateTime.Now.Year}/{DateTime.Now.Month}/{total + 1:00000}";
    }
}

public static class Generator
{
    public static int ParseString(this string input)
    {
        return char.IsLetter(input[0]) ? 1000000 : int.Parse(input);
    }
}