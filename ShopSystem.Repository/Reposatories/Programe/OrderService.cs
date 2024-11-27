using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShopSystem.Core.Dtos.Program.Ivoice;
using ShopSystem.Core.Dtos.Program;
using ShopSystem.Core.Dtos;
using ShopSystem.Core.Models.Entites;
using ShopSystem.Core.Services.Programe;
using ShopSystem.Repository.Data.Identity;
using ShopSystem.Repository.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Kernel.Pdf.Canvas;
using PdfDocument = iText.Kernel.Pdf.PdfDocument;
using PdfDocumentLayout = iText.Layout.Document;
using iText.Layout.Properties;
using System.Drawing.Printing;
using System.IO;
using iText.IO.Font;
using iText.Kernel.Font;



namespace ShopSystem.Repository.Reposatories.Programe
{
    public class OrderService : IOrderRepository
    {
        private readonly StoreContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<OrderService> _logger;
        private readonly AppIdentityDbContext _appIdentityDbContext;


        // Constructor with dependencies injected
        public OrderService(StoreContext context, IMapper mapper, ILogger<OrderService> logger,
            AppIdentityDbContext appIdentityDbContext)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _appIdentityDbContext = appIdentityDbContext;

        }

        public async Task<OrderDTO> CreateOrderAsync(CreateOrderDTO createOrderDto, string userId)
        {
            // Begin a new transaction
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Verify the user exists in the database
                var user = await _appIdentityDbContext.Users
                                .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    throw new InvalidOperationException($"User with ID {userId} does not exist in the AppUser table.");
                }

                // Initialize a new order entity
                var order = new Order
                {
                    CustomerId = createOrderDto.CustomerId,
                    UserId = userId,  // Associate the order with the found user
                    OrderDate = DateTime.Now,
                    OrderItems = new List<OrderItem>()
                };

                decimal totalAmount = 0;
                decimal totalDiscount = 0;

                // Loop through each order item to process
                foreach (var itemDto in createOrderDto.OrderItems)
                {
                    var product = await _context.Products.FindAsync(itemDto.ProductId);
                    if (product == null)
                    {
                        throw new InvalidOperationException($"Product with ID {itemDto.ProductId} does not exist.");
                    }

                    if (product.Quantity < itemDto.Quantity)
                    {
                        throw new InvalidOperationException($"Insufficient stock for product ID {itemDto.ProductId}. Available quantity: {product.Quantity}");
                    }

                    // Deduct stock from the product
                    product.Quantity -= itemDto.Quantity;
                    product.IsStock = product.Quantity > 0;

                    // Calculate item totals and discounts
                    decimal itemSubtotal = itemDto.Quantity * product.SellingPrice;
                    decimal itemDiscount = itemSubtotal * (itemDto.Discount / 100m);
                    decimal itemTotal = itemSubtotal - itemDiscount;

                    totalAmount += itemTotal;
                    totalDiscount += itemDiscount;

                    // Create an order item and add it to the order
                    var orderItem = new OrderItem
                    {
                        ProductId = itemDto.ProductId,
                        Quantity = itemDto.Quantity,
                        Discount = itemDto.Discount
                    };

                    order.OrderItems.Add(orderItem);
                }

                // Set the total amount and discount on the order
                order.TotalAmount = totalAmount;
                order.TotalDiscount = totalDiscount;

                // Add the order to the context and save changes
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Commit the transaction
                await transaction.CommitAsync();

