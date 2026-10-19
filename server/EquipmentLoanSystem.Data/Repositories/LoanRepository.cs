using EquipmentLoanSystem.Core.DTOs;
using EquipmentLoanSystem.Core.Entities;
using EquipmentLoanSystem.Core.Enums;
using EquipmentLoanSystem.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EquipmentLoanSystem.Data.Repositories
{
    public class LoanRepository : ILoanRepository
    {
        private readonly AppDbContext _context;

        public LoanRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Loan loan, CancellationToken cancellationToken = default)
        {
            await _context.Loans.AddAsync(loan, cancellationToken);
        }

        public Task<Loan?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.Loans.AsNoTracking()
                .Include(l => l.ItemCopy)
                    .ThenInclude(c => c.Item)
                .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        }

        public Task<Loan?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.Loans
                .Include(l => l.ItemCopy)
                    .ThenInclude(c => c.Item)
                .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        }

        public Task<List<Loan>> GetByMemberAsync(int memberId, bool activeOnly, CancellationToken cancellationToken = default)
        {
            var query = _context.Loans.AsNoTracking().Where(l => l.MemberId == memberId);

            if (activeOnly)
            {
                query = query.Where(l => l.Status == LoanStatus.Active);
            }

            return query
                .Include(l => l.ItemCopy)
                    .ThenInclude(c => c.Item)
                .OrderByDescending(l => l.LoanDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<PagedResult<Loan>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _context.Loans.AsNoTracking();
            var total = await query.CountAsync(cancellationToken);

            var loans = await query
                .OrderByDescending(l => l.LoanDate)
                .ThenByDescending(l => l.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(l => l.ItemCopy)
                    .ThenInclude(c => c.Item)
                .ToListAsync(cancellationToken);

            return new PagedResult<Loan> { TotalCount = total, Page = page, PageSize = pageSize, Items = loans };
        }
    }
}
