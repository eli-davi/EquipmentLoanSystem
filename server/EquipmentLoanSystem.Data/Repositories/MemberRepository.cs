using EquipmentLoanSystem.Core.Entities;
using EquipmentLoanSystem.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EquipmentLoanSystem.Data.Repositories
{
    public class MemberRepository : IMemberRepository
    {
        private readonly AppDbContext _context;

        public MemberRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.Members.AnyAsync(member => member.Id == id, cancellationToken);
        }

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        {
            var normalized = email.Trim().ToLowerInvariant();
            return _context.Members.AnyAsync(m => m.Email == normalized, cancellationToken);
        }

        public Task<Member?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var normalized = email.Trim().ToLowerInvariant();
            return _context.Members.AsNoTracking().FirstOrDefaultAsync(m => m.Email == normalized, cancellationToken);
        }

        public async Task AddAsync(Member member, CancellationToken cancellationToken = default)
        {
            await _context.Members.AddAsync(member, cancellationToken);
        }
    }
}