                // Map the order entity to an OrderDTO and return it
                return _mapper.Map<OrderDTO>(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while creating the order for user ID {userId}: {ex.Message}");
                await transaction.RollbackAsync();
                throw;
            }
        }




        public async Task<OrderDTO> GetOrderByIdAsync(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id);
            return order == null ? null : _mapper.Map<OrderDTO>(order);
        }







        public async Task<PagedResult<OrderDTO>> GetAllOrdersAsync(PaginationParameters paginationParameters, QueryOptions queryOptions)
        {
            try
            {
                var query = _context.Orders
                    .Include(o => o.OrderItems)
                    .AsQueryable();

                // Apply searching
                if (!string.IsNullOrEmpty(queryOptions.Search))
                {
                    query = query.Where(o => o.Customer.Name.Contains(queryOptions.Search) ||
                                             o.OrderItems.Any(oi => oi.Product.Name.Contains(queryOptions.Search)));
                }

                // Apply filtering by MinAmount and MaxAmount if available
                if (queryOptions.MinAmount.HasValue)
                {
                    query = query.Where(o => o.TotalAmount >= queryOptions.MinAmount.Value);
                }
                if (queryOptions.MaxAmount.HasValue)
                {
                    query = query.Where(o => o.TotalAmount <= queryOptions.MaxAmount.Value);
                }

                // Apply sorting if the specified SortField exists on the Order entity
                if (!string.IsNullOrEmpty(queryOptions.SortField))
                {
                    var propertyInfo = typeof(Order).GetProperty(queryOptions.SortField);
                    if (propertyInfo != null)
                    {
                        query = queryOptions.SortDescending
                            ? query.OrderByDescending(e => EF.Property<object>(e, queryOptions.SortField))
                            : query.OrderBy(e => EF.Property<object>(e, queryOptions.SortField));
                    }
                    else
                    {
                        _logger.LogWarning($"Sort field '{queryOptions.SortField}' does not exist on Order entity.");
                    }
                }

                // Get total count for pagination
                var totalItems = await query.CountAsync();

                // Apply pagination
                var orders = await query
                    .Skip((paginationParameters.PageNumber - 1) * paginationParameters.PageSize)
                    .Take(paginationParameters.PageSize)
                    .ToListAsync();

                var orderDtos = _mapper.Map<List<OrderDTO>>(orders);

                return new PagedResult<OrderDTO>
                {
                    Items = orderDtos,
                    TotalCount = totalItems,
                    PageNumber = paginationParameters.PageNumber,
                    PageSize = paginationParameters.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching all orders.");
                throw;
            }
        }

        public async Task<OrderDTO> UpdateOrderAsync(int id, OrderDTO orderDto)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                throw new KeyNotFoundException($"Order with ID {id} not found.");
            }

            // Update order details
            order.OrderDate = orderDto.OrderDate;
            order.CustomerId = orderDto.CustomerId;
            // Other updates...

            await _context.SaveChangesAsync();
            return _mapper.Map<OrderDTO>(order);
        }

        public async Task<int> DeleteMultipleOrdersAsync(IEnumerable<int> ids)
        {
            var orders = await _context.Orders.Where(o => ids.Contains(o.Id)).ToListAsync();
            _context.Orders.RemoveRange(orders);
            return await _context.SaveChangesAsync();
        }

        public async Task<decimal> CalculateTotalOrderValueAsync(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                throw new KeyNotFoundException($"Order with ID {id} not found.");
            }

            return order.OrderItems.Sum(oi => oi.Quantity * oi.Product.SellingPrice * (1 - oi.Discount / 100));
        }




        #region MyRegion
        public void GenerateInvoicePdf(InvoiceDTO invoice, string filePath)
        {
            try
            {
                // Define custom page size (e.g., receipt width of 80mm)
                var pageSize = new iText.Kernel.Geom.PageSize(226, 600); // Adjust height as needed
                using (var writer = new PdfWriter(filePath))
                using (var pdf = new PdfDocument(writer))
                {
                    pdf.SetDefaultPageSize(pageSize);
                    var document = new iText.Layout.Document(pdf);
                    document.SetMargins(10, 10, 10, 10);

                    // Add store name or header
                    document.Add(new Paragraph("Ziad Store")
                        .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                        .SetFontSize(16)
                        .SetBold());

                    document.Add(new Paragraph("Address Line 1\nAddress Line 2\nPhone: 01022673000 \n _____________________________________")
                        .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                        .SetFontSize(10));

                    // Add invoice details
                    document.Add(new Paragraph($"Invoice ID: {invoice.OrderId}")
                        .SetFontSize(10));
                    document.Add(new Paragraph($"Order Date: {invoice.OrderDate:yyyy-MM-dd}")
                        .SetFontSize(10));
                    document.Add(new Paragraph($"Customer: {invoice.CustomerName}")
                        .SetFontSize(10));
                    document.Add(new Paragraph($"Processed By: {invoice.UserName}")
                        .SetFontSize(10));

                    // Add line separator
                    document.Add(new iText.Layout.Element.LineSeparator(new iText.Kernel.Pdf.Canvas.Draw.SolidLine()));

                    // Add table for invoice items
                    var table = new Table(new float[] { 3, 1, 1, 1, 1 });
                    table.SetWidth(UnitValue.CreatePercentValue(100));
                    table.AddHeaderCell("Product");
                    table.AddHeaderCell("Qty");
                    table.AddHeaderCell("Price");
                    table.AddHeaderCell("Disc");
                    table.AddHeaderCell("Subtotal");

                    foreach (var item in invoice.Items)
                    {
                        table.AddCell(new Paragraph(item.ProductName).SetFontSize(10));
                        table.AddCell(new Paragraph(item.Quantity.ToString()).SetFontSize(10));
                        table.AddCell(new Paragraph(item.UnitPrice.ToString("C")).SetFontSize(10));
                        table.AddCell(new Paragraph(item.Discount.ToString("C")).SetFontSize(10));
                        table.AddCell(new Paragraph(item.SubTotal.ToString("C")).SetFontSize(10));
                    }
                    document.Add(table);

                    // Add totals section
                    document.Add(new Paragraph($"Total Amount: {invoice.TotalAmount:C}")
                        .SetFontSize(10)
                        .SetBold());
                    document.Add(new Paragraph($"Total Discount: {invoice.TotalDiscount:C}")
                        .SetFontSize(10));
                    document.Add(new Paragraph($"Final Amount: {invoice.FinalAmount:C}")
                        .SetFontSize(12)
                        .SetBold());

                    // Add thank you note
                    document.Add(new Paragraph("Thank you for your business!")
                        .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                        .SetFontSize(10)
                        .SetItalic());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating PDF: {ex.Message}");
                throw;
            }
        }






        public async Task<InvoiceDTO> GenerateInvoiceAsync(int orderId)
        {
            try
            {
                // Fetch the order from the primary StoreContext
                var order = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.Id == orderId);


                if (order == null)
                {
                    throw new KeyNotFoundException($"Order with ID {orderId} not found.");
                }

                // Fetch the AppUser from the separate AppIdentityDbContext
                var user = await _appIdentityDbContext.Users
                    .FirstOrDefaultAsync(u => u.Id == order.UserId);

                //var customer = await _context.Customers
                //    .FirstOrDefaultAsync(z=>z.Name == order.Customer.Name);

                // Map order details to the InvoiceDTO
                var invoice = new InvoiceDTO
                {
                    OrderId = order.Id,
                    OrderDate = order.OrderDate,
                    CustomerName = order.Customer.Name ?? "UnKnown Customer",

                    UserName = user?.UserName ?? "Unknown User", // Use the fetched AppUser
                    TotalAmount = order.TotalAmount,
                    TotalDiscount = order.TotalDiscount,
                    FinalAmount = order.TotalAmount - order.TotalDiscount,
                    Items = order.OrderItems.Select(oi => new InvoiceItemDTO
                    {
                        ProductName = oi.Product?.Name ?? "Unknown Product",
                        Quantity = oi.Quantity,
                        UnitPrice = oi.Product?.SellingPrice ?? 0,
                        Discount = oi.Discount,
                        SubTotal = oi.Quantity * (oi.Product?.SellingPrice ?? 0) * (1 - oi.Discount / 100)
                    }).ToList()
                };

                // Return invoice to be used in the controller
                return invoice;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching invoice details for Order ID {orderId}: {ex.Message}");
                throw;
            }
        }

        #endregion


        public async Task PrintInvoiceAsync(int orderId, string printerName)
        {
            try
            {
                // Generate the file path for the invoice
                string targetDirectory = @"D:\System"; // Change as per your requirement
                string filePath = Path.Combine(targetDirectory, $"Invoice_{orderId}.pdf");

                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException("Invoice file does not exist.", filePath);
                }

                // Create and configure the PrintDocument object
                using (PrintDocument printDocument = new PrintDocument())
                {
                    printDocument.PrinterSettings.PrinterName = printerName;

                    printDocument.PrintPage += (sender, args) =>
                    {
                        // Load and print the file
                        using (var image = System.Drawing.Image.FromFile(filePath))
                        {
                            args.Graphics.DrawImage(image, args.PageBounds);
                        }
                    };

                    // Print the document
                    printDocument.Print();
                }

                _logger.LogInformation($"Order {orderId} invoice printed successfully on {printerName}.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while printing invoice for Order ID {orderId}: {ex.Message}");
                throw;
            }
        }


    }
}
