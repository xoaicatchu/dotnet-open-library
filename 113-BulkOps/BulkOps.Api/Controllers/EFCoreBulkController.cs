using BulkOps.Api.Entities;
using BulkOps.Api.Services;
using Bogus;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Linq;

namespace BulkOps.Api.Controllers
{
    [ApiController]
    [Route("api/efcore")]
    public class EFCoreBulkController : ControllerBase
    {
        private readonly EFCoreBulkService _service;
        public EFCoreBulkController(EFCoreBulkService service) => _service = service;

        [HttpPost("bulk-insert")]
        public async Task<IActionResult> BulkInsert([FromQuery] int count = 1000)
        {
            var faker = new Faker<ProductEntity>()
                .RuleFor(p => p.Name, f => f.Commerce.ProductName())
                .RuleFor(p => p.Price, f => decimal.Parse(f.Commerce.Price()))
                .RuleFor(p => p.Stock, f => f.Random.Int(1, 100))
                .RuleFor(p => p.IsActive, f => f.Random.Bool());
            
            var products = faker.Generate(count);
            await _service.BulkInsertTraditionalAsync(products);
            return StatusCode(201, new { count });
        }

        [HttpPut("bulk-update-price")]
        public async Task<IActionResult> BulkUpdatePrice([FromQuery] decimal multiplier = 1.1m)
        {
            var rowsAffected = await _service.BulkUpdatePriceAsync(multiplier);
            return Ok(new { rowsAffected });
        }

        [HttpDelete("bulk-delete-inactive")]
        public async Task<IActionResult> BulkDeleteInactive()
        {
            var rowsAffected = await _service.BulkDeleteInactiveAsync();
            return Ok(new { rowsAffected });
        }
        
        [HttpGet("products")]
        public async Task<IActionResult> GetProducts([FromServices] BulkOps.Api.Data.AppDbContext db)
        {
            return Ok(db.Products.Take(100).ToList());
        }
    }
}