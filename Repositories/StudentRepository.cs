using System.Threading.Tasks;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Repositories
{
    public class StudentRepository : IStudentRepository
    {
        private readonly ApplicationDbContext _context;

        public StudentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<StudentsStudentprofile?> GetProfileWithUserAsync(long userId, CancellationToken ct = default)
        {
            return await _context.StudentsStudentprofiles
                .AsNoTracking()
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId, ct);
        }

        public async Task<AccountsCustomuser?> GetUserWithProfileAsync(long userId, CancellationToken ct = default)
        {
            return await _context.AccountsCustomusers
                .AsNoTracking()
                .Include(u => u.StudentsStudentprofile)
                .FirstOrDefaultAsync(u => u.Id == userId, ct);
        }

        public async Task<StudentsReferralcode?> GetReferralCodeAsync(long userId, CancellationToken ct = default)
        {
            return await _context.StudentsReferralcodes
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.StudentId == userId, ct);
        }

        public async Task<StudentsReferralcode?> GetReferralCodeByCodeAsync(string code, CancellationToken ct = default)
        {
            return await _context.StudentsReferralcodes
                .FirstOrDefaultAsync(r => r.Code == code, ct);
        }

        public async Task<bool> HasAppliedReferralAsync(long userId, CancellationToken ct = default)
        {
            return await _context.StudentsReferralhistories
                .AnyAsync(h => h.ReferredStudentId == userId, ct);
        }

        public void AddReferralCode(StudentsReferralcode referralCode, CancellationToken ct = default)
        {
            _context.StudentsReferralcodes.Add(referralCode);
        }

        public void AddReferralHistory(StudentsReferralhistory history, CancellationToken ct = default)
        {
            _context.StudentsReferralhistories.Add(history);
        }

        public async Task<System.Collections.Generic.List<object>> GetReferralHistoryAsync(long userId, int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            return await _context.StudentsReferralhistories
                .AsNoTracking()
                .Include(h => h.ReferredStudent)
                .Where(h => h.ReferrerId == userId)
                .OrderByDescending(h => h.AppliedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(h => (object)new
                {
                    id = h.Id,
                    applied_at = h.AppliedAt.ToString("O"),
                    referred_student = h.ReferredStudent.FirstName + " " + h.ReferredStudent.LastName
                })
                .ToListAsync(ct);
        }

        public async Task SaveChangesAsync(CancellationToken ct = default)
        {
            await _context.SaveChangesAsync(ct);
        }
    }
}
