using EquipmentLoanSystem.Core.DTOs;
using EquipmentLoanSystem.Core.Entities;

namespace EquipmentLoanSystem.Core.Interfaces
{
    public interface IItemRepository
    {
        /// <summary>סינון, מיון ו-Skip/Take מתבצעים בתוך השאילתה מול בסיס הנתונים.</summary>
        Task<PagedResult<Item>> GetPagedAsync(ItemQueryDto query, CancellationToken cancellationToken = default);

        /// <summary>קריאה בלבד (AsNoTracking) כולל עותקים וקטגוריות.</summary>
        Task<Item?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>טעינה עם מעקב, לצורך עדכון/מחיקה.</summary>
        Task<Item?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default);

        Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
        Task<bool> HasLoansAsync(int itemId, CancellationToken cancellationToken = default);
        Task AddAsync(Item item, CancellationToken cancellationToken = default);
        void Remove(Item item);
    }

    public interface ICategoryRepository
    {
        Task<List<Category>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<List<Category>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
    }

    public interface IItemCopyRepository
    {
        /// <summary>טעינה עם מעקב (tracking) - כדי ש-EF ישמור את ערך ה-Version המקורי לבדיקת ההתנגשות.</summary>
        Task<ItemCopy?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>עותק פנוי ראשון של פריט, עם מעקב.</summary>
        Task<ItemCopy?> GetAvailableByItemIdAsync(int itemId, CancellationToken cancellationToken = default);
    }

    public interface ILoanRepository
    {
        Task AddAsync(Loan loan, CancellationToken cancellationToken = default);

        /// <summary>קריאה בלבד, כולל ItemCopy ו-Item (Include + ThenInclude).</summary>
        Task<Loan?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>טעינה עם מעקב לצורך החזרה.</summary>
        Task<Loan?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default);

        Task<List<Loan>> GetByMemberAsync(int memberId, bool activeOnly, CancellationToken cancellationToken = default);
        Task<PagedResult<Loan>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    }

    public interface IMemberRepository
    {
        Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
        Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
        Task<Entities.Member?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task AddAsync(Entities.Member member, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// שמירה של כל השינויים ב-DbContext (scoped) בטרנזקציה אחת.
    /// זורק ConcurrencyConflictException כאשר EF זורק DbUpdateConcurrencyException.
    /// </summary>
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
