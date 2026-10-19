using EquipmentLoanSystem.Core.DTOs;
using EquipmentLoanSystem.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentLoanSystem.API.Controllers
{
    [Route("api/auth")]
    public class AuthController : ApiControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>הרשמה של משתמש חדש (תמיד בתפקיד User).</summary>
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var result = await _authService.RegisterAsync(dto, cancellationToken);
            if (!result.IsSuccess) return Failure(result);

            return StatusCode(StatusCodes.Status201Created, result.Value);
        }

        /// <summary>התחברות. משתמשי דמו: admin@example.com / user@example.com / user2@example.com, סיסמה 123456</summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var result = await _authService.LoginAsync(dto, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : Failure(result);
        }
    }
}
