namespace ShopSystem.Core.Models.Entites
{
    public class OrderItem : BaseEntity
    {
        public int Id { get; set; } // Primary key
        public int OrderId { get; set; } // Foreign key
        public int ProductId { get; set; } // Foreign key
        public int Quantity { get; set; } // Quantity of the product
        public decimal Discount { get; set; } // Discount for this item

        // Navigation properties
        public Order Order { get; set; }
        public Product Product { get; set; } // Assuming there's a Product entity

    }
}