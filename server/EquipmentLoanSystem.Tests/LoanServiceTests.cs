using EquipmentLoanSystem.Core.DTOs;
using EquipmentLoanSystem.Core.Entities;
using EquipmentLoanSystem.Core.Enums;
using EquipmentLoanSystem.Core.Exceptions;
using EquipmentLoanSystem.Core.Interfaces;
using EquipmentLoanSystem.Service.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace EquipmentLoanSystem.Tests
{
    /// <summary>בדיקות יחידה לשכבת ה-Service, עם repositories מדומים ב-Moq.</summary>
    public class LoanServiceTests
    {
        private const int MemberId = 2;
        private const int ItemId = 1;

        private readonly Mock<IItemRepository> _items = new();
        private readonly Mock<IItemCopyRepository> _copies = new();
        private readonly Mock<ILoanRepository> _loans = new();
        private readonly Mock<IMemberRepository> _members = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly LoanService _service;

        public LoanServiceTests()
        {
            _members.Setup(m => m.ExistsAsync(MemberId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _items.Setup(i => i.ExistsAsync(ItemId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            _service = new LoanService(_items.Object, _copies.Object, _loans.Object, _members.Object,
                _unitOfWork.Object, NullLogger<LoanService>.Instance);
        }

        private static ItemCopy AvailableCopy() => new()
        {
            Id = 10, ItemId = ItemId, SerialNumber = "SN-TEST", Status = CopyStatus.Available,
            Item = new Item { Id = ItemId, Name = "מצלמה" }
        };

        // ---------- המשאב המוגבל: מקרה תקין ----------
        [Fact]
        public async Task CreateLoan_WhenCopyAvailable_Succeeds_AndMarksCopyAsLoaned()
        {
            var copy = AvailableCopy();
            _copies.Setup(c => c.GetAvailableByItemIdAsync(ItemId, It.IsAny<CancellationToken>())).ReturnsAsync(copy);

            var result = await _service.CreateLoanAsync(MemberId, new LoanRequestDto { ItemId = ItemId });

            Assert.True(result.IsSuccess);
            Assert.Equal(CopyStatus.Loaned, copy.Status);
            Assert.Equal(MemberId, result.Value!.MemberId);
            Assert.Equal(LoanStatus.Active, result.Value.Status);
            _loans.Verify(l => l.AddAsync(It.Is<Loan>(x => x.ItemCopyId == copy.Id), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ---------- המשאב המוגבל: מקרה שנדחה (אין עותק פנוי) ----------
        [Fact]
        public async Task CreateLoan_WhenNoCopyAvailable_Returns409_AlreadyLoaned_AndDoesNotSave()
        {
            _copies.Setup(c => c.GetAvailableByItemIdAsync(ItemId, It.IsAny<CancellationToken>())).ReturnsAsync((ItemCopy?)null);

            var result = await _service.CreateLoanAsync(MemberId, new LoanRequestDto { ItemId = ItemId });

            Assert.False(result.IsSuccess);
            Assert.Equal(409, result.ErrorCode);
            Assert.Equal(LoanService.AlreadyLoanedMessage, result.ErrorMessage);
            _loans.Verify(l => l.AddAsync(It.IsAny<Loan>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ---------- המשאב המוגבל: מקרה שנדחה (התנגשות בזמן השמירה) ----------
        [Fact]
        public async Task CreateLoan_WhenConcurrencyConflictOnSave_Returns409()
        {
            _copies.Setup(c => c.GetAvailableByItemIdAsync(ItemId, It.IsAny<CancellationToken>())).ReturnsAsync(AvailableCopy());
            _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new ConcurrencyConflictException("conflict", new Exception()));

            var result = await _service.CreateLoanAsync(MemberId, new LoanRequestDto { ItemId = ItemId });

            Assert.False(result.IsSuccess);
            Assert.Equal(409, result.ErrorCode);
            Assert.StartsWith(LoanService.AlreadyLoanedMessage, result.ErrorMessage);
        }

        [Fact]
        public async Task CreateLoan_WhenSpecificCopyAlreadyLoaned_Returns409()
        {
            var copy = AvailableCopy();
            copy.Status = CopyStatus.Loaned;
            _copies.Setup(c => c.GetByIdAsync(copy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(copy);

            var result = await _service.CreateLoanAsync(MemberId, new LoanRequestDto { ItemId = ItemId, ItemCopyId = copy.Id });

            Assert.Equal(409, result.ErrorCode);
            Assert.Equal(LoanService.AlreadyLoanedMessage, result.ErrorMessage);
        }

        [Fact]
        public async Task CreateLoan_WhenItemDoesNotExist_Returns404()
        {
            var result = await _service.CreateLoanAsync(MemberId, new LoanRequestDto { ItemId = 999 });

            Assert.Equal(404, result.ErrorCode);
        }

        [Fact]
        public async Task ReturnLoan_ByAnotherUser_Returns403()
        {
            var loan = new Loan { Id = 5, MemberId = 77, Status = LoanStatus.Active, ItemCopy = AvailableCopy() };
            _loans.Setup(l => l.GetForUpdateAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(loan);

            var result = await _service.ReturnLoanAsync(5, MemberId, isAdmin: false);

            Assert.Equal(403, result.ErrorCode);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ReturnLoan_ByOwner_MakesCopyAvailableAgain()
        {
            var copy = AvailableCopy();
            copy.Status = CopyStatus.Loaned;
            var loan = new Loan { Id = 5, MemberId = MemberId, Status = LoanStatus.Active, ItemCopy = copy, ItemCopyId = copy.Id };
            _loans.Setup(l => l.GetForUpdateAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(loan);

            var result = await _service.ReturnLoanAsync(5, MemberId, isAdmin: false);

            Assert.True(result.IsSuccess);
            Assert.Equal(LoanStatus.Returned, loan.Status);
            Assert.Equal(CopyStatus.Available, copy.Status);
            Assert.NotNull(loan.ReturnDate);
        }
    }
}
