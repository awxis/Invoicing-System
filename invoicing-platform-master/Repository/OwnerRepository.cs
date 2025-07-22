using Client_Invoice_System.Data;
using Client_Invoice_System.Models;
using Client_Invoice_System.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Client_Invoice_System.Repository
{
    public class OwnerRepository : GenericRepository<OwnerProfile>
    {
        public OwnerRepository(IDbContextFactory<ApplicationDbContext> contextFactory)
            : base(contextFactory) { }

        public async Task<IEnumerable<OwnerProfile>> GetAllOwnerProfilesAsync()
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                return await context.Set<OwnerProfile>()
                                    .Include(o => o.CountryCurrency)
                                    .Include(o => o.BankAccounts)
                                        .ThenInclude(ba => ba.CountryCurrency) // For the account's currency
                                    .Include(o => o.BankAccounts)
                                        .ThenInclude(ba => ba.BankCountry) // For the bank's country (if different)
                                    .Where(o => !o.IsDeleted)
                                    .AsNoTracking()
                                    .ToListAsync();
            }
            catch (Exception)
            {

                throw;
            }

        }

        public async Task<IEnumerable<OwnerProfile>> GetAllOwnersAsync()
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                return await context.Set<OwnerProfile>()
                                    .Include(o => o.CountryCurrency) // Keep this for consistency if needed elsewhere
                                    .Include(o => o.BankAccounts)
                                        .ThenInclude(ba => ba.CountryCurrency)
                                    .Include(o => o.BankAccounts)
                                        .ThenInclude(ba => ba.BankCountry)
                                    .Where(o => !o.IsDeleted)
                                    .AsNoTracking()
                                    .ToListAsync();
            }
            catch (Exception)
            {

                throw;
            }

        }

        public async Task<OwnerProfile> GetOwnerProfileByIdAsync(int ownerId)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                return await context.Set<OwnerProfile>()
                                    .Include(o => o.CountryCurrency)
                                    .Include(o => o.BankAccounts)
                                        .ThenInclude(ba => ba.CountryCurrency)
                                    .Include(o => o.BankAccounts)
                                        .ThenInclude(ba => ba.BankCountry)
                                    .Where(o => !o.IsDeleted && o.Id == ownerId)
                                    .AsNoTracking() // Consider if tracking is needed if you plan to update immediately after fetching
                                    .FirstOrDefaultAsync();
            }
            catch (Exception)
            {

                throw;
            }

        }

        public async Task<OwnerProfile> GetOwnerProfileAsync()
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                return await context.Set<OwnerProfile>()
                                    .Include(o => o.CountryCurrency)
                                    .Include(o => o.BankAccounts)
                                        .ThenInclude(ba => ba.CountryCurrency)
                                    .Include(o => o.BankAccounts)
                                        .ThenInclude(ba => ba.BankCountry)
                                    .Where(o => !o.IsDeleted) // Exclude soft-deleted owners
                                    .AsNoTracking() // Consider if tracking is needed
                                    .FirstOrDefaultAsync() ?? new OwnerProfile();
            }
            catch (Exception)
            {

                throw;
            }

        }

        public async Task AddOwnerProfileAsync(OwnerProfile owner)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                try
                {
                    await context.Set<OwnerProfile>().AddAsync(owner);
                    await context.SaveChangesAsync();
                }
                catch (Exception)
                {
                    throw;
                }
            }
            catch (Exception)
            {

                throw;
            }

        }

        public async Task UpdateOwnerProfileAsync(OwnerProfile owner)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var existingOwner = await context.Set<OwnerProfile>().FirstOrDefaultAsync(o => o.Id == owner.Id && !o.IsDeleted);
                if (existingOwner != null)
                {
                    existingOwner.OwnerName = owner.OwnerName;
                    existingOwner.BillingEmail = owner.BillingEmail;
                    existingOwner.PhoneNumber = owner.PhoneNumber;
                    existingOwner.BillingAddress = owner.BillingAddress;
                    existingOwner.CustomCurrency = owner.CustomCurrency;
                    existingOwner.CountryCurrencyId = owner.CountryCurrencyId; // This is still valid
                    existingOwner.Logo = owner.Logo;
                    // BankAccounts collection is not managed here directly. 
                    // It should be handled by OwnerBankAccountRepository or a service layer.
                    context.Update(existingOwner);
                }
                else
                {
                    await context.Set<OwnerProfile>().AddAsync(owner);
                }
                await context.SaveChangesAsync();
            }
            catch (Exception)
            {

                throw;
            }

        }

        public async Task DeleteOwnerProfileAsync(int ownerId)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var owner = await context.Set<OwnerProfile>().FindAsync(ownerId);
                if (owner != null)
                {
                    owner.IsDeleted = true;
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception)
            {

                throw;
            }

        }
    }
}
