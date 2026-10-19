using System.Text.Json;
using EquipmentLoanSystem.Core.DTOs;

namespace EquipmentLoanSystem.API.Middlewares
{
    /// <summary>
    /// טיפול גלובלי בשגיאות: כל חריגה שלא טופלה מתועדת ברמת Error
    /// ומוחזרת ללקוח כ-JSON אחיד (ErrorResponseDto) - בלי stack trace.
    /// </summary>
    public class ErrorHandlingMiddleware
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                // הלקוח ביטל את הבקשה (CancellationToken) - לא שגיאה של השרת
                _logger.LogInformation("Request {Path} was cancelled by the client (CorrelationId {CorrelationId})",
                    context.Request.Path, context.TraceIdentifier);
                if (!context.Response.HasStarted)
                    context.Response.StatusCode = 499;
            }
            catch (Exception ex)
            {
                using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = context.TraceIdentifier }))
                {
                    _logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
                }

                if (context.Response.HasStarted)
                    throw;

                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json; charset=utf-8";

                var body = new ErrorResponseDto
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Message = "אירעה שגיאה בלתי צפויה בשרת. נסו שוב מאוחר יותר.",
                    CorrelationId = context.TraceIdentifier
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
            }
        }
    }
}
