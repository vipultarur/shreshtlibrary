using System.Threading.Tasks;
using System.Linq;
using WebApplication1.Models.Responses;
using System.Threading;
using System;
using WebApplication1.Repositories;

namespace WebApplication1.Services
{
    public class StudentReferralService : IStudentReferralService
    {
        private readonly IStudentRepository _repository;

        public StudentReferralService(IStudentRepository repository)
        {
            _repository = repository;
        }

        public async Task<ApiResponse<object>> GetReferralCodeAsync(long userId, CancellationToken ct = default)
        {
            var refCode = await _repository.GetReferralCodeAsync(userId, ct);
            
            if (refCode == null)
            {
                return ApiResponse<object>.Fail("No referral code found. Please generate one.");
            }

            return ApiResponse<object>.Ok(new
            {
                id = refCode.Id,
                code = refCode.Code,
                used_by_count = refCode.UsedByCount,
                benefit_given = refCode.BenefitGiven
            });
        }

        public async Task<ApiResponse<object>> GenerateReferralCodeAsync(long userId, CancellationToken ct = default)
        {
            var existing = await _repository.GetReferralCodeAsync(userId, ct);
            if (existing != null)
            {
                return ApiResponse<object>.Fail("Referral code already exists.");
            }

            var profile = await _repository.GetProfileWithUserAsync(userId, ct);
            if (profile == null) return ApiResponse<object>.Fail("Profile not found");

            string code = $"REF{profile.StudentId?.Replace("-", "")}{System.Security.Cryptography.RandomNumberGenerator.GetInt32(10, 100)}";
            var refCode = new WebApplication1.Models.StudentsReferralcode
            {
                StudentId = userId,
                Code = code,
                UsedByCount = 0,
                BenefitGiven = "1 month free extension"
            };
            
            _repository.AddReferralCode(refCode, ct);
            await _repository.SaveChangesAsync(ct);

            return ApiResponse<object>.Ok(new
            {
                id = refCode.Id,
                code = refCode.Code,
                used_by_count = refCode.UsedByCount,
                benefit_given = refCode.BenefitGiven
            });
        }

        public async Task<ApiResponse<object>> ApplyReferralAsync(long userId, string code, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(code))
                return ApiResponse<object>.Fail("Referral code is required");

            var refCode = await _repository.GetReferralCodeByCodeAsync(code, ct);
            if (refCode == null)
                return ApiResponse<object>.Fail("Invalid referral code");

            if (refCode.StudentId == userId)
                return ApiResponse<object>.Fail("You cannot use your own referral code");

            var hasApplied = await _repository.HasAppliedReferralAsync(userId, ct);
            if (hasApplied)
                return ApiResponse<object>.Fail("You have already applied a referral code");

            var history = new WebApplication1.Models.StudentsReferralhistory
            {
                ReferrerId = refCode.StudentId,
                ReferredStudentId = userId,
                AppliedAt = DateTime.UtcNow
            };

            refCode.UsedByCount++;
            
            _repository.AddReferralHistory(history, ct);
            await _repository.SaveChangesAsync(ct);

            return ApiResponse<object>.Ok(new { message = "Referral code applied successfully" });
        }

        public async Task<ApiResponse<object>> GetReferralHistoryAsync(long userId, int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 50) pageSize = 50;

            var history = await _repository.GetReferralHistoryAsync(userId, page, pageSize, ct);
            return ApiResponse<object>.Ok(history);
        }
    }
}
