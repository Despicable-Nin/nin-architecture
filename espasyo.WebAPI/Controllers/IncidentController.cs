using espasyo.Application.Incidents.Commands.CreateIncident;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace espasyo.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IncidentController( IMediator mediator) : ControllerBase
    {
        
        // POST api/<IncidentController>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CreateIncidentCommand incident)
        {
            var id = await mediator.Send( incident );
            return Created("api/incident", id);
        }
    }
}
