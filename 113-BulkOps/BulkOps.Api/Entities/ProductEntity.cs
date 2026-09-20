using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace BulkOps.Api.Entities
{
    public class ProductEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public bool IsActive { get; set; }
    }
}