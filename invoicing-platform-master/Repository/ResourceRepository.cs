using Client_Invoice_System.Data;
using Client_Invoice_System.Models;
using Client_Invoice_System.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Client_Invoice_System.Repository
{
    public class ResourceRepository : GenericRepository<Resource>
    {
        private readonly ILogger<ResourceRepository> _logger;

        public ResourceRepository(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<ResourceRepository> logger)
            : base(contextFactory)
        {
            _logger = logger;
        }

        public async Task<IEnumerable<Resource>> GetByClientIdAsync(int clientId)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var resources = await context.Resources
                    .Include(r => r.Employee)
                    .Include(r => r.OwnerProfile)
                    .Where(r => r.ClientId == clientId && !r.IsDeleted)
                    .ToListAsync();
                _logger.LogInformation($"Retrieved {resources.Count} resources for ClientId {clientId}");
                return resources;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving resources for client {clientId}");
                return new List<Resource>();
            }
        }

        public async Task<List<int>> GetInvoicedResourceIdsAsync()
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var invoicedResourceIds = await context.InvoiceItems
                    .Include(ii => ii.Invoice)
                    .Where(ii => !ii.Invoice.IsDeleted && ii.Invoice.InvoiceStatuses != InvoiceStatus.Paid)
                    .Select(ii => ii.ResourceId)
                    .Distinct()
                    .ToListAsync();
                _logger.LogInformation($"Retrieved {invoicedResourceIds.Count} invoiced resource IDs");
                return invoicedResourceIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoiced resource IDs");
                return new List<int>();
            }
        }

        public async Task<decimal> CalculateClientBillingAsync(int clientId)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var invoiceItems = await context.InvoiceItems
                    .Include(ii => ii.Invoice)
                    .Include(ii => ii.Resource)
                    .Where(ii => ii.Invoice.ClientId == clientId && !ii.Invoice.IsDeleted)
                    .ToListAsync();
                var total = invoiceItems.Sum(ii => (ii.ConsumedHours ?? 0m) * (ii.RatePerHour ?? 0m));
                _logger.LogInformation($"Calculated billing for client {clientId}: {total}");
                return total;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calculating billing for client {clientId}");
                return 0;
            }
        }

        public async Task<IEnumerable<Resource>> GetAllResourcesWithDetailsAsync()
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var resources = await context.Resources
                    .Include(r => r.Client)
                    .Include(r => r.Employee)
                    .Where(r => !r.IsDeleted)
                    .ToListAsync();
                _logger.LogInformation($"Retrieved {resources.Count} resources with details");
                return resources;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all resources with details");
                return new List<Resource>();
            }
        }

        public async Task<Resource?> GetResourceDetailsAsync(int resourceId)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var resource = await context.Resources
                    .Include(r => r.Client)
                    .Include(r => r.Employee)
                    .Where(r => r.ResourceId == resourceId && !r.IsDeleted)
                    .IgnoreQueryFilters() // Bypass global filters on related Client
                    .FirstOrDefaultAsync();
                _logger.LogInformation($"Retrieved resource details for ResourceId {resourceId}");
                return resource;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving resource details for {resourceId}");
                return null;
            }
        }

        public async Task<List<Resource>> GetAllAsync()
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var resources = await context.Resources
                    .Include(r => r.Client)
                    .Include(r => r.Employee)
                    .Include(r => r.OwnerProfile)
                    .Where(r => !r.IsDeleted)
                    .IgnoreQueryFilters() // Bypass global filters on related Client
                    .ToListAsync();
                _logger.LogInformation($"Retrieved {resources.Count} resources");
                return resources;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all resources");
                return new List<Resource>();
            }
        }

        public async Task<Resource?> GetByIdAsync(int resourceId)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var resource = await context.Resources
                    .Include(r => r.Client)
                    .Include(r => r.Employee)
                    .Where(r => r.ResourceId == resourceId && !r.IsDeleted)
                    .IgnoreQueryFilters() // Bypass global filters on related Client
                    .FirstOrDefaultAsync();
                _logger.LogInformation($"Retrieved resource by ID {resourceId}");
                return resource;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving resource by ID {resourceId}");
                return null;
            }
        }
        public async Task AddAsync(Resource resource)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                await context.Resources.AddAsync(resource);
                await context.SaveChangesAsync();
                _logger.LogInformation($"Added resource with ID {resource.ResourceId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding resource");
                throw;
            }
        }


        public async Task<Resource> UpdateAsync(Resource resource)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var existingResource = await context.Resources
                    .Where(r => r.ResourceId == resource.ResourceId && !r.IsDeleted)
                    .FirstOrDefaultAsync();

                if (existingResource != null)
                {
                    existingResource.ClientId = resource.ClientId;
                    existingResource.ResourceName = resource.ResourceName;
                    existingResource.EmployeeId = resource.EmployeeId;
                    existingResource.IsActive = resource.IsActive;
                    existingResource.OwnerProfileId = resource.OwnerProfileId;
                    existingResource.CommittedHours = resource.CommittedHours;
                    existingResource.Recurrence = resource.Recurrence;
                    existingResource.UpdatedAt = DateTime.UtcNow;

                    await context.SaveChangesAsync();
                    _logger.LogInformation($"Updated resource with ID {resource.ResourceId}");

                    // Reload the resource to ensure we return the latest state
                    var updatedResource = await context.Resources
                        .Where(r => r.ResourceId == resource.ResourceId && !r.IsDeleted)
                        .FirstOrDefaultAsync();

                    return updatedResource ?? existingResource;
                }
                else
                {
                    _logger.LogWarning($"Resource with ID {resource.ResourceId} not found or deleted");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating resource {resource.ResourceId}");
                throw;
            }
        }
        public async Task DeleteAsync(int resourceId)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var resource = await context.Resources
                    .Where(r => r.ResourceId == resourceId && !r.IsDeleted)
                    .FirstOrDefaultAsync();

                if (resource != null)
                {
                    resource.IsDeleted = true;
                    await context.SaveChangesAsync();
                    _logger.LogInformation($"Deleted resource with ID {resourceId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting resource {resourceId}");
                throw;
            }
        }
    }
}