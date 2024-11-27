namespace ShopSystem.Core.Dtos.Program
{
    public class OrderItemDTO
    {
        public int ProductId { get; set; }  // Product identifier
        public int Quantity { get; set; }  // Quantity of the product
        public decimal Discount { get; set; }  // Discount applied to the item
        public decimal SubTotal { get; set; }  // Subtotal for the order item
    }
}