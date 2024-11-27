﻿using ShopSystem.Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopSystem.Core.Dtos.Program
{
    public class ProductDTO
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Product name is required.")]
        [StringLength(100, ErrorMessage = "Product name cannot exceed 100 characters.")]
        public string Name { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Quantity cannot be negative.")]
        public int? Quantity { get; set; }

        public bool? IsStock { get; set; }= true;

        [Required(ErrorMessage = "Price is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
        // Purchase and selling prices 
        public decimal PurchasePrice { get; set; }  // The price at which the product is bought
        public decimal SellingPrice { get; set; }   // The price at which the product is sold

        // Property for profit, calculated based on PurchasePrice and SellingPrice
        [NotMapped]
        public decimal Profit => SellingPrice - PurchasePrice;


        [Required(ErrorMessage = "Category ID is required.")]
        public int CategoryId { get; set; }

        public ProductStatus? Status { get; set; } = ProductStatus.Active;

        public string UniqueNumber { get; set; } = string.Empty;

       
    }
}