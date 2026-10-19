using EquipmentLoanSystem.Core.DTOs;
using EquipmentLoanSystem.Core.Entities;
using EquipmentLoanSystem.Core.Enums;
using EquipmentLoanSystem.Core.Exceptions;
using EquipmentLoanSystem.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace EquipmentLoanSystem.Service.Services
{
    public class LoanService : ILoanService
    {
        public const string LoanSuccessMessage = "הפריט הושאל בהצלחה";
        public const string AlreadyLoanedMessage = "זה כבר מושאל";
        public const string RaceLostMessage = "זה כבר מושאל - משתמש אחר השאיל את העותק באותו רגע";
        public const int DefaultLoanDays = 14;

        private readonly IItemRepository _itemRepository;
        private readonly IItemCopyRepository _itemCopyRepository;
        private readonly ILoanRepository _loanRepository;
        private readonly IMemberRepository _memberRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<LoanService> _logger;

        public LoanService(
            IItemRepository itemRepository,
            IItemCopyRepository itemCopyRepository,
            ILoanRepository loanRepository,
            IMemberRepository memberRepository,
            IUnitOfWork unitOfWork,
            ILogger<LoanService> logger)
        {
            _itemRepository = itemRepository;
            _itemCopyRepository = itemCopyRepository;
            _loanRepository = loanRepository;
            _memberRepository = memberRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<Loan>> CreateLoanAsync(int memberId, LoanRequestDto dto, CancellationToken cancellationToken = default)
        {
            var itemId = dto.ItemId!.Value;

            if (!await _memberRepository.ExistsAsync(memberId, cancellationToken))
                return Result<Loan>.NotFound("המשתמש לא נמצא");

            if (!await _itemRepository.ExistsAsync(itemId, cancellationToken))
                return Result<Loan>.NotFound("הפריט לא נמצא");

            // קריאה עם מעקב: EF שומר את ה-Version המקורי של העותק
            var copy = dto.ItemCopyId.HasValue
                ? await _itemCopyRepository.GetByIdAsync(dto.ItemCopyId.Value, cancellationToken)
                : await _itemCopyRepository.GetAvailableByItemIdAsync(itemId, cancellationToken);

            if (copy is null && dto.ItemCopyId.HasValue)
                return Result<Loan>.NotFound("העותק לא נמצא");

            if (copy is not null && copy.ItemId != itemId)
                return Result<Loan>.Fail("העותק אינו שייך לפריט שנבחר");

            // ולידציה עסקית (תלויה במצב הנתונים) - יושבת כאן, לא ב-Controller
            if (copy is null || copy.Status != CopyStatus.Available)
            {
                _logger.LogInformation("Loan rejected: item {ItemId} has no available copy (member {MemberId})", itemId, memberId);
                return Result<Loan>.Conflict(AlreadyLoanedMessage);
            }

            var now = DateTime.UtcNow;
            copy.Status = CopyStatus.Loaned;

            var loan = new Loan
            {
                ItemCopy = copy,
                ItemCopyId = copy.Id,
                MemberId = memberId,
                LoanDate = now,
                DueDate = now.AddDays(DefaultLoanDays),
                Status = LoanStatus.Active
            };
            await _loanRepository.AddAsync(loan, cancellationToken);

            try
            {
                // UPDATE ItemCopies SET Status=1, Version=@new WHERE Id=@id AND Version=@original
                // + INSERT Loans - באותה טרנזקציה. אם מישהו שינה את העותק בינתיים - 0 שורות => התנגשות.
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (ConcurrencyConflictException)
            {
                _logger.LogWarning(
                    "Concurrency conflict: copy {CopyId} of item {ItemId} was taken by another request (member {MemberId})",
                    copy.Id, itemId, memberId);
                return Result<Loan>.Conflict(RaceLostMessage);
            }

            _logger.LogInformation("Loan {LoanId} created: copy {CopyId} of item {ItemId} for member {MemberId}",
                loan.Id, copy.Id, itemId, memberId);

            return Result<Loan>.Success(loan);
        }

        public async Task<Result<Loan>> ReturnLoanAsync(int loanId, int memberId, bool isAdmin, CancellationToken cancellationToken = default)
        {
            var loan = await _loanRepository.GetForUpdateAsync(loanId, cancellationToken);
            if (loan is null)
                return Result<Loan>.NotFound("ההשאלה לא נמצאה");

            if (loan.MemberId != memberId && !isAdmin)
                return Result<Loan>.Forbidden("אין הרשאה להחזיר השאלה של משתמש אחר");

            if (loan.Status == LoanStatus.Returned)
                return Result<Loan>.Conflict("ההשאלה כבר הוחזרה");

            loan.Status = LoanStatus.Returned;
            loan.ReturnDate = DateTime.UtcNow;
            loan.ItemCopy.Status = CopyStatus.Available;

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (ConcurrencyConflictException)
            {
                _logger.LogWarning("Concurrency conflict while returning loan {LoanId} (copy {CopyId})", loanId, loan.ItemCopyId);
                return Result<Loan>.Conflict("העותק עודכן במקביל על ידי בקשה אחרת. נסה שוב.");
            }

            _logger.LogInformation("Loan {LoanId} returned by member {MemberId}", loanId, memberId);
            return Result<Loan>.Success(loan);
        }

        public async Task<Result<Loan>> GetByIdAsync(int loanId, int memberId, bool isAdmin, CancellationToken cancellationToken = default)
        {
            var loan = await _loanRepository.GetByIdAsync(loanId, cancellationToken);
            if (loan is null)
                return Result<Loan>.NotFound("ההשאלה לא נמצאה");

            if (loan.MemberId != memberId && !isAdmin)
                return Result<Loan>.Forbidden("אין הרשאה לצפות בהשאלה של משתמש אחר");

            return Result<Loan>.Success(loan);
        }

        public Task<List<Loan>> GetMemberLoansAsync(int memberId, bool activeOnly, CancellationToken cancellationToken = default)
            => _loanRepository.GetByMemberAsync(memberId, activeOnly, cancellationToken);

        public Task<PagedResult<Loan>> GetAllLoansAsync(int page, int pageSize, CancellationToken cancellationToken = default)
            => _loanRepository.GetPagedAsync(page, pageSize, cancellationToken);
    }
}
