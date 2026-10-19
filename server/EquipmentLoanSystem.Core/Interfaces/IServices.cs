using EquipmentLoanSystem.Core.DTOs;
using EquipmentLoanSystem.Core.Entities;

namespace EquipmentLoanSystem.Core.Interfaces
{
    public interface IItemService
    {
        Task<PagedResult<Item>> GetItemsAsync(ItemQueryDto query, CancellationToken cancellationToken = default);
        Task<Result<Item>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<List<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default);
        Task<Result<Item>> CreateAsync(Item item, IEnumerable<int> categoryIds, int numberOfCopies, CancellationToken cancellationToken = default);
        Task<Result<bool>> UpdateAsync(int id, UpdateItemDto dto, CancellationToken cancellationToken = default);
        Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default);
    }

    public interface ILoanService
    {
        Task<Result<Loan>> CreateLoanAsync(int memberId, LoanRequestDto dto, CancellationToken cancellationToken = default);
        Task<Result<Loan>> ReturnLoanAsync(int loanId, int memberId, bool isAdmin, CancellationToken cancellationToken = default);
        Task<Result<Loan>> GetByIdAsync(int loanId, int memberId, bool isAdmin, CancellationToken cancellationToken = default);
        Task<List<Loan>> GetMemberLoansAsync(int memberId, bool activeOnly, CancellationToken cancellationToken = default);
        Task<PagedResult<Loan>> GetAllLoansAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    }

    public interface IAuthService
    {
        Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default);
        Task<Result<AuthResponseDto>> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);
    }

    public interface IPasswordHasher
    {
        string Hash(string password);
        bool Verify(string password, string storedHash);
    }
}
