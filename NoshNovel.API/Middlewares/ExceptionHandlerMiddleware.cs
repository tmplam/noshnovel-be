using NoshNovel.Plugin.Strategies.Exeptions;
using System.Net;

namespace NoshNovel.API.Middlewares
{
    public class ExceptionHandlerMiddleware
    {
        private readonly ILogger logger;
        private readonly RequestDelegate next;

        public ExceptionHandlerMiddleware(ILogger<ExceptionHandlerMiddleware> logger, RequestDelegate next)
        {
            this.logger = logger;
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                // If anything happens during the call
                await next(httpContext);
            }
            catch(RequestExeption ex)
            {
                httpContext.Response.StatusCode = (int) ex.StatusCode;
                httpContext.Response.ContentType = "application/json";

                var error = new
                {
                    ErrorId = Guid.NewGuid(),
                    RequestUrl = $"{httpContext.Request.Path}{httpContext.Request.QueryString}",
                    ErrorMessage = ex.Message,
                };

                await httpContext.Response.WriteAsJsonAsync(error);
            }
            catch (Exception ex)
            {
                var errorId = Guid.NewGuid();
                
                // Log this unhandled Exception
                logger.LogError(ex, $"{errorId} : {ex.Message}");

                // Return a custom error response
                httpContext.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
                httpContext.Response.ContentType = "application/json";

                var error = new
                {
                    ErrorId = errorId,
                    RequestUrl = $"{httpContext.Request.Path}{httpContext.Request.QueryString}",
                    ErrorMessage = ex.Message,
                };

                await httpContext.Response.WriteAsJsonAsync(error);
            }
        }
    }
}
