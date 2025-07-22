using Client_Invoice_System.Data;
using Client_Invoice_System.Models;
using Client_Invoice_System.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Client_Invoice_System.Repository
{
    public class ReceiptRepository : GenericRepository<Receipt>
    {
        public ReceiptRepository(IDbContextFactory<ApplicationDbContext> contextFactory)
      : base(contextFactory) { }
        public async Task<IEnumerable<Receipt>> GetAllAsync()
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var receipt = await context.Receipts
                                   .Include(r => r.Client)
                                       .ThenInclude(c => c.CountryCurrency)
                                   .Include(r => r.CountryCurrency) // this line adds CountryCurrency directly from Receipt
                                   .Where(e => !e.IsDeleted)
                                   .AsNoTracking()
                                   .ToListAsync();
                return receipt ?? new List<Receipt>();
            }
            catch (Exception)
            {

                throw;
            }

        }
        public override async Task AddAsync(Receipt receipt)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                await context.Receipts.AddAsync(receipt);
                await context.SaveChangesAsync();
            }
            catch (Exception)
            {

                throw;
            }

        }
    }
}
