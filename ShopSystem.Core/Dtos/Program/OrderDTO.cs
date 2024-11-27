namespace ShopSystem.Core.Dtos.Program
{
    public class OrderDTO
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public int CustomerId { get; set; }
        public string UserId { get; set; }
        public decimal TotalDiscount { get; set; } // Automatically calculated discount
        public decimal TotalAmount { get; set; } // Total amount after applying discount
        public List<OrderItemDTO> OrderItems { get; set; }
    }
}