using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using System.Linq;
using System;

namespace WebApplication1.Repositories
{
    public class AttendanceRepository : IAttendanceRepository
    {
        private readonly ApplicationDbContext _context;

        public AttendanceRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AttendanceAttendance?> GetAttendanceAsync(long id, CancellationToken ct = default)
        {
            return await _context.AttendanceAttendances
                .Include(a => a.Student)
                .FirstOrDefaultAsync(a => a.Id == id, ct);
        }

        public async Task<List<AttendanceAttendance>> GetAttendanceForDateAsync(DateOnly date, CancellationToken ct = default)
        {
            return await _context.AttendanceAttendances
                .Include(a => a.Student)
                .Where(a => a.Date == date)
                .ToListAsync(ct);
        }

        public async Task<AttendanceAttendance?> GetStudentAttendanceTodayAsync(long studentId, DateOnly today, CancellationToken ct = default)
        {
            return await _context.AttendanceAttendances
                .FirstOrDefaultAsync(a => a.StudentId == studentId && a.Date == today, ct);
        }

        public Task UpdateAttendanceAsync(AttendanceAttendance attendance, CancellationToken ct = default)
        {
            _context.AttendanceAttendances.Update(attendance);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync(CancellationToken ct = default)
        {
            await _context.SaveChangesAsync(ct);
        }
    }
}
