using EquipmentLoanSystem.Core.DTOs;
using EquipmentLoanSystem.Core.Enums;
using EquipmentLoanSystem.Core.Exceptions;
using EquipmentLoanSystem.Data;
using EquipmentLoanSystem.Data.Repositories;
using EquipmentLoanSystem.Service.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EquipmentLoanSystem.Tests
{
    /// <summary>
    /// ההוכחה שה-optimistic concurrency עובד: שני DbContext נפרדים (כמו שתי בקשות HTTP)
    /// קוראים את אותו עותק, שניהם משנים אותו, הראשון שומר בהצלחה והשני מקבל חריגה.
    /// </summary>
    public class ConcurrencyTests : IDisposable
    {
        private const int CopyId = 1; // SN-001, פנוי ב-Seed
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<AppDbContext> _options;

        public ConcurrencyTests()
        {
            // חיבור אחד פתוח = אותו בסיס נתונים in-memory לשני ה-contexts
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;

            using var setup = new AppDbContext(_options);
            setup.Database.EnsureCreated(); // כולל נתוני ה-Seed
        }

        [Fact]
        public async Task TwoContexts_UpdateSameCopy_FirstSaves_SecondThrowsDbUpdateConcurrencyException()
        {
            await using var contextA = new AppDbContext(_options);
            await using var contextB = new AppDbContext(_options);

            // שתי "בקשות" קוראות את אותה שורה כשהיא עדיין Available
            var copyA = await contextA.ItemCopies.SingleAsync(c => c.Id == CopyId);
            var copyB = await contextB.ItemCopies.SingleAsync(c => c.Id == CopyId);
            Assert.Equal(CopyStatus.Available, copyA.Status);
            Assert.Equal(CopyStatus.Available, copyB.Status);

            // שתיהן משנות
            copyA.Status = CopyStatus.Loaned;
            copyB.Status = CopyStatus.Loaned;

            // הראשונה שומרת בהצלחה (ה-Version מתחלף)
            await contextA.SaveChangesAsync();

            // השנייה מנסה לעדכן לפי Version שכבר לא קיים => 0 שורות => חריגה
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => contextB.SaveChangesAsync());
        }

        [Fact]
        public async Task UnitOfWork_TranslatesDbUpdateConcurrencyException_ToConcurrencyConflictException()
        {
            await using var contextA = new AppDbContext(_options);
            await using var contextB = new AppDbContext(_options);

            var copyA = await contextA.ItemCopies.SingleAsync(c => c.Id == CopyId);
            var copyB = await contextB.ItemCopies.SingleAsync(c => c.Id == CopyId);
            copyA.Status = CopyStatus.Loaned;
            copyB.Status = CopyStatus.Maintenance;

            await new UnitOfWork(contextA).SaveChangesAsync();

            await Assert.ThrowsAsync<ConcurrencyConflictException>(() => new UnitOfWork(contextB).SaveChangesAsync());
        }

        [Fact]
        public async Task TwoUsers_LoanSameItem_OnlyOneLoanIsCreated_SecondGets409()
        {
            // שני scopes נפרדים = שתי בקשות במקביל, כל אחת עם DbContext ו-LoanService משלה
            await using var contextA = new AppDbContext(_options);
            await using var contextB = new AppDbContext(_options);
            var serviceA = CreateService(contextA);
            var serviceB = CreateService(contextB);

            // בקשה B קוראת את העותק לפני ש-A שומרת (מדמה את חלון הזמן בין הקריאה לשמירה)
            var staleCopyInB = await new ItemCopyRepository(contextB).GetAvailableByItemIdAsync(1);
            Assert.NotNull(staleCopyInB);

            var first = await serviceA.CreateLoanAsync(2, new LoanRequestDto { ItemId = 1 });
            // B ממשיכה עם העותק שכבר נטען אצלה (EF מחזיר את המופע המקומי, עם ה-Version הישן)
            var second = await serviceB.CreateLoanAsync(3, new LoanRequestDto { ItemId = 1, ItemCopyId = staleCopyInB!.Id });

            Assert.True(first.IsSuccess);
            Assert.False(second.IsSuccess);
            Assert.Equal(409, second.ErrorCode);

            await using var verify = new AppDbContext(_options);
            var activeLoans = await verify.Loans.CountAsync(l => l.ItemCopyId == staleCopyInB.Id && l.Status == LoanStatus.Active);
            Assert.Equal(1, activeLoans); // אין שתי השאלות פעילות על עותק אחד
        }

        [Fact]
        public async Task LoanTheSameItemTwice_FirstSucceeds_SecondSaysAlreadyLoaned()
        {
            await using var context = new AppDbContext(_options);
            var service = CreateService(context);

            var first = await service.CreateLoanAsync(2, new LoanRequestDto { ItemId = 1 });
            var second = await service.CreateLoanAsync(2, new LoanRequestDto { ItemId = 1 });

            Assert.True(first.IsSuccess);
            Assert.Equal(409, second.ErrorCode);
            Assert.Equal(LoanService.AlreadyLoanedMessage, second.ErrorMessage);
        }

        private static LoanService CreateService(AppDbContext context) => new(
            new ItemRepository(context),
            new ItemCopyRepository(context),
            new LoanRepository(context),
            new MemberRepository(context),
            new UnitOfWork(context),
            NullLogger<LoanService>.Instance);

        public void Dispose() => _connection.Dispose();
    }
}
