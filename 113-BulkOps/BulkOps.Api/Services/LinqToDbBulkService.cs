using BulkOps.Api.Data;
using BulkOps.Api.Entities;
using LinqToDB.Data;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace BulkOps.Api.Services
{
    public class LinqToDbBulkService
    {
        private readonly AppDbContext _db;
        public LinqToDbBulkService(AppDbContext db) => _db = db;

        public async Task<long> BulkCopyAsync(List<ProductEntity> products)
        {
            try {
                var result = await LinqToDBForEFTools.BulkCopyAsync(_db, new BulkCopyOptions {
                    BulkCopyType = BulkCopyType.Default,
                    CheckConstraints = false,
                }, products.Select(p => new ProductEntity {
                    Name = p.Name, Price = p.Price, Stock = p.Stock, IsActive = p.IsActive
                }));
                return result.RowsCopied;
            } catch (Exception) {
                // fallback
                await _db.Products.AddRangeAsync(products);
                var count = await _db.SaveChangesAsync();
                return count;
            }
        }
    }
}