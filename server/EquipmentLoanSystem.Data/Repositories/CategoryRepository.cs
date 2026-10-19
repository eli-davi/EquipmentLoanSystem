using EquipmentLoanSystem.Core.Entities;
using EquipmentLoanSystem.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EquipmentLoanSystem.Data.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly AppDbContext _context;

        public CategoryRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<List<Category>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return _context.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync(cancellationToken);
        }

        // עם מעקב - כי הן מתווספות לקשר many-to-many של פריט
        public Task<List<Category>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
        {
            var idList = ids.Distinct().ToList();
            return _context.Categories.Where(c => idList.Contains(c.Id)).ToListAsync(cancellationToken);
        }
    }
}
