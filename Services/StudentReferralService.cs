using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
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
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;

        public StudentReferralService(IStudentRepository repository, Microsoft.Extensions.Caching.Memory.IMemoryCache cache)
        {
            _repository = repository;
            _cache = cache;
        }

        public async Task<ApiResponse<object>> GetReferralCodeAsync(long userId, CancellationToken ct = default)
        {
            var cacheKey = $"StudentReferralCode_{userId}";
            if (_cache.TryGetValue(cacheKey, out object? cachedCode) && cachedCode != null)
            {
                return ApiResponse<object>.Ok(cachedCode);
            }

            var refCode = await _repository.GetReferralCodeAsync(userId, ct);
            
            if (refCode == null)
            {
                return ApiResponse<object>.Fail("No referral code found. Please generate one.");
            }

            var result = new
            {
                id = refCode.Id,
                code = refCode.Code,
                used_by_count = refCode.UsedByCount,
                benefit_given = refCode.BenefitGiven
            };

            _cache.Set(cacheKey, result, TimeSpan.FromHours(24));
            return ApiResponse<object>.Ok(result);
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

            var result = new
            {
                id = refCode.Id,
                code = refCode.Code,
                used_by_count = refCode.UsedByCount,
                benefit_given = refCode.BenefitGiven
            };

            _cache.Remove($"StudentReferralCode_{userId}");
            
            return ApiResponse<object>.Ok(result);
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

            _cache.Remove($"StudentReferralCode_{refCode.StudentId}");
            _cache.Remove($"StudentReferralHistory_{userId}_1_20");

            return ApiResponse<object>.Ok(new { message = "Referral code applied successfully" });
        }

        public async Task<ApiResponse<object>> GetReferralHistoryAsync(long userId, int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 50) pageSize = 50;

            var cacheKey = $"StudentReferralHistory_{userId}_{page}_{pageSize}";
            if (_cache.TryGetValue(cacheKey, out object? cachedHistory) && cachedHistory != null)
            {
                return ApiResponse<object>.Ok(cachedHistory);
            }

            var history = await _repository.GetReferralHistoryAsync(userId, page, pageSize, ct);
            
            _cache.Set(cacheKey, history, TimeSpan.FromHours(1));
            return ApiResponse<object>.Ok(history);
        }
    }
}
