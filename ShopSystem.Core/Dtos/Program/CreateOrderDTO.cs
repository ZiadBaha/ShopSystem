using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopSystem.Core.Dtos.Program
{
    public class CreateOrderDTO
    {
        public int CustomerId { get; set; }  // Assuming this is a string; adjust as needed.
        public List<OrderItemDTO> OrderItems { get; set; }  // List of items in the order.
    }
}
