using EquipmentLoanSystem.Core.DTOs;
using EquipmentLoanSystem.Core.Entities;
using EquipmentLoanSystem.Core.Enums;
using EquipmentLoanSystem.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EquipmentLoanSystem.Data.Repositories
{
    public class ItemRepository : IItemRepository
    {
        private readonly AppDbContext _context;

        public ItemRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<Item>> GetPagedAsync(ItemQueryDto query, CancellationToken cancellationToken = default)
        {
            IQueryable<Item> items = _context.Items.AsNoTracking();

            // ---------- סינון ----------
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                // ב-PostgreSQL השוואת טקסט רגישה לאותיות גדולות/קטנות, לכן משווים באותיות קטנות
                var term = query.Search.Trim().ToLower();
                items = items.Where(i => i.Name.ToLower().Contains(term) || i.Description.ToLower().Contains(term));
            }

            if (query.CategoryId.HasValue)
            {
                items = items.Where(i => i.Categories.Any(c => c.Id == query.CategoryId.Value));
            }

            if (query.OnlyAvailable)
            {
                items = items.Where(i => i.Copies.Any(c => c.Status == CopyStatus.Available));
            }

            // ספירה בבסיס הנתונים, לפני החיתוך
            var totalCount = await items.CountAsync(cancellationToken);

            // ---------- מיון ----------
            items = (query.SortBy, query.Descending) switch
            {
                ("name", false) => items.OrderBy(i => i.Name).ThenBy(i => i.Id),
                ("name", true) => items.OrderByDescending(i => i.Name).ThenBy(i => i.Id),
                ("available", false) => items.OrderBy(i => i.Copies.Count(c => c.Status == CopyStatus.Available)).ThenBy(i => i.Id),
                ("available", true) => items.OrderByDescending(i => i.Copies.Count(c => c.Status == CopyStatus.Available)).ThenBy(i => i.Id),
                (_, true) => items.OrderByDescending(i => i.Id),
                _ => items.OrderBy(i => i.Id)
            };

            // ---------- Pagination אמיתי: Skip/Take בתוך השאילתה ----------
            var pageItems = await items
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Include(i => i.Copies)
                .Include(i => i.Categories)
                .AsSplitQuery() // מונע "פיצוץ קרטזי" בטעינת שני אוספים, ובלי N+1
                .ToListAsync(cancellationToken);

            return new PagedResult<Item>
            {
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize,
                Items = pageItems
            };
        }

        public Task<Item?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.Items.AsNoTracking()
                .Include(i => i.Copies)
                .Include(i => i.Categories)
                .AsSplitQuery()
                .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        }

        public Task<Item?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.Items
                .Include(i => i.Categories)
                .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        }

        public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.Items.AnyAsync(i => i.Id == id, cancellationToken);
        }

        public Task<bool> HasLoansAsync(int itemId, CancellationToken cancellationToken = default)
        {
            return _context.Loans.AnyAsync(l => l.ItemCopy.ItemId == itemId, cancellationToken);
        }

        public async Task AddAsync(Item item, CancellationToken cancellationToken = default)
        {
            await _context.Items.AddAsync(item, cancellationToken);
        }

        public void Remove(Item item)
        {
            _context.Items.Remove(item);
        }
    }
}
