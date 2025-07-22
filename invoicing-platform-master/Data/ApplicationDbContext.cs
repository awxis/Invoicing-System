using Client_Invoice_System.Components.Pages.Invoice_Pages;
using Client_Invoice_System.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Client_Invoice_System.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<OwnerProfile> Owners { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<ActiveClient> ActiveClients { get; set; }
    public DbSet<Resource> Resources { get; set; }
    public DbSet<ClientProfileCrossTable> ClientProfileCrosses { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<CountryCurrency> CountryCurrencies { get; set; }
    public DbSet<InvoiceItem> InvoiceItems { get; set; }
    public DbSet<Receipt> Receipts { get; set; }
    public DbSet<OwnerBankAccount> OwnerBankAccounts { get; set; }
    public DbSet<Designation> Designations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Global Query Filters
        modelBuilder.Entity<Client>()
            .HasQueryFilter(c => !c.IsDeleted);
        modelBuilder.Entity<ActiveClient>()
            .HasQueryFilter(ac => !ac.Client.IsDeleted);
        modelBuilder.Entity<Invoice>()
            .HasQueryFilter(i => !i.IsDeleted);
        modelBuilder.Entity<Resource>()
            .HasQueryFilter(r => !r.IsDeleted);
        modelBuilder.Entity<Receipt>()
            .HasQueryFilter(r => !r.IsDeleted);

        // Employee Configuration
        modelBuilder.Entity<Employee>()
            .Property(e => e.EmployeeName)
            .IsRequired()
            .HasMaxLength(255);

        // Resource Configuration
        modelBuilder.Entity<Resource>()
            .Property(r => r.ResourceName)
            .IsRequired()
            .HasMaxLength(255);
        modelBuilder.Entity<Resource>()
            .Property(r => r.CommittedHours)
            .HasPrecision(18, 2);
        modelBuilder.Entity<Resource>()
            .Property(r => r.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // InvoiceItem Configuration
        modelBuilder.Entity<InvoiceItem>()
            .Property(ii => ii.ConsumedHours)
            .HasPrecision(18, 2);
        modelBuilder.Entity<InvoiceItem>()
            .Property(ii => ii.RatePerHour)
            .HasPrecision(18, 2);
        modelBuilder.Entity<InvoiceItem>()
            .Property(i => i.TotalAmount)
            .HasPrecision(18, 2) // Ensure precision matches
            .HasDefaultValue(0m);

        // Client Configuration
        modelBuilder.Entity<Client>()
            .Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(255);
        modelBuilder.Entity<Client>()
            .Property(c => c.Email)
            .HasMaxLength(255);
        modelBuilder.Entity<Client>()
            .Property(c => c.PhoneNumber)
            .HasMaxLength(50);
        modelBuilder.Entity<Client>()
            .Property(c => c.Address)
            .HasMaxLength(500);

        // Invoice Configuration
        modelBuilder.Entity<Invoice>()
            .Property(i => i.TotalAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);
        modelBuilder.Entity<Invoice>()
            .Property(i => i.PaidAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);
        modelBuilder.Entity<Invoice>()
            .Property(i => i.RemainingAmount)
            .HasPrecision(18, 2)
            .HasComputedColumnSql("CAST([TotalAmount] - [PaidAmount] AS DECIMAL(18,2))");

        // CountryCurrency Configuration
        modelBuilder.Entity<CountryCurrency>()
            .Property(cc => cc.Symbol)
            .HasMaxLength(10);
        modelBuilder.Entity<CountryCurrency>()
            .Property(cc => cc.CurrencyCode)
            .HasMaxLength(10);
        modelBuilder.Entity<CountryCurrency>()
            .Property(cc => cc.CurrencyName)
            .HasMaxLength(50);

        // OwnerProfile Configuration
        modelBuilder.Entity<OwnerProfile>()
            .Property(op => op.OwnerName)
            .IsRequired()
            .HasMaxLength(255);
        modelBuilder.Entity<OwnerProfile>()
            .Property(op => op.BillingEmail)
            .HasMaxLength(255);
        modelBuilder.Entity<OwnerProfile>()
            .Property(op => op.PhoneNumber)
            .HasMaxLength(50);
        modelBuilder.Entity<OwnerProfile>()
            .Property(op => op.BillingAddress)
            .HasMaxLength(500);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter(ConvertFilterExpression(entityType.ClrType));
            }
        }

        // Invoice & InvoiceItem Relationship
        modelBuilder.Entity<InvoiceItem>()
            .HasOne(ii => ii.Invoice)
            .WithMany(i => i.InvoiceItems)
            .HasForeignKey(ii => ii.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InvoiceItem>()
            .HasOne(ii => ii.Resource)
            .WithMany()
            .HasForeignKey(ii => ii.ResourceId)
            .OnDelete(DeleteBehavior.Restrict);

        // Client & Invoice Relationship
        modelBuilder.Entity<Client>()
            .HasMany(c => c.Invoices)
            .WithOne(i => i.Client)
            .HasForeignKey(i => i.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Receipt>()
       .HasOne(r => r.Client)
       .WithMany(c => c.Receipts)
       .HasForeignKey(r => r.ClientId)
       .IsRequired(false);

        modelBuilder.Entity<Receipt>()
          .HasOne(c => c.CountryCurrency)
          .WithMany()
          .HasForeignKey(c => c.CurrencyId);

        modelBuilder.Entity<CountryCurrency>().HasData(
            new CountryCurrency { Id = 1, CountryName = "United States", CurrencyName = "US Dollar", Symbol = "$", CurrencyCode = "USD" },
            new CountryCurrency { Id = 2, CountryName = "United Kingdom", CurrencyName = "Pound Sterling", Symbol = "£", CurrencyCode = "GBP" },
            new CountryCurrency { Id = 3, CountryName = "European Union", CurrencyName = "Euro", Symbol = "€", CurrencyCode = "EUR" },
            new CountryCurrency { Id = 4, CountryName = "Japan", CurrencyName = "Japanese Yen", Symbol = "¥", CurrencyCode = "JPY" },
            new CountryCurrency { Id = 5, CountryName = "India", CurrencyName = "Indian Rupee", Symbol = "₹", CurrencyCode = "INR" },
            new CountryCurrency { Id = 6, CountryName = "Canada", CurrencyName = "Canadian Dollar", Symbol = "C$", CurrencyCode = "CAD" },
            new CountryCurrency { Id = 7, CountryName = "Australia", CurrencyName = "Australian Dollar", Symbol = "A$", CurrencyCode = "AUD" },
            new CountryCurrency { Id = 8, CountryName = "Switzerland", CurrencyName = "Swiss Franc", Symbol = "CHF", CurrencyCode = "CHF" },
            new CountryCurrency { Id = 9, CountryName = "China", CurrencyName = "Chinese Yuan", Symbol = "¥", CurrencyCode = "CNY" },
            new CountryCurrency { Id = 10, CountryName = "Russia", CurrencyName = "Russian Ruble", Symbol = "₽", CurrencyCode = "RUB" },
            new CountryCurrency { Id = 11, CountryName = "Brazil", CurrencyName = "Brazilian Real", Symbol = "R$", CurrencyCode = "BRL" },
            new CountryCurrency { Id = 12, CountryName = "South Korea", CurrencyName = "South Korean Won", Symbol = "₩", CurrencyCode = "KRW" },
            new CountryCurrency { Id = 13, CountryName = "Mexico", CurrencyName = "Mexican Peso", Symbol = "$", CurrencyCode = "MXN" },
            new CountryCurrency { Id = 14, CountryName = "South Africa", CurrencyName = "South African Rand", Symbol = "R", CurrencyCode = "ZAR" },
            new CountryCurrency { Id = 15, CountryName = "Singapore", CurrencyName = "Singapore Dollar", Symbol = "S$", CurrencyCode = "SGD" },
            new CountryCurrency { Id = 16, CountryName = "New Zealand", CurrencyName = "New Zealand Dollar", Symbol = "NZ$", CurrencyCode = "NZD" },
            new CountryCurrency { Id = 17, CountryName = "Turkey", CurrencyName = "Turkish Lira", Symbol = "₺", CurrencyCode = "TRY" },
            new CountryCurrency { Id = 18, CountryName = "Saudi Arabia", CurrencyName = "Saudi Riyal", Symbol = "﷼", CurrencyCode = "SAR" },
            new CountryCurrency { Id = 19, CountryName = "Sweden", CurrencyName = "Swedish Krona", Symbol = "kr", CurrencyCode = "SEK" },
            new CountryCurrency { Id = 20, CountryName = "Norway", CurrencyName = "Norwegian Krone", Symbol = "kr", CurrencyCode = "NOK" }
            //new CountryCurrency { Id = 21, CountryName = "Pakistan", CurrencyName = "Rupees", Symbol = "RS", CurrencyCode = "PKR" }
        );

        var hasher = new PasswordHasher<ApplicationUser>();
        var user = new ApplicationUser
        {
            Id = Guid.Parse("9a869d71-1c2a-4b99-9f2f-4fbbda7b0d5e").ToString(),
            UserName = "admin@gmail.com",
            NormalizedUserName = "ADMIN@GMAIL.COM",
            Email = "admin@gmail.com",
            NormalizedEmail = "ADMIN@GMAIL.COM",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString("D"),
            ConcurrencyStamp = Guid.NewGuid().ToString("D")
        };

        user.PasswordHash = hasher.HashPassword(user, "Admin@123");

        modelBuilder.Entity<ApplicationUser>().HasData(user);

        // ActiveClient & Client Relationship
        modelBuilder.Entity<ActiveClient>()
            .HasOne(ac => ac.Client)
            .WithOne(c => c.ActiveClient)
            .HasForeignKey<ActiveClient>(ac => ac.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        // Client & Resources Relationship
        modelBuilder.Entity<Client>()
            .HasMany(c => c.Resources)
            .WithOne(r => r.Client)
            .HasForeignKey(r => r.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        // Employee & Resources Relationship
        modelBuilder.Entity<Employee>()
            .HasMany(e => e.Resources)
            .WithOne(r => r.Employee)
            .HasForeignKey(r => r.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Client Profile Cross Table (Many-to-Many)
        modelBuilder.Entity<ClientProfileCrossTable>()
            .HasKey(cpc => new { cpc.ClientId, cpc.EmployeeId });

        modelBuilder.Entity<ClientProfileCrossTable>()
            .HasOne(cpc => cpc.Client)
            .WithMany(c => c.ClientProfileCrosses)
            .HasForeignKey(cpc => cpc.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ClientProfileCrossTable>()
            .HasOne(cpc => cpc.Employee)
            .WithMany()
            .HasForeignKey(cpc => cpc.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // CountryCurrency Relationships
        modelBuilder.Entity<Client>()
            .HasOne(c => c.CountryCurrency)
            .WithMany()
            .HasForeignKey(c => c.CountryCurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OwnerProfile>()
            .HasOne(o => o.CountryCurrency)
            .WithMany()
            .HasForeignKey(o => o.CountryCurrencyId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Invoice>()
            .HasOne(i => i.CountryCurrency)
            .WithMany()
            .HasForeignKey(i => i.CountryCurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Resource>()
            .HasOne(r => r.OwnerProfile)
            .WithMany(o => o.Resources)
            .HasForeignKey(r => r.OwnerProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        // OwnerProfile and OwnerBankAccount Relationship
        modelBuilder.Entity<OwnerProfile>()
            .HasMany(op => op.BankAccounts)
            .WithOne(ba => ba.OwnerProfile)
            .HasForeignKey(ba => ba.OwnerProfileId)
            .OnDelete(DeleteBehavior.Cascade); // Cascade delete if OwnerProfile is deleted

        // OwnerBankAccount Configuration
        modelBuilder.Entity<OwnerBankAccount>(entity =>
        {
            entity.Property(ba => ba.IsDefault)
                  .HasDefaultValue(false);

            // Unique constraint for IsDefault per OwnerProfileId and CurrencyId
            // Ensures only one bank account can be default for a specific owner and currency.
            entity.HasIndex(ba => new { ba.OwnerProfileId, ba.CurrencyId, ba.IsDefault })
                  .IsUnique()
                  .HasFilter("[IsDefault] = 1"); // SQL Server specific filter

            // Relationship for CurrencyId (to CountryCurrency)
            entity.HasOne(oba => oba.CountryCurrency)
                  .WithMany() // CountryCurrency doesn't have a collection of OwnerBankAccounts for this specific FK
                  .HasForeignKey(oba => oba.CurrencyId)
                  .OnDelete(DeleteBehavior.Restrict); // Restrict deletion if currency is in use

            // Relationship for nullable CountryId (BankCountry to CountryCurrency)
            entity.HasOne(oba => oba.BankCountry)
                  .WithMany() // CountryCurrency doesn't have a specific collection for BankCountry of OwnerBankAccounts
                  .HasForeignKey(oba => oba.CountryId)
                  .IsRequired(false) // CountryId is nullable
                  .OnDelete(DeleteBehavior.Restrict); // Restrict deletion if country is in use
        });
    }

    public override int SaveChanges()
    {
        HandleSoftDelete();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        HandleSoftDelete();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void HandleSoftDelete()
    {
        var entriesToSoftDelete = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Deleted && e.Entity is ISoftDeletable)
            .ToList();

        foreach (var entry in entriesToSoftDelete)
        {
            Console.WriteLine($"Soft-deleting entity: {entry.Entity.GetType().Name}, ID: {entry.Property("Id")?.CurrentValue ?? entry.Property("ClientId")?.CurrentValue}");

            if (entry.Entity is Client client)
            {
                var relatedInvoices = ChangeTracker.Entries<Invoice>()
                    .Where(e => e.State == EntityState.Deleted && e.Entity.ClientId == client.ClientId)
                    .ToList();

                foreach (var invoiceEntry in relatedInvoices)
                {
                    invoiceEntry.State = EntityState.Unchanged;
                    Console.WriteLine($"Prevented deletion of Invoice with ID: {invoiceEntry.Entity.InvoiceId}");
                }

                var relatedResources = ChangeTracker.Entries<Resource>()
                    .Where(e => e.State == EntityState.Deleted && e.Entity.ClientId == client.ClientId)
                    .ToList();

                foreach (var resourceEntry in relatedResources)
                {
                    resourceEntry.State = EntityState.Unchanged;
                    Console.WriteLine($"Prevented deletion of Resource with ID: {resourceEntry.Entity.ResourceId}");
                }
            }

            entry.State = EntityState.Modified;
            ((ISoftDeletable)entry.Entity).IsDeleted = true;
        }
    }

    private static LambdaExpression ConvertFilterExpression(Type entityType)
    {
        var parameter = Expression.Parameter(entityType, "e");
        var property = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
        var condition = Expression.Equal(property, Expression.Constant(false));
        return Expression.Lambda(condition, parameter);
    }
}