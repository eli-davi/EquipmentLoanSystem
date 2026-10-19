using AutoMapper;
using EquipmentLoanSystem.Core.Constants;
using EquipmentLoanSystem.Core.DTOs;
using EquipmentLoanSystem.Core.Entities;
using EquipmentLoanSystem.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentLoanSystem.API.Controllers
{
    [Route("api/items")]
    public class ItemsController : ApiControllerBase
    {
        private readonly IItemService _itemService;
        private readonly IMapper _mapper;

        public ItemsController(IItemService itemService, IMapper mapper)
        {
            _itemService = itemService;
            _mapper = mapper;
        }

        /// <summary>רשימת ציוד עם pagination, חיפוש, סינון ומיון (פתוח לכולם).</summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<ItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetItems([FromQuery] ItemQueryDto query, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var result = await _itemService.GetItemsAsync(query, cancellationToken);

            return Ok(new PagedResult<ItemDto>
            {
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize,
                Items = _mapper.Map<List<ItemDto>>(result.Items)
            });
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ItemDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetItem([FromRoute] int id, CancellationToken cancellationToken)
        {
            var result = await _itemService.GetByIdAsync(id, cancellationToken);
            return result.IsSuccess ? Ok(_mapper.Map<ItemDto>(result.Value)) : Failure(result);
        }

        [HttpGet("~/api/categories")]
        [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
        {
            var categories = await _itemService.GetCategoriesAsync(cancellationToken);
            return Ok(_mapper.Map<List<CategoryDto>>(categories));
        }

        /// <summary>הוספת פריט - Admin בלבד.</summary>
        [HttpPost]
        [Authorize(Roles = Roles.Admin)]
        [ProducesResponseType(typeof(ItemDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateItem([FromBody] CreateItemDto dto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var item = _mapper.Map<Item>(dto);
            var result = await _itemService.CreateAsync(item, dto.CategoryIds, dto.NumberOfCopies, cancellationToken);
            if (!result.IsSuccess) return Failure(result);

            var itemDto = _mapper.Map<ItemDto>(result.Value);
            return CreatedAtAction(nameof(GetItem), new { id = itemDto.Id }, itemDto); // 201 + Location
        }

        /// <summary>עדכון פריט - Admin בלבד.</summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = Roles.Admin)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateItem([FromRoute] int id, [FromBody] UpdateItemDto dto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var result = await _itemService.UpdateAsync(id, dto, cancellationToken);
            return result.IsSuccess ? NoContent() : Failure(result);
        }

        /// <summary>מחיקת פריט - Admin בלבד. פריט עם היסטוריית השאלות לא נמחק (409).</summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = Roles.Admin)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteItem([FromRoute] int id, CancellationToken cancellationToken)
        {
            var result = await _itemService.DeleteAsync(id, cancellationToken);
            return result.IsSuccess ? NoContent() : Failure(result);
        }
    }
}
