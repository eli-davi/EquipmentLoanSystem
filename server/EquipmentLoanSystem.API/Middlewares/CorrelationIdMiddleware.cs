using System.Diagnostics;

namespace EquipmentLoanSystem.API.Middlewares
{
    /// <summary>
    /// מייצר (או מקבל מהלקוח) CorrelationId לכל בקשה, מחזיר אותו ב-header,
    /// ופותח logging scope כך שכל שורת לוג של הבקשה - מכל שכבה - נושאת אותו.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        public const string HeaderName = "X-Correlation-ID";

        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;

        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var incoming)
                                && Guid.TryParse(incoming.ToString(), out var parsed)
                ? parsed.ToString()
                : Guid.NewGuid().ToString();

            context.TraceIdentifier = correlationId;
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[HeaderName] = correlationId;
                return Task.CompletedTask;
            });

            using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
            {
                var stopwatch = Stopwatch.StartNew();

                // רק method ו-path. בלי query string, בלי body ובלי headers (שעלולים להכיל token)
                _logger.LogInformation("Incoming request {Method} {Path}", context.Request.Method, context.Request.Path);

                await _next(context);

                _logger.LogInformation("Completed {Method} {Path} with {StatusCode} in {ElapsedMs} ms",
                    context.Request.Method, context.Request.Path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
