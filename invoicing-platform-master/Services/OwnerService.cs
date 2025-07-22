using Client_Invoice_System.Data;
using Client_Invoice_System.Models;
using Client_Invoice_System.Repository;
using Microsoft.EntityFrameworkCore; // For IDbContextFactory
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq; // For Linq

namespace Client_Invoice_System.Services
{
    public class OwnerService
    {
        private readonly OwnerRepository _ownerRepository;
        private readonly OwnerBankAccountRepository _ownerBankAccountRepository;
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public OwnerService(
            OwnerRepository ownerRepository,
            OwnerBankAccountRepository ownerBankAccountRepository,
            IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _ownerRepository = ownerRepository;
            _ownerBankAccountRepository = ownerBankAccountRepository;
            _contextFactory = contextFactory;
        }

        // Methods for OwnerProfile (can be added later if logic needs to be moved from UI)
        public async Task<OwnerProfile?> GetOwnerProfileByIdAsync(int id)
        {
            return await _ownerRepository.GetOwnerProfileByIdAsync(id); // Assumes this method in repo includes BankAccounts
        }
        
        public async Task<List<OwnerProfile>> GetAllOwnerProfilesAsync()
        {
             return (await _ownerRepository.GetAllOwnerProfilesAsync()).ToList(); // Assumes this method in repo includes BankAccounts and returns IEnumerable
        }

        public async Task UpdateOwnerProfileAsync(OwnerProfile ownerProfile)
        {
            await _ownerRepository.UpdateAsync(ownerProfile);
        }

        public async Task<OwnerProfile> CreateOwnerProfileAsync(OwnerProfile ownerProfile)
        {
            // Ensure BankAccounts are not processed here if they are handled separately
            // or ensure the incoming ownerProfile.BankAccounts are what you intend to save.
            // Typically, child collections are managed after the parent is created.
            var newProfile = new OwnerProfile 
            {
                OwnerName = ownerProfile.OwnerName,
                BillingEmail = ownerProfile.BillingEmail,
                PhoneNumber = ownerProfile.PhoneNumber,
                BillingAddress = ownerProfile.BillingAddress,
                CountryCurrencyId = ownerProfile.CountryCurrencyId,
                CustomCurrency = ownerProfile.CustomCurrency,
                Logo = ownerProfile.Logo,
                // BankAccounts will be empty initially, managed via AddBankAccountAsync
            };
             await _ownerRepository.AddAsync(newProfile);
            return newProfile;
        }


        // CRUD for OwnerBankAccount
        public async Task AddBankAccountAsync(OwnerBankAccount bankAccount)
        {
            if (bankAccount.IsDefault)
            {
                await _ownerBankAccountRepository.UnsetDefaultIfExistsAsync(bankAccount.OwnerProfileId, bankAccount.CurrencyId, 0); // 0 as we are adding a new one
            }
            await _ownerBankAccountRepository.AddAsync(bankAccount);
        }

        public async Task UpdateBankAccountAsync(OwnerBankAccount bankAccount)
        {
            if (bankAccount.IsDefault)
            {
                // Ensure other accounts for the same owner & currency are not default
                await _ownerBankAccountRepository.UnsetDefaultIfExistsAsync(bankAccount.OwnerProfileId, bankAccount.CurrencyId, bankAccount.Id);
            }
            await _ownerBankAccountRepository.UpdateAsync(bankAccount);
        }

        public async Task DeleteBankAccountAsync(int bankAccountId)
        {
            if(bankAccountId > 0)
                await _ownerBankAccountRepository.DeleteAsync(bankAccountId);
        }

        public async Task<OwnerBankAccount?> GetBankAccountByIdAsync(int id)
        {
            return await _ownerBankAccountRepository.GetByIdAsync(id);
        }

        public async Task<List<OwnerBankAccount>> GetBankAccountsByOwnerIdAsync(int ownerProfileId)
        {
             // This might require a specific method in OwnerBankAccountRepository if not covered by GenericRepository
             // For now, let's assume OwnerProfile loading in GetOwnerProfileByIdAsync includes these.
             // If direct access is needed:
            using var context = _contextFactory.CreateDbContext();
            return await context.OwnerBankAccounts
                                .Where(ba => ba.OwnerProfileId == ownerProfileId && !ba.IsDeleted)
                                .Include(ba => ba.CountryCurrency) // Eager load currency details
                                .Include(ba => ba.BankCountry) // Eager load bank country details (if different)
                                .ToListAsync();
        }
    }
}
