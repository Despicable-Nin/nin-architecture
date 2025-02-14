using MediatR;
using Microsoft.AspNetCore.Mvc;
using nin.Application.Products.Queries.GetProducts;

namespace nin.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController(IMediator mediator) : ControllerBase
    {
        
        // GET: api/<ProductController>
        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            return Ok(await mediator.Send(new GetProductsQuery()));
        }

    }
}
