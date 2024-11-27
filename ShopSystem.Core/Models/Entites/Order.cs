using ShopSystem.Core.Models.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopSystem.Core.Models.Entites
{
    public class Order : BaseEntity
    {
        //public int Id { get; set; } // Primary key
        public DateTime OrderDate { get; set; }
        public int CustomerId { get; set; } // Foreign key
        public string UserId { get; set; } // Foreign key

        [ForeignKey("UserId")]
        public virtual AppUser User { get; set; } // Assuming there's a User entity
        public decimal TotalDiscount { get; set; } // Total discount for the order
        public decimal TotalAmount { get; set; } // Total amount after discounts

        // Navigation properties
        public Customer Customer { get; set; } // Assuming there's a Customer entity
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        
    }
}