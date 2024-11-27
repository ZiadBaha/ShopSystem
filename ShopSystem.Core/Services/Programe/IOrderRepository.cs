using ShopSystem.Core.Dtos.Program.Ivoice;
using ShopSystem.Core.Dtos.Program;
using ShopSystem.Core.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopSystem.Core.Services.Programe
{
    public interface IOrderRepository
    {
        Task<OrderDTO> CreateOrderAsync(CreateOrderDTO createOrderDto, string userId);
        Task<OrderDTO> GetOrderByIdAsync(int id);
        Task<PagedResult<OrderDTO>> GetAllOrdersAsync(PaginationParameters paginationParameters, QueryOptions queryOptions);
        Task<OrderDTO> UpdateOrderAsync(int id, OrderDTO orderDto);
        Task<int> DeleteMultipleOrdersAsync(IEnumerable<int> ids);
        Task<decimal> CalculateTotalOrderValueAsync(int id);



        // aboout print invoice 

        Task<InvoiceDTO> GenerateInvoiceAsync(int orderId);
        void GenerateInvoicePdf(InvoiceDTO invoice, string filePath);       
        Task PrintInvoiceAsync(int orderId, string printerName);


    }
}
