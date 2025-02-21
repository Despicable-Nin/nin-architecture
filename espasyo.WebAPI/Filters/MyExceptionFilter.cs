using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace espasyo.WebAPI.Filters;

public class MyExceptionFilter(ILogger<MyExceptionFilter> logger) : IExceptionFilter
{
    private readonly ILogger<MyExceptionFilter> _logger = logger;

    public void OnException(ExceptionContext context)
    {

        var exception = context.Exception;
        logger.LogError(exception.Message);
        
        var response = context.HttpContext.Response;
        response.StatusCode = (int)HttpStatusCode.InternalServerError;
        response.ContentType = "application/json";
        
        var errorResponse = new
        {
            Message = "An unexpected error occurred. Please try again later.",
            Detailed = "See logs for more details."
        };

        if (exception is InvalidOperationException && exception.Source == "Microsoft.ML.KMeansClustering")
        {
            //override the status code
            response.StatusCode = (int)HttpStatusCode.BadRequest;
            errorResponse = errorResponse with { Message = "Insufficient number of records."};
        }
      
        context.Result = new JsonResult(errorResponse);

    }
}