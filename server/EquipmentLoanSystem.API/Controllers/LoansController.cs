using AutoMapper;
using EquipmentLoanSystem.API.Extensions;
using EquipmentLoanSystem.Core.Constants;
using EquipmentLoanSystem.Core.DTOs;
using EquipmentLoanSystem.Core.Enums;
using EquipmentLoanSystem.Core.Interfaces;
using EquipmentLoanSystem.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentLoanSystem.API.Controllers
{
    [Authorize] // כל ה-endpoints כאן דורשים token
    [Route("api/loans")]
    public class LoansController : ApiControllerBase
    {
        private readonly ILoanService _loanService;
        private readonly IMapper _mapper;

        public LoansController(ILoanService loanService, IMapper mapper)
        {
            _loanService = loanService;
            _mapper = mapper;
        }

        /// <summary>
        /// השאלת פריט. 201 - "הפריט הושאל בהצלחה". 409 - "זה כבר מושאל" (אין עותק פנוי, או שמשתמש אחר תפס אותו באותו רגע).
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(LoanDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateLoan([FromBody] LoanRequestDto dto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var result = await _loanService.CreateLoanAsync(User.GetMemberId(), dto, cancellationToken);
            if (!result.IsSuccess) return Failure(result);

            var loanDto = _mapper.Map<LoanDto>(result.Value);
            loanDto.Message = LoanService.LoanSuccessMessage;
            return CreatedAtAction(nameof(GetLoan), new { id = loanDto.Id }, loanDto);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(LoanDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLoan([FromRoute] int id, CancellationToken cancellationToken)
        {
            var result = await _loanService.GetByIdAsync(id, User.GetMemberId(), User.IsAdmin(), cancellationToken);
            return result.IsSuccess ? Ok(_mapper.Map<LoanDto>(result.Value)) : Failure(result);
        }

        /// <summary>ההשאלות של המשתמש המחובר.</summary>
        [HttpGet("mine")]
        [ProducesResponseType(typeof(List<LoanDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyLoans([FromQuery] bool activeOnly = true, CancellationToken cancellationToken = default)
        {
            var loans = await _loanService.GetMemberLoansAsync(User.GetMemberId(), activeOnly, cancellationToken);
            return Ok(_mapper.Map<List<LoanDto>>(loans));
        }

        /// <summary>כל ההשאלות במערכת - Admin בלבד (User מקבל 403).</summary>
        [HttpGet]
        [Authorize(Roles = Roles.Admin)]
        [ProducesResponseType(typeof(PagedResult<LoanDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllLoans(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            if (page < 1 || pageSize < 1 || pageSize > 100)
                return BadRequest(new ErrorResponseDto { Status = 400, Message = "פרמטרי עמוד לא תקינים", CorrelationId = HttpContext.TraceIdentifier });

            var result = await _loanService.GetAllLoansAsync(page, pageSize, cancellationToken);
            return Ok(new PagedResult<LoanDto>
            {
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize,
                Items = _mapper.Map<List<LoanDto>>(result.Items)
            });
        }

        /// <summary>החזרת פריט: PATCH עם { "status": "Returned" }. בעלי ההשאלה או Admin.</summary>
        [HttpPatch("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateLoanStatus([FromRoute] int id, [FromBody] UpdateLoanStatusDto dto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            if (dto.Status != LoanStatus.Returned)
                return BadRequest(new ErrorResponseDto { Status = 400, Message = "ניתן לעדכן השאלה רק לסטטוס Returned", CorrelationId = HttpContext.TraceIdentifier });

            var result = await _loanService.ReturnLoanAsync(id, User.GetMemberId(), User.IsAdmin(), cancellationToken);
            return result.IsSuccess ? NoContent() : Failure(result);
        }
    }
}
