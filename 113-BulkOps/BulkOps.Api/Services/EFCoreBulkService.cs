using BulkOps.Api.Data;
using BulkOps.Api.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace BulkOps.Api.Services
{
    public class EFCoreBulkService
    {
        private readonly AppDbContext _db;
        public EFCoreBulkService(AppDbContext db) => _db = db;

        public async Task<int> BulkUpdatePriceAsync(decimal multiplier)
        {
            var sw = Stopwatch.StartNew();
            var count = await _db.Products
                .Where(p => p.IsActive)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Price, p => p.Price * multiplier));
            sw.Stop();
            return count;
        }

        public async Task<int> BulkDeleteInactiveAsync()
        {
            return await _db.Products
                .Where(p => !p.IsActive)
                .ExecuteDeleteAsync();
        }

        public async Task BulkInsertTraditionalAsync(List<ProductEntity> products)
        {
            await _db.Products.AddRangeAsync(products);
            await _db.SaveChangesAsync();
        }
    }
}