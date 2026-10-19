using EquipmentLoanSystem.Core.DTOs;
using EquipmentLoanSystem.Core.Entities;
using EquipmentLoanSystem.Core.Enums;
using EquipmentLoanSystem.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace EquipmentLoanSystem.Service.Services
{
    public class ItemService : IItemService
    {
        private readonly IItemRepository _itemRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ItemService> _logger;

        public ItemService(
            IItemRepository itemRepository,
            ICategoryRepository categoryRepository,
            IUnitOfWork unitOfWork,
            ILogger<ItemService> logger)
        {
            _itemRepository = itemRepository;
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public Task<PagedResult<Item>> GetItemsAsync(ItemQueryDto query, CancellationToken cancellationToken = default)
            => _itemRepository.GetPagedAsync(query, cancellationToken);

        public async Task<Result<Item>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var item = await _itemRepository.GetByIdAsync(id, cancellationToken);
            return item is null ? Result<Item>.NotFound("הפריט לא נמצא") : Result<Item>.Success(item);
        }

        public Task<List<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default)
            => _categoryRepository.GetAllAsync(cancellationToken);

        public async Task<Result<Item>> CreateAsync(Item item, IEnumerable<int> categoryIds, int numberOfCopies, CancellationToken cancellationToken = default)
        {
            var categoriesResult = await LoadCategoriesAsync(categoryIds, cancellationToken);
            if (!categoriesResult.IsSuccess)
                return Result<Item>.Fail(categoriesResult.ErrorMessage!, categoriesResult.ErrorCode);

            item.Categories = categoriesResult.Value!;

            var prefix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            item.Copies = Enumerable.Range(1, numberOfCopies)
                .Select(n => new ItemCopy { SerialNumber = $"SN-{prefix}-{n:D2}", Status = CopyStatus.Available })
                .ToList();

            await _itemRepository.AddAsync(item, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Item {ItemId} created with {Copies} copies", item.Id, numberOfCopies);
            return Result<Item>.Success(item);
        }

        public async Task<Result<bool>> UpdateAsync(int id, UpdateItemDto dto, CancellationToken cancellationToken = default)
        {
            var item = await _itemRepository.GetForUpdateAsync(id, cancellationToken);
            if (item is null)
                return Result<bool>.NotFound("הפריט לא נמצא");

            var categoriesResult = await LoadCategoriesAsync(dto.CategoryIds, cancellationToken);
            if (!categoriesResult.IsSuccess)
                return Result<bool>.Fail(categoriesResult.ErrorMessage!, categoriesResult.ErrorCode);

            item.Name = dto.Name.Trim();
            item.Description = dto.Description?.Trim() ?? string.Empty;
            item.Categories.Clear();
            foreach (var category in categoriesResult.Value!)
                item.Categories.Add(category);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Item {ItemId} updated", id);
            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var item = await _itemRepository.GetForUpdateAsync(id, cancellationToken);
            if (item is null)
                return Result<bool>.NotFound("הפריט לא נמצא");

            if (await _itemRepository.HasLoansAsync(id, cancellationToken))
                return Result<bool>.Conflict("לא ניתן למחוק פריט שיש לו היסטוריית השאלות");

            _itemRepository.Remove(item);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Item {ItemId} deleted", id);
            return Result<bool>.Success(true);
        }

        private async Task<Result<List<Category>>> LoadCategoriesAsync(IEnumerable<int>? categoryIds, CancellationToken cancellationToken)
        {
            var ids = (categoryIds ?? Enumerable.Empty<int>()).Distinct().ToList();
            if (ids.Count == 0)
                return Result<List<Category>>.Success(new List<Category>());

            var categories = await _categoryRepository.GetByIdsAsync(ids, cancellationToken);
            if (categories.Count != ids.Count)
                return Result<List<Category>>.Fail("אחת או יותר מהקטגוריות לא קיימות");

            return Result<List<Category>>.Success(categories);
        }
    }
}
