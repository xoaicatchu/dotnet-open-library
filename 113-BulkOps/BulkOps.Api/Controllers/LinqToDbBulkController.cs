using BulkOps.Api.Entities;
using BulkOps.Api.Services;
using Bogus;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BulkOps.Api.Controllers
{
    [ApiController]
    [Route("api/linq2db")]
    public class LinqToDbBulkController : ControllerBase
    {
        private readonly LinqToDbBulkService _service;
        public LinqToDbBulkController(LinqToDbBulkService service) => _service = service;

        [HttpPost("bulk-insert")]
        public async Task<IActionResult> BulkInsert([FromQuery] int count = 1000)
        {
            var faker = new Faker<ProductEntity>()
                .RuleFor(p => p.Name, f => f.Commerce.ProductName())
                .RuleFor(p => p.Price, f => decimal.Parse(f.Commerce.Price()))
                .RuleFor(p => p.Stock, f => f.Random.Int(1, 100))
                .RuleFor(p => p.IsActive, f => f.Random.Bool());
            
            var products = faker.Generate(count);
            var rowsCopied = await _service.BulkCopyAsync(products);
            return Ok(new { rowsCopied });
        }
    }
}