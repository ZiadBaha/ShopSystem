using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ShopSystem.Core.Dtos.Program.Ivoice;
using ShopSystem.Core.Dtos.Program;
using ShopSystem.Core.Dtos;
using ShopSystem.Core.Errors;
using ShopSystem.Core.Models.Identity;
using ShopSystem.Core.Models;
using ShopSystem.Core.Services.Programe;
using System.Security.Claims;

using AutoMapper;

namespace Shop_System.Controllers
{

    //[Authorize]
    public class OrderController : ApiBaseController
    {
        private readonly IOrderRepository _orderService;
        private readonly ILogger<OrderController> _logger;
        private readonly UserManager<AppUser> _userManager;
        private readonly IMapper _mapper;

        public OrderController(IOrderRepository orderService, ILogger<OrderController> logger,
            UserManager<AppUser> userManager, IMapper mapper)
        {
            _orderService = orderService;
            _logger = logger;
            _userManager = userManager;
            _mapper = mapper;
        }

        // Create a new order
        // POST: api/orders
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDTO createOrderDto)
        {
            // Validate the request data
            if (createOrderDto == null || !ModelState.IsValid)
            {
                return BadRequest(new ContentContainer<string>(null, "Invalid order data."));
            }

            try
            {
                var email = User.FindFirst(ClaimTypes.Email)?.Value;
                if (email == null)
                {
                    return BadRequest(new ApiResponse(400, "Invalid user"));
                }
                var user = await _userManager.FindByEmailAsync(email);

                if (string.IsNullOrEmpty(user.Id))
                {
                    return Unauthorized(new ContentContainer<string>(null, "User ID is missing."));
                }

                // Call the service to create the order
                var createdOrder = await _orderService.CreateOrderAsync(createOrderDto, user.Id);

                // Return a 201 Created response with the created order
                var response = new ContentContainer<OrderDTO>(createdOrder, "Order created successfully.");
                return CreatedAtAction(nameof(GetOrderById), new { id = createdOrder.Id }, response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "An error occurred while creating the order.");
                return NotFound(new ContentContainer<string>(null, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred.");
                return StatusCode(500, new ContentContainer<string>(null, "An unexpected error occurred while processing the order."));
            }
        }




        // Get an order by ID
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            try
            {
                var order = await _orderService.GetOrderByIdAsync(id);
                if (order == null)
                {
                    return NotFound(new ContentContainer<string>(null, "Order not found"));
                }
                return Ok(new ContentContainer<OrderDTO>(order));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching the order.");
                return StatusCode(500, new ContentContainer<string>(null, "An error occurred while fetching the order."));
            }
        }

        // Get all orders with pagination and query options
        [HttpGet]
        public async Task<IActionResult> GetAllOrders([FromQuery] PaginationParameters paginationParameters, [FromQuery] QueryOptions queryOptions)
        {
            try
            {
                var orders = await _orderService.GetAllOrdersAsync(paginationParameters, queryOptions);
                return Ok(new ContentContainer<PagedResult<OrderDTO>>(orders));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching all orders.");
                return StatusCode(500, new ContentContainer<string>(null, "An error occurred while fetching all orders."));
            }
        }

        // Update an order
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateOrder(int id, [FromBody] OrderDTO orderDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ContentContainer<string>(null, "Invalid input data"));
            }

            try
            {
                var updatedOrder = await _orderService.UpdateOrderAsync(id, orderDto);
                return Ok(new ContentContainer<OrderDTO>(updatedOrder));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Order not found for update.");
                return NotFound(new ContentContainer<string>(null, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating the order.");
                return StatusCode(500, new ContentContainer<string>(null, "An error occurred while updating the order."));
            }
        }

        // Delete multiple orders
        [HttpDelete]
        public async Task<IActionResult> DeleteMultipleOrders([FromBody] IEnumerable<int> ids)
        {
            if (ids == null || !ids.Any())
            {
                return BadRequest(new ContentContainer<string>(null, "Order IDs must be provided"));
            }

            try
            {
                var deletedCount = await _orderService.DeleteMultipleOrdersAsync(ids);
                return Ok(new ContentContainer<int>(deletedCount, $"{deletedCount} orders deleted successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting orders.");
                return StatusCode(500, new ContentContainer<string>(null, "An error occurred while deleting the orders."));
            }
        }

        // Calculate the total value of an order
        [HttpGet("{id:int}/calculate-total")]
        public async Task<IActionResult> CalculateTotalOrderValue(int id)
        {
            try
            {
                var totalValue = await _orderService.CalculateTotalOrderValueAsync(id);
                return Ok(new ContentContainer<decimal>(totalValue, "Total order value calculated successfully."));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Order not found for total value calculation.");
                return NotFound(new ContentContainer<string>(null, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while calculating total order value.");
                return StatusCode(500, new ContentContainer<string>(null, "An error occurred while calculating total order value."));
            }
        }



        [HttpGet("{id:int}/invoice")]
        public async Task<IActionResult> GetOrderInvoice(int id)
        {
            try
            {
                var invoice = await _orderService.GenerateInvoiceAsync(id);
                return Ok(new ContentContainer<InvoiceDTO>(invoice, "Invoice generated successfully."));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Order not found for invoice generation.");
                return NotFound(new ContentContainer<string>(null, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while generating the invoice.");
                return StatusCode(500, new ContentContainer<string>(null, "An error occurred while generating the invoice."));
            }
        }




        [HttpGet("generate/{orderId}")]
        public async Task<IActionResult> GenerateInvoice(int orderId)
        {
            try
            {
                // Generate the invoice details
                var invoice = await _orderService.GenerateInvoiceAsync(orderId);

                if (invoice == null)
                {
                    return NotFound($"Order with ID {orderId} not found.");
                }

                // Define the file path to save the PDF
                var targetDirectory = @"D:\System"; // Use your desired directory here

                // Ensure the directory exists
                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                var filePath = Path.Combine(targetDirectory, $"Invoice_{orderId}.pdf");

                // Log the file path for debugging
                _logger.LogInformation($"Saving invoice to: {filePath}");

                // Generate the PDF
                _orderService.GenerateInvoicePdf(invoice, filePath);

                // Check if the file was successfully created
                if (!System.IO.File.Exists(filePath))
                {
                    return StatusCode(500, "Failed to generate the invoice PDF.");
                }

                // Return the file as a downloadable response
                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                return File(fileBytes, "application/pdf", Path.GetFileName(filePath));
            }
            catch (Exception ex)
            {
                // Log the exception and return an error response
                _logger.LogError(ex, $"An error occurred while generating the invoice for order ID {orderId}.");
                return StatusCode(500, $"An error occurred while generating the invoice: {ex.Message}");
            }
        }






        [HttpPost("print/{orderId}")]
        public async Task<IActionResult> PrintInvoice(int orderId, [FromQuery] string printerName)
        {
            try
            {
                if (string.IsNullOrEmpty(printerName))
                {
                    return BadRequest("Printer name is required.");
                }

                await _orderService.PrintInvoiceAsync(orderId, printerName);
                return Ok($"Order {orderId} has been sent to the printer {printerName}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while printing the invoice for order ID {orderId}.");
                return StatusCode(500, $"An error occurred while printing the invoice: {ex.Message}");
            }
        }
    }




}
