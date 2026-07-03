using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using WebApplication1.Models;
using System;

namespace WebApplication1.Repositories
{
    public interface IAttendanceRepository
    {
        Task<AttendanceAttendance?> GetAttendanceAsync(long id, CancellationToken ct = default);
        Task<List<AttendanceAttendance>> GetAttendanceForDateAsync(DateOnly date, CancellationToken ct = default);
        Task<AttendanceAttendance?> GetStudentAttendanceTodayAsync(long studentId, DateOnly today, CancellationToken ct = default);
        Task UpdateAttendanceAsync(AttendanceAttendance attendance, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}
