using Microsoft.EntityFrameworkCore;
using UssdDevelopmentCore.Extensions;
using UssdDevelopmentCore.Models;

namespace UssdDevelopmentCore.Database;

public class NasdacDatabase : DbContext
{
    public NasdacDatabase(DbContextOptions<NasdacDatabase> options) : base(options) { }
    
    public DbSet<Member> Members => Set<Member>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
    public DbSet<VoucherTransaction> VoucherTransactions => Set<VoucherTransaction>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<SmsMessage> SmsMessages => Set<SmsMessage>();
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.MapNpgsqlEnums();
        builder.Entity<Member>().ToTable("Members");
        builder.Entity<WalletTransaction>().ToTable("WalletTransactions");
        builder.Entity<VoucherTransaction>().ToTable("VoucherTransactions");
        builder.Entity<Wallet>().ToTable("Wallets");
        builder.Entity<SmsMessage>().ToTable("SmsMessages");
    }
}