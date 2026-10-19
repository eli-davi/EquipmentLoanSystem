using EquipmentLoanSystem.Core.Exceptions;
using EquipmentLoanSystem.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EquipmentLoanSystem.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // SaveChanges עוטף את כל השינויים (עדכון העותק + יצירת ההשאלה) בטרנזקציה אחת.
                return await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // תופסים רק את החריגה הספציפית. שורה שה-Version שלה השתנה מאז הקריאה => 0 שורות עודכנו.
                // מנקים את ה-ChangeTracker כדי שהשינויים שנדחו לא "ידלפו" לשמירה הבאה באותו scope.
                _context.ChangeTracker.Clear();
                throw new ConcurrencyConflictException("The entity was modified by another request.", ex);
            }
        }
    }
}
