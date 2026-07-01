using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class ReportsService : IReportsService
    {
        private readonly ApplicationDbContext _context;

        public ReportsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResult<object>> GetAttendanceReportAsync(int page, int pageSize, CancellationToken ct = default)
        {
            var query = _context.AttendanceAttendances.AsNoTracking().Include(a => a.Student);
            
            var totalCount = await query.CountAsync(ct);
            var items = await query.OrderByDescending(a => a.Date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new {
                    id = a.Id,
                    date = a.Date.ToString("yyyy-MM-dd"),
                    student_name = a.Student != null ? a.Student.FirstName + " " + a.Student.LastName : "Unknown",
                    is_present = a.IsPresent,
                    time_in = a.TimeIn,
                    time_out = a.TimeOut,
                    total_hours = a.TotalHours
                })
                .ToListAsync(ct);

            return ServiceResult<object>.Ok(new { count = totalCount, data = items });
        }

        public async Task<ServiceResult<object>> GetPaymentsReportAsync(int page, int pageSize, CancellationToken ct = default)
        {
            var query = _context.PaymentsPayments.AsNoTracking().Include(p => p.Student);
            
            var totalCount = await query.CountAsync(ct);
            var items = await query.OrderByDescending(p => p.PaymentDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new {
                    id = p.Id,
                    date = p.PaymentDate.ToString("yyyy-MM-dd"),
                    student_name = p.Student != null ? p.Student.FirstName + " " + p.Student.LastName : "Unknown",
                    amount = p.Amount,
                    status = p.Status,
                    payment_mode = p.PaymentMode
                })
                .ToListAsync(ct);

            return ServiceResult<object>.Ok(new { count = totalCount, data = items });
        }

        public async Task<ServiceResult<object>> GetStudentsReportAsync(int page, int pageSize, CancellationToken ct = default)
        {
            var query = _context.StudentsStudentprofiles.AsNoTracking().Include(s => s.User);
            
            var totalCount = await query.CountAsync(ct);
            var items = await query.OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new {
                    id = s.Id,
                    student_id = s.StudentId,
                    name = s.User.FirstName + " " + s.User.LastName,
                    status = s.Status,
                    joining_date = s.JoiningDate
                })
                .ToListAsync(ct);

            return ServiceResult<object>.Ok(new { count = totalCount, data = items });
        }

        public async Task<ServiceResult<object>> GetMembershipsReportAsync(int page, int pageSize, CancellationToken ct = default)
        {
            var query = _context.MembershipsMemberships.AsNoTracking().Include(m => m.Student).Include(m => m.Plan);
            
            var totalCount = await query.CountAsync(ct);
            var items = await query.OrderByDescending(m => m.StartDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new {
                    id = m.Id,
                    student_name = m.Student != null ? m.Student.FirstName + " " + m.Student.LastName : "Unknown",
                    plan_name = m.Plan != null ? m.Plan.Name : m.PlanNameSnapshot,
                    start_date = m.StartDate.ToString("yyyy-MM-dd"),
                    end_date = m.EndDate.ToString("yyyy-MM-dd"),
                    status = m.Status,
                    price = m.PriceSnapshot
                })
                .ToListAsync(ct);

            return ServiceResult<object>.Ok(new { count = totalCount, data = items });
        }

        public async Task<ServiceResult<object>> GetDailySummaryAsync(CancellationToken ct = default)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            
            var activeStudents = await _context.AccountsCustomusers.CountAsync(u => u.Role == "student" && u.IsActive, ct);
            var presentToday = await _context.AttendanceAttendances.CountAsync(a => a.Date == today && a.IsPresent, ct);
            var collectedToday = await _context.PaymentsPayments.Where(p => p.Status == "completed" && p.PaymentDate == today).SumAsync(p => p.Amount, ct);
            
            return ServiceResult<object>.Ok(new {
                active_students = activeStudents,
                present_today = presentToday,
                collected_today = collectedToday,
                date = today.ToString("yyyy-MM-dd")
            });
        }

        public async Task<ServiceResult<object>> GetSeatsReportAsync(CancellationToken ct = default)
        {
            var seats = await _context.SeatsSeats.AsNoTracking().ToListAsync(ct);

            var report = seats.GroupBy(s => s.Floor).Select(g => {
                var floorSeats = g.ToList();
                var floorOccupied = floorSeats.Count(s => s.Status != null && s.Status.ToUpper() == "OCCUPIED");
                var floorReserved = floorSeats.Count(s => (s.Status != null && s.Status.ToUpper() == "RESERVED") || s.IsReservedForGirls == true);

                return new {
                    floor = g.Key,
                    total = floorSeats.Count,
                    occupied = floorOccupied,
                    reserved = floorReserved,
                    available = floorSeats.Count - floorOccupied - floorReserved
                };
            }).ToList();

            return ServiceResult<object>.Ok(report);
        }

        public async Task<byte[]> ExportReportCsvAsync(string kind, CancellationToken ct = default)
        {
            var sb = new System.Text.StringBuilder();

            switch (kind.ToLower())
            {
                case "attendance":
                    sb.AppendLine("Id,Date,Student,IsPresent,TimeIn,TimeOut,TotalHours");
                    var attendances = await _context.AttendanceAttendances.AsNoTracking().Include(a => a.Student).OrderByDescending(a => a.Date).Take(1000).ToListAsync(ct);
                    foreach (var a in attendances)
                    {
                        var name = a.Student != null ? $"{a.Student.FirstName} {a.Student.LastName}" : "Unknown";
                        sb.AppendLine($"{a.Id},{a.Date:yyyy-MM-dd},{EscapeCsv(name)},{a.IsPresent},{a.TimeIn},{a.TimeOut},{a.TotalHours}");
                    }
                    break;
                case "payments":
                    sb.AppendLine("Id,Date,Student,Amount,Status,PaymentMode");
                    var payments = await _context.PaymentsPayments.AsNoTracking().Include(p => p.Student).OrderByDescending(p => p.PaymentDate).Take(1000).ToListAsync(ct);
                    foreach (var p in payments)
                    {
                        var name = p.Student != null ? $"{p.Student.FirstName} {p.Student.LastName}" : "Unknown";
                        sb.AppendLine($"{p.Id},{p.PaymentDate:yyyy-MM-dd},{EscapeCsv(name)},{p.Amount},{p.Status},{p.PaymentMode}");
                    }
                    break;
                case "students":
                    sb.AppendLine("Id,StudentId,Name,Status,JoiningDate");
                    var students = await _context.StudentsStudentprofiles.AsNoTracking().Include(s => s.User).OrderByDescending(s => s.CreatedAt).Take(1000).ToListAsync(ct);
                    foreach (var s in students)
                    {
                        var name = s.User != null ? $"{s.User.FirstName} {s.User.LastName}" : "Unknown";
                        sb.AppendLine($"{s.Id},{s.StudentId},{EscapeCsv(name)},{s.Status},{s.JoiningDate}");
                    }
                    break;
                case "memberships":
                    sb.AppendLine("Id,Student,Plan,StartDate,EndDate,Status,Price");
                    var memberships = await _context.MembershipsMemberships.AsNoTracking().Include(m => m.Student).Include(m => m.Plan).OrderByDescending(m => m.StartDate).Take(1000).ToListAsync(ct);
                    foreach (var m in memberships)
                    {
                        var name = m.Student != null ? $"{m.Student.FirstName} {m.Student.LastName}" : "Unknown";
                        var plan = m.Plan != null ? m.Plan.Name : m.PlanNameSnapshot;
                        sb.AppendLine($"{m.Id},{EscapeCsv(name)},{EscapeCsv(plan)},{m.StartDate:yyyy-MM-dd},{m.EndDate:yyyy-MM-dd},{m.Status},{m.PriceSnapshot}");
                    }
                    break;
                default:
                    sb.AppendLine("Unknown Report Type");
                    break;
            }

            return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        }

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }
    }
}
