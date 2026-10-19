using EquipmentLoanSystem.Core.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentLoanSystem.API.Controllers
{
    [ApiController]
    [Produces("application/json")]
    public abstract class ApiControllerBase : ControllerBase
    {
        /// <summary>מתרגם Result שנכשל לסטטוס HTTP עם JSON אחיד.</summary>
        protected IActionResult Failure<T>(Result<T> result)
        {
            return StatusCode(result.ErrorCode, new ErrorResponseDto
            {
                Status = result.ErrorCode,
                Message = result.ErrorMessage ?? "הבקשה נכשלה",
                CorrelationId = HttpContext.TraceIdentifier
            });
        }
    }
}
