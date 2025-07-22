using Client_Invoice_System.Data;
using Client_Invoice_System.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks; // Added for async Task examples
using System.Linq;
using Client_Invoice_System.Repositories; // Added for Linq examples

namespace Client_Invoice_System.Repository
{
    public class OwnerBankAccountRepository : GenericRepository<OwnerBankAccount>
    {
        public OwnerBankAccountRepository(IDbContextFactory<ApplicationDbContext> contextFactory) : base(contextFactory)
        {
        }

        // Example of a specific method that might be needed in the future
        public async Task<OwnerBankAccount?> GetDefaultByOwnerAndCurrencyAsync(int ownerProfileId, int currencyId)
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.Set<OwnerBankAccount>()
                .Where(ba => ba.OwnerProfileId == ownerProfileId && ba.CurrencyId == currencyId && ba.IsDefault && !ba.IsDeleted)
                .FirstOrDefaultAsync();
        }

        // Example of a specific method to unset other defaults when a new default is set
        public async Task UnsetDefaultIfExistsAsync(int ownerProfileId, int currencyId, int currentBankAccountIdToExclude)
        {
            using var context = _contextFactory.CreateDbContext();
            var existingDefaults = await context.Set<OwnerBankAccount>()
                .Where(ba => ba.OwnerProfileId == ownerProfileId && 
                             ba.CurrencyId == currencyId && 
                             ba.IsDefault && 
                             ba.Id != currentBankAccountIdToExclude && 
                             !ba.IsDeleted)
                .ToListAsync();

            if (existingDefaults.Any())
            {
                foreach (var account in existingDefaults)
                {
                    account.IsDefault = false;
                }
                await context.SaveChangesAsync();
            }
        }
    }
}
