using EquipmentLoanSystem.Core.Entities;
using EquipmentLoanSystem.Core.Enums;
using EquipmentLoanSystem.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EquipmentLoanSystem.Data.Repositories
{
    public class ItemCopyRepository : IItemCopyRepository
    {
        private readonly AppDbContext _context;

        public ItemCopyRepository(AppDbContext context)
        {
            _context = context;
        }

        // שתי השאילתות כאן בכוונה בלי AsNoTracking: העותק עומד להתעדכן,
        // ו-EF צריך לזכור את ה-Version המקורי כדי לבדוק התנגשות בשמירה.
        public Task<ItemCopy?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.ItemCopies
                .Include(c => c.Item)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        public Task<ItemCopy?> GetAvailableByItemIdAsync(int itemId, CancellationToken cancellationToken = default)
        {
            return _context.ItemCopies
                .Include(c => c.Item)
                .Where(c => c.ItemId == itemId && c.Status == CopyStatus.Available)
                .OrderBy(c => c.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
