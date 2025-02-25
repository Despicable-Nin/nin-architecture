using espasyo.Application.UseCase.Streets.Commands.CreateStreets;
using espasyo.Application.UseCase.Streets.Queries.GetStreets;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace espasyo.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StreetController(IMediator mediator) : ControllerBase
    {
        
        // GET: api/<StreetController>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesErrorResponseType(typeof(BadRequestResult))]
        public IActionResult Get()
        {
            return Ok(mediator.Send(new GetStreetsQuery()));
        }

        // POST api/<StreetController>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesErrorResponseType(typeof(BadRequestResult))]
        public async Task<IActionResult> Post([FromBody] CreateStreetsCommand command)
        {
            return Created("/api/street", await mediator.Send(command));
        }
    }
}
