using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ShoeRetail.Domain;
using ShoeRetail.Infrastructure.Persistence;
using Xunit;

namespace ShoeRetail.Api.Tests;

// Gerçek shoeretail_dev'e bağlanan entegrasyon testi (mock yok — DB kısıtları ve
// trigger'lar ancak gerçek PostgreSQL ile doğrulanabilir). ShoeRetail.Api'nin
// User Secrets kimliğini kullanır; yerel geliştirme ortamı kurulu olmalı.
public class AppDbContextTests
{
    private const string ApiUserSecretsId = "dc5140c4-d28d-4a5b-8f03-102f408513f4";

    private static AppDbContext CreateContext()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets(ApiUserSecretsId)
            .Build();

        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default bulunamadı (User Secrets kurulu mu?).");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task SaveChanges_sets_CreatedAt_and_UpdatedAt_from_db_defaults()
    {
        await using var db = CreateContext();

        var supplier = new Supplier { CompanyName = "EF Test Tedarikçi", Phone = "0555 000 00 09" };
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();

        try
        {
            Assert.True(supplier.Id > 0);
            Assert.NotEqual(default, supplier.CreatedAt);
            Assert.NotEqual(default, supplier.UpdatedAt);
        }
        finally
        {
            db.Suppliers.Remove(supplier);
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task SaveChanges_UpdatedAt_is_refreshed_by_trigger_not_by_app()
    {
        await using var db = CreateContext();

        var supplier = new Supplier { CompanyName = "EF Trigger Testi", Phone = "0555 000 00 08" };
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();

        try
        {
            var updatedAtAfterInsert = supplier.UpdatedAt;

            await Task.Delay(1000); // now() farkını ölçülebilir kılmak için

            supplier.Notes = "değişti";
            await db.SaveChangesAsync();

            // Uygulama UpdatedAt'e hiç dokunmadı — trigger + ValueGeneratedOnAddOrUpdate
            // sayesinde EF, DB'nin now() ile yazdığı gerçek değeri RETURNING ile geri okudu.
            Assert.True(supplier.UpdatedAt > updatedAtAfterInsert);
        }
        finally
        {
            db.Suppliers.Remove(supplier);
            await db.SaveChangesAsync();
        }
    }
}
