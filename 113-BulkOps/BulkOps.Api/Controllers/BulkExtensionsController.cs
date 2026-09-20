using BulkOps.Api.Entities;
using BulkOps.Api.Services;
using Bogus;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BulkOps.Api.Controllers
{
    [ApiController]
    [Route("api/bulkext")]
    public class BulkExtensionsController : ControllerBase
    {
        private readonly BulkExtensionsService _service;
        public BulkExtensionsController(BulkExtensionsService service) => _service = service;

        [HttpPost("bulk-insert")]
        public async Task<IActionResult> BulkInsert([FromQuery] int count = 1000)
        {
            var faker = new Faker<ProductEntity>()
                .RuleFor(p => p.Name, f => f.Commerce.ProductName())
                .RuleFor(p => p.Price, f => decimal.Parse(f.Commerce.Price()))
                .RuleFor(p => p.Stock, f => f.Random.Int(1, 100))
                .RuleFor(p => p.IsActive, f => f.Random.Bool());
            
            var products = faker.Generate(count);
            await _service.BulkInsertAsync(products);
            return StatusCode(201, new { count });
        }

        [HttpPut("bulk-update")]
        public async Task<IActionResult> BulkUpdate([FromBody] System.Collections.Generic.List<ProductEntity> products)
        {
            await _service.BulkUpdateAsync(products);
            return Ok();
        }

        [HttpDelete("bulk-delete")]
        public async Task<IActionResult> BulkDelete([FromBody] System.Collections.Generic.List<ProductEntity> products)
        {
            await _service.BulkDeleteAsync(products);
            return Ok();
        }

        [HttpPost("bulk-upsert")]
        public async Task<IActionResult> BulkUpsert([FromBody] System.Collections.Generic.List<ProductEntity> products)
        {
            await _service.BulkInsertOrUpdateAsync(products);
            return Ok();
        }
    }
}