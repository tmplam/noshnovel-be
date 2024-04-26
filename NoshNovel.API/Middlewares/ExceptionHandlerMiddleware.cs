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
            catch (Exception ex)
            {
                var errorId = Guid.NewGuid();

                // Log this Exception
                logger.LogError(ex, $"{errorId} : {ex.Message}");

                // Return a custom error response
                httpContext.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
                httpContext.Response.ContentType = "application/json";

                var error = new
                {
                    Id = errorId,
                    RequestUrl = httpContext.Request.Path,
                    ErrorMessage = "Something went wrong! We are looking into resolving this!",
                };

                await httpContext.Response.WriteAsJsonAsync(error);
            }
        }
    }
}
