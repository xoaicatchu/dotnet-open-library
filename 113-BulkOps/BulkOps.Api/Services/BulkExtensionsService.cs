using BulkOps.Api.Data;
using BulkOps.Api.Entities;
using EFCore.BulkExtensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace BulkOps.Api.Services
{
    public class BulkExtensionsService
    {
        private readonly AppDbContext _db;
        public BulkExtensionsService(AppDbContext db) => _db = db;

        public async Task BulkInsertAsync(List<ProductEntity> products)
        {
            var sw = Stopwatch.StartNew();
            try {
                await _db.BulkInsertAsync(products);
            } catch (Exception) {
                await _db.Products.AddRangeAsync(products);
                await _db.SaveChangesAsync();
            }
            sw.Stop();
        }

        public async Task BulkUpdateAsync(List<ProductEntity> products)
        {
            try {
                await _db.BulkUpdateAsync(products);
            } catch (Exception) {
                try {
                    _db.Products.UpdateRange(products);
                    await _db.SaveChangesAsync();
                } catch (DbUpdateConcurrencyException) { }
            }
        }

        public async Task BulkDeleteAsync(List<ProductEntity> products)
        {
            try {
                await _db.BulkDeleteAsync(products);
            } catch (Exception) {
                try {
                    _db.Products.RemoveRange(products);
                    await _db.SaveChangesAsync();
                } catch (DbUpdateConcurrencyException) { }
            }
        }

        public async Task BulkInsertOrUpdateAsync(List<ProductEntity> products)
        {
            try {
                await _db.BulkInsertOrUpdateAsync(products);
            } catch (Exception) {
                try {
                    foreach (var p in products) {
                        if (p.Id == 0) _db.Products.Add(p);
                        else _db.Products.Update(p);
                    }
                    await _db.SaveChangesAsync();
                } catch (DbUpdateConcurrencyException) { }
            }
        }
    }
}