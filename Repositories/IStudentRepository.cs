using System.Threading.Tasks;
using System.Threading;
using WebApplication1.Models;

namespace WebApplication1.Repositories
{
    public interface IStudentRepository
    {
        Task<StudentsStudentprofile?> GetProfileWithUserAsync(long userId, CancellationToken ct = default);
        Task<AccountsCustomuser?> GetUserWithProfileAsync(long userId, CancellationToken ct = default);
        Task<StudentsReferralcode?> GetReferralCodeAsync(long userId, CancellationToken ct = default);
        Task<StudentsReferralcode?> GetReferralCodeByCodeAsync(string code, CancellationToken ct = default);
        Task<bool> HasAppliedReferralAsync(long userId, CancellationToken ct = default);
        Task<System.Collections.Generic.List<object>> GetReferralHistoryAsync(long userId, int page = 1, int pageSize = 20, CancellationToken ct = default);
        void AddReferralCode(StudentsReferralcode referralCode, CancellationToken ct = default);
        void AddReferralHistory(StudentsReferralhistory history, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}
