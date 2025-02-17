using espasyo.Application.Products.Commands.CreateProduct;
using espasyo.Application.Products.Queries.GetProducts;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace espasyo.WebAPI.Controllers
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
        
        [HttpPost]
        public async Task<IActionResult> AddProduct([FromBody] CreateProductCommand command )
        {
            return Ok(await mediator.Send(command));
        }

    }
}
