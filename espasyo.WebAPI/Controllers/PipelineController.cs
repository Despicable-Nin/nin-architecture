using espasyo.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace espasyo.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PipelineController : ControllerBase
{
    private readonly PipelineOrchestratorService _pipeline;

    public PipelineController(PipelineOrchestratorService pipeline)
    {
        _pipeline = pipeline;
    }

    [HttpPost("run")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RunPipeline([FromBody] PipelineRequest request)
    {
        try
        {
            var result = await _pipeline.RunFullPipeline(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                Error = "Pipeline execution failed",
                Detail = ex.Message,
                InnerException = ex.InnerException?.Message
            });
        }
    }
}
