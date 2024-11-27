using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShopSystem.Core.Dtos.Program;
using ShopSystem.Core.Dtos;
using ShopSystem.Core.Models.Entites;
using ShopSystem.Core.Services.Programe;
using ShopSystem.Repository.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopSystem.Repository.Reposatories.Programe
{
    public class PurchaseService : IPurchaseRepository
    {
        private readonly StoreContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<PurchaseService> _logger;

        public PurchaseService(StoreContext context, IMapper mapper, ILogger<PurchaseService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        //public async Task<PagedResult<PurchaseDTO>> GetPurchasesWithFiltersAsync(PaginationParameters paginationParameters, QueryOptions queryOptions)
        //{
        //    try
        //    {
        //        var query = _context.Purchases.Include(p => p.PurchaseItems).AsQueryable();

        //        // Apply filters
        //        if (queryOptions.MinAmount.HasValue)
        //            query = query.Where(p => p.TotalAmount >= queryOptions.MinAmount.Value);

        //        if (queryOptions.MaxAmount.HasValue)
        //            query = query.Where(p => p.TotalAmount <= queryOptions.MaxAmount.Value);

        //        // Apply search
        //        if (!string.IsNullOrEmpty(queryOptions.Search))
        //            query = query.Where(p => p.Notes.Contains(queryOptions.Search));

        //        // Apply sorting
        //        if (!string.IsNullOrEmpty(queryOptions.SortField))
        //        {
        //            var propertyInfo = typeof(Purchase).GetProperty(queryOptions.SortField);
        //            if (propertyInfo != null)
        //            {
        //                query = queryOptions.SortDescending
        //                    ? query.OrderByDescending(e => EF.Property<object>(e, queryOptions.SortField))
        //                    : query.OrderBy(e => EF.Property<object>(e, queryOptions.SortField));
        //            }
        //            else
        //            {
        //                _logger.LogWarning($"Sort field '{queryOptions.SortField}' does not exist on Purchase entity.");
        //            }
        //        }

        //        // Get total count
        //        var totalItems = await query.CountAsync();

        //        // Apply pagination
        //        var purchases = await query
        //            .Skip((paginationParameters.PageNumber - 1) * paginationParameters.PageSize)
        //            .Take(paginationParameters.PageSize)
        //            .ToListAsync();

        //        var purchaseDtos = _mapper.Map<List<PurchaseDTO>>(purchases);

        //        return new PagedResult<PurchaseDTO>
        //        {
        //            Items = purchaseDtos,
        //            TotalCount = totalItems,
        //            PageNumber = paginationParameters.PageNumber,
        //            PageSize = paginationParameters.PageSize
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "An error occurred while fetching purchases with filters.");
        //        throw;
        //    }
        //}



        public async Task<PagedResult<PurchaseDTO>> GetPurchasesWithFiltersAsync(PaginationParameters paginationParameters, QueryOptions queryOptions)
        {
            try
            {
                var query = _context.Purchases
                    .Include(p => p.PurchaseItems)
                    .Include(p => p.Merchant)  // Join with Merchant table
                    .AsQueryable();

                // Apply filters
                if (queryOptions.MinAmount.HasValue)
                    query = query.Where(p => p.TotalAmount >= queryOptions.MinAmount.Value);

                if (queryOptions.MaxAmount.HasValue)
                    query = query.Where(p => p.TotalAmount <= queryOptions.MaxAmount.Value);

                // Apply search for notes
                if (!string.IsNullOrEmpty(queryOptions.Search))
                    query = query.Where(p => p.Notes.Contains(queryOptions.Search));

                // Apply search for merchant name
                if (!string.IsNullOrEmpty(queryOptions.Name))
                    query = query.Where(p => p.Merchant.Name.Contains(queryOptions.Name));  // Filter by Merchant Name

                // Apply date range filter
                if (queryOptions.StartDate.HasValue)
                    query = query.Where(p => p.OrderDate >= queryOptions.StartDate.Value);

                if (queryOptions.EndDate.HasValue)
                    query = query.Where(p => p.OrderDate <= queryOptions.EndDate.Value);

                // Apply sorting
                if (!string.IsNullOrEmpty(queryOptions.SortField))
                {
                    // Check for valid sort field
                    var propertyInfo = typeof(Purchase).GetProperty(queryOptions.SortField);
                    if (propertyInfo != null)
                    {
                        // Sort by the requested field
                        if (queryOptions.SortField.Equals("TotalAmount", StringComparison.OrdinalIgnoreCase))
                        {
                            query = queryOptions.SortDescending
                                ? query.OrderByDescending(p => p.TotalAmount)
                                : query.OrderBy(p => p.TotalAmount);
                        }
                        else if (queryOptions.SortField.Equals("PurchaseDate", StringComparison.OrdinalIgnoreCase))
                        {
                            query = queryOptions.SortDescending
                                ? query.OrderByDescending(p => p.OrderDate)
                                : query.OrderBy(p => p.OrderDate);
                        }
                        else
                        {
                            query = queryOptions.SortDescending
                                ? query.OrderByDescending(e => EF.Property<object>(e, queryOptions.SortField))
                                : query.OrderBy(e => EF.Property<object>(e, queryOptions.SortField));
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Sort field '{queryOptions.SortField}' does not exist on Purchase entity.");
                    }
                }

                // Get total count
                var totalItems = await query.CountAsync();

                // Apply pagination
                var purchases = await query
                    .Skip((paginationParameters.PageNumber - 1) * paginationParameters.PageSize)
                    .Take(paginationParameters.PageSize)
                    .ToListAsync();

                var purchaseDtos = _mapper.Map<List<PurchaseDTO>>(purchases);

                return new PagedResult<PurchaseDTO>
                {
                    Items = purchaseDtos,
                    TotalCount = totalItems,
                    PageNumber = paginationParameters.PageNumber,
                    PageSize = paginationParameters.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching purchases with filters.");
                throw;
            }
        }


        public async Task<PurchaseDTO> GetPurchaseByIdAsync(int purchaseId)
        {
            try
            {
                var purchase = await _context.Purchases
                    .Include(p => p.PurchaseItems)
                    .FirstOrDefaultAsync(p => p.Id == purchaseId);

                if (purchase == null)
                {
                    _logger.LogWarning($"Purchase with ID {purchaseId} not found.");
                    return null;
                }

                return _mapper.Map<PurchaseDTO>(purchase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while retrieving purchase with ID {purchaseId}.");
                throw;
            }
        }

        public async Task<PurchaseDTO> CreatePurchaseAsync(CreatePurchaseDTO purchaseDto)
        {
            try
            {
                var purchase = _mapper.Map<Purchase>(purchaseDto);
                purchase.TotalAmount = purchase.PurchaseItems.Sum(item => item.PricePerUnit * item.Quantity);

                await _context.Purchases.AddAsync(purchase);
                await _context.SaveChangesAsync();

                return _mapper.Map<PurchaseDTO>(purchase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating a purchase.");
                throw;
            }
        }

        public async Task<bool> UpdatePurchaseDetailsAsync(int purchaseId, CreatePurchaseDTO purchaseDto)
        {
            try
            {
                var purchase = await _context.Purchases.Include(p => p.PurchaseItems).FirstOrDefaultAsync(p => p.Id == purchaseId);

                if (purchase == null)
                {
                    _logger.LogWarning($"Purchase with ID {purchaseId} not found for update.");
                    return false;
                }

                // Remove existing purchase items and map new data
                _context.PurchaseItems.RemoveRange(purchase.PurchaseItems);
                var updatedPurchase = _mapper.Map(purchaseDto, purchase);
                updatedPurchase.TotalAmount = updatedPurchase.PurchaseItems.Sum(item => item.PricePerUnit * item.Quantity);

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while updating purchase with ID {purchaseId}.");
                throw;
            }
        }

        public async Task<int> DeleteMultiplePurchasesAsync(IEnumerable<int> ids)
        {
            try
            {
                var purchases = await _context.Purchases
                    .Where(p => ids.Contains(p.Id))
                    .ToListAsync();

                if (!purchases.Any())
                {
                    _logger.LogWarning("No matching purchases found for deletion.");
                    return 0;
                }

                _context.Purchases.RemoveRange(purchases);
                var deletedCount = await _context.SaveChangesAsync();
                _logger.LogInformation($"{deletedCount} purchases deleted successfully.");
                return deletedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting multiple purchases.");
                throw;
            }
        }


        public async Task<decimal> GetPurchaseTotalAmountAsync(int purchaseId)
        {
            try
            {
                var purchase = await _context.Purchases
                    .Include(p => p.PurchaseItems)  // Make sure PurchaseItems are included
                    .FirstOrDefaultAsync(p => p.Id == purchaseId);

                if (purchase == null)
                {
                    _logger.LogWarning($"Purchase with ID {purchaseId} not found.");
                    return 0;  // Or throw exception, depending on your desired behavior
                }

                // Sum of PricePerUnit * Quantity for each PurchaseItem
                var totalAmount = purchase.PurchaseItems.Sum(item => item.PricePerUnit * item.Quantity);

                return totalAmount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while calculating the total amount for purchase ID {purchaseId}.");
                throw;
            }
        }


    }
}

