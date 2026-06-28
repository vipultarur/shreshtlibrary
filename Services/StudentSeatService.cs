using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class StudentSeatService : IStudentSeatService
    {
        private readonly ApplicationDbContext _context;

        public StudentSeatService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResult<object>> GetSeatLayoutAsync(CancellationToken ct = default)
        {
            var seats = await _context.SeatsSeats
                .AsNoTracking()
                .OrderBy(s => s.Floor).ThenBy(s => s.Row).ThenBy(s => s.SeatNumber)
                .Select(s => new
                {
                    id = s.Id,
                    seat_number = s.SeatNumber,
                    status = s.Status,
                    x_position = 0,
                    y_position = 0,
                    row_id = s.RowRefId,
                    row_name = s.Row,
                    floor_name = s.Floor,
                    is_active = true
                })
                .ToListAsync(ct);

            return ServiceResult<object>.Ok(seats);
        }

        public async Task<ServiceResult<object>> GetSeatHistoryAsync(long studentId, CancellationToken ct = default)
        {
            var history = await _context.SeatsSeatassignments
                .AsNoTracking()
                .Include(a => a.Seat)
                .Where(a => a.StudentId == studentId)
                .OrderByDescending(a => a.AssignedDate)
                .Select(a => new
                {
                    id = a.Id,
                    seat = new
                    {
                        id = a.Seat.Id,
                        seat_number = a.Seat.SeatNumber
                    },
                    assignment_date = a.AssignedDate.ToString("yyyy-MM-dd"),
                    start_time = (string)null,
                    end_time = a.ReleasedDate.HasValue ? a.ReleasedDate.Value.ToString("yyyy-MM-dd") : null,
                    status = a.ReleasedDate.HasValue ? "released" : "active",
                    notes = a.Seat.Notes
                })
                .ToListAsync(ct);

            return ServiceResult<object>.Ok(history);
        }
    }
}
