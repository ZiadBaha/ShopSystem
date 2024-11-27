﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using ShopSystem.Core.Enums;
using ShopSystem.Core.Models.Entites;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopSystem.Core.Models.Identity
{
    public class AppUser : IdentityUser
    {
        public UserRole UserRole { get; set; }
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string? Image { get; set; }
        [NotMapped]
        public IFormFile? ImageFile { get; set; }



        // Relationships
        public virtual ICollection<Order> Orders { get; set; }

        //public ICollection<Expense> Expenses { get; set; }


    }
}