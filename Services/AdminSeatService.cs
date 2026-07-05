using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class AdminSeatService : IAdminSeatService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IServiceScopeFactory _scopeFactory;

        public AdminSeatService(ApplicationDbContext context, IEmailService emailService, IServiceScopeFactory scopeFactory)
        {
            _context = context;
            _emailService = emailService;
            _scopeFactory = scopeFactory;
        }

        public async Task<ServiceResult<object>> GetSeatsLayoutAsync(CancellationToken ct = default)
        {
            var floors = await _context.SeatsFloors
                .AsNoTracking()
                .Include(f => f.SeatsSeatrows)
                    .ThenInclude(r => r.SeatsSeats)
                        .ThenInclude(s => s.Student)
                            .ThenInclude(st => st!.StudentsStudentprofile)
                .AsSplitQuery()
                .OrderBy(f => f.Order)
                .ToListAsync(ct);

            var result = floors.Select(f => new {
                id = f.Id,
                name = f.Name,
                description = f.Description,
                order = f.Order,
                is_active = f.IsActive,
                rows = f.SeatsSeatrows.OrderBy(r => r.Order).Select(r => new {
                    id = r.Id,
                    floor = r.FloorId,
                    label = r.Label,
                    order = r.Order,
                    seats = r.SeatsSeats.OrderBy(s => s.SeatNumber).Select(s => new {
                        id = s.Id,
                        floor = s.Floor,
                        row = s.Row,
                        row_ref = s.RowRefId,
                        seat_number = s.SeatNumber,
                        status = s.Status?.ToUpper(),
                        student = s.StudentId,
                        student_name = s.Student != null ? $"{s.Student.FirstName} {s.Student.LastName}".Trim() : null,
                        student_code = s.Student?.StudentsStudentprofile?.StudentId,
                        student_profile_image = s.Student?.StudentsStudentprofile?.ProfilePhoto,
                        student_profile_photo = s.Student?.StudentsStudentprofile?.ProfilePhoto,
                        assigned_at = s.AssignedAt,
                        notes = s.Notes,
                        is_reserved_for_girls = s.IsReservedForGirls
                    }).ToList()
                }).ToList()
            });

            return ServiceResult<object>.Ok(result);
        }

        public async Task<ServiceResult<object>> ReleaseAllSeatsAsync(CancellationToken ct = default)
        {
            var seats = await _context.SeatsSeats
                .Include(s => s.Student)
                .Where(s => s.Status != null && s.Status.ToUpper() == "OCCUPIED")
                .ToListAsync(ct);
                
            var emailTasks = new List<Task>();
            
            foreach (var seat in seats)
            {
                string? email = seat.Student?.Email;
                var seatNumber = seat.SeatNumber;

                seat.Status = "AVAILABLE";
                seat.StudentId = null;
                seat.AssignedAt = null;
                
                if (!string.IsNullOrWhiteSpace(email))
                {
                    var task = Task.Run(async () => 
                    {
                        try 
                        {
                            using var scope = _scopeFactory.CreateScope();
                            var emailSvc = scope.ServiceProvider.GetRequiredService<IEmailService>();
                            await emailSvc.SendSeatReleasedEmailAsync(email, seatNumber, "All seats have been administratively released.");
                        } 
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error sending release email: {ex}");
                        }
                    });
                    emailTasks.Add(task);
                }
            }
            await _context.SaveChangesAsync(ct);
            
            if (emailTasks.Any())
            {
                await Task.WhenAll(emailTasks);
            }
            
            return ServiceResult<object>.Ok(new { message = $"{seats.Count} seats released." });
        }

        public async Task<ServiceResult<object>> ReserveBulkSeatsAsync(List<int> seatIds, bool isReservedForGirls, CancellationToken ct = default)
        {
            if (seatIds == null || !seatIds.Any())
                return ServiceResult<object>.Fail("No seat IDs provided.");

            var seats = await _context.SeatsSeats.Where(s => seatIds.Contains((int)s.Id)).ToListAsync(ct);
            
            foreach (var seat in seats)
            {
                seat.IsReservedForGirls = isReservedForGirls;
            }
            await _context.SaveChangesAsync(ct);
            return ServiceResult<object>.Ok(new { message = $"{seats.Count} seats updated." });
        }

        public async Task<ServiceResult<object>> GetAvailableSeatsAsync(CancellationToken ct = default)
        {
            var seats = await _context.SeatsSeats
                .AsNoTracking()
                .Where(s => s.Status != null && s.Status.ToUpper() == "AVAILABLE")
                .OrderBy(s => s.Floor).ThenBy(s => s.Row).ThenBy(s => s.SeatNumber)
                .Take(500)
                .Select(s => new {
                    id = s.Id,
                    floor = s.Floor,
                    row = s.Row,
                    row_ref = s.RowRefId,
                    seat_number = s.SeatNumber,
                    status = s.Status!.ToUpper(),
                    student = s.StudentId,
                    assigned_at = s.AssignedAt,
                    notes = s.Notes,
                    is_reserved_for_girls = s.IsReservedForGirls
                })
                .ToListAsync(ct);

            return ServiceResult<object>.Ok(seats);
        }

        public async Task<ServiceResult<object>> GetSeatsStatsAsync(CancellationToken ct = default)
        {
            var stats = await _context.SeatsSeats
                .AsNoTracking()
                .GroupBy(s => s.Floor)
                .Select(g => new {
                    floor = g.Key,
                    total = g.Count(),
                    occupied = g.Count(s => s.Status != null && s.Status.ToUpper() == "OCCUPIED"),
                    available = g.Count(s => s.Status != null && s.Status.ToUpper() == "AVAILABLE"),
                    reserved = g.Count(s => s.Status != null && s.Status.ToUpper() == "RESERVED")
                })
                .ToListAsync(ct);

            return ServiceResult<object>.Ok(stats);
        }

        public async Task<ServiceResult<object>> AddSeatAsync(string floor, string row, string seatNumber, string? status, string? notes, bool? isReservedForGirls, long? rowRefId, CancellationToken ct = default)
        {
            var draft = new SeatsSeat {
                Floor = floor,
                Row = row,
                SeatNumber = seatNumber,
                Status = status ?? "AVAILABLE",
                Notes = notes,
                IsReservedForGirls = isReservedForGirls ?? false,
                RowRefId = rowRefId
            };
            _context.SeatsSeats.Add(draft);
            await _context.SaveChangesAsync(ct);
            return ServiceResult<object>.Ok(new {
                id = draft.Id,
                floor = draft.Floor,
                row = draft.Row,
                seat_number = draft.SeatNumber,
                status = draft.Status,
                is_reserved_for_girls = draft.IsReservedForGirls,
                row_ref = draft.RowRefId
            });
        }

        public async Task<ServiceResult<bool>> DeleteSeatAsync(long pk, CancellationToken ct = default)
        {
            var seat = await _context.SeatsSeats.FindAsync(new object[] { pk }, ct);
            if (seat != null)
            {
                var assignments = _context.SeatsSeatassignments.Where(a => a.SeatId == seat.Id);
                _context.SeatsSeatassignments.RemoveRange(assignments);
                var changelogs = _context.SeatsSeatchangelogs.Where(c => c.SeatId == seat.Id || c.PreviousSeatId == seat.Id);
                _context.SeatsSeatchangelogs.RemoveRange(changelogs);

                _context.SeatsSeats.Remove(seat);
                await _context.SaveChangesAsync(ct);
                return ServiceResult<bool>.Ok(true);
            }
            return ServiceResult<bool>.NotFound("Seat not found");
        }

        public async Task<ServiceResult<object>> GetSeatsListAsync(int page = 1, int pageSize = 200, string nextTemplate = "", string prevTemplate = "", CancellationToken ct = default)
        {
            var query = _context.SeatsSeats
                .AsNoTracking()
                .Include(s => s.Student)
                .ThenInclude(st => st!.StudentsStudentprofile)
                .AsQueryable();

            var totalCount = await query.CountAsync(ct);
            var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);

            var data = await query
                .OrderBy(s => s.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new {
                    id = s.Id,
                    floor = s.Floor,
                    row = s.Row,
                    row_ref = s.RowRefId,
                    seat_number = s.SeatNumber,
                    status = s.Status!.ToUpper(),
                    student = s.StudentId,
                    student_name = s.Student != null ? $"{s.Student.FirstName} {s.Student.LastName}".Trim() : null,
                    student_code = s.Student != null && s.Student.StudentsStudentprofile != null ? s.Student.StudentsStudentprofile.StudentId : null,
                    student_profile_image = s.Student != null && s.Student.StudentsStudentprofile != null ? s.Student.StudentsStudentprofile.ProfilePhoto : null,
                    student_profile_photo = s.Student != null && s.Student.StudentsStudentprofile != null ? s.Student.StudentsStudentprofile.ProfilePhoto : null,
                    assigned_at = s.AssignedAt,
                    notes = s.Notes,
                    is_reserved_for_girls = s.IsReservedForGirls
                }).ToListAsync(ct);

            return ServiceResult<object>.Ok(new {
                count = totalCount,
                total_pages = totalPages,
                current_page = page,
                next = page < totalPages ? nextTemplate.Replace("{P}", (page + 1).ToString()) : null,
                previous = page > 1 ? prevTemplate.Replace("{P}", (page - 1).ToString()) : null,
                data = data
            });
        }

        public async Task<ServiceResult<object>> UpdateSeatAsync(long pk, string? floor, string? row, string? seatNumber, string? status, string? notes, bool? isReservedForGirls, long? rowRefId, CancellationToken ct = default)
        {
            var seat = await _context.SeatsSeats.FindAsync(new object[] { pk }, ct);
            if (seat == null) return ServiceResult<object>.NotFound("Seat not found");

            if (!string.IsNullOrEmpty(floor)) seat.Floor = floor;
            if (!string.IsNullOrEmpty(row)) seat.Row = row;
            if (!string.IsNullOrEmpty(seatNumber)) seat.SeatNumber = seatNumber;
            if (status != null) seat.Status = status;
            if (notes != null) seat.Notes = notes;
            if (isReservedForGirls.HasValue) seat.IsReservedForGirls = isReservedForGirls.Value;
            if (rowRefId.HasValue) seat.RowRefId = rowRefId;

            await _context.SaveChangesAsync(ct);
            return ServiceResult<object>.Ok(new {
                id = seat.Id,
                floor = seat.Floor,
                row = seat.Row,
                seat_number = seat.SeatNumber,
                status = seat.Status,
                student = seat.StudentId,
                is_reserved_for_girls = seat.IsReservedForGirls,
                row_ref = seat.RowRefId
            });
        }

        public async Task<ServiceResult<object>> UpdateSeatStatusAsync(long pk, string status, string? reason, CancellationToken ct = default)
        {
            var seat = await _context.SeatsSeats.Include(s => s.Student).FirstOrDefaultAsync(s => s.Id == pk, ct);
            if (seat == null) return ServiceResult<object>.NotFound("Seat not found");

            seat.Status = status.ToUpper();
            if (seat.Status == "AVAILABLE" && seat.StudentId != null)
            {
                string? email = seat.Student?.Email;
                var seatNumber = seat.SeatNumber;

                seat.StudentId = null;
                seat.AssignedAt = null;

                if (!string.IsNullOrWhiteSpace(email))
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            using var scope = _scopeFactory.CreateScope();
                            var emailSvc = scope.ServiceProvider.GetRequiredService<IEmailService>();
                            await emailSvc.SendSeatReleasedEmailAsync(email, seatNumber, reason ?? "Seat status updated to Available.");
                        }
                        catch { }
                    });
                }
            }
            await _context.SaveChangesAsync(ct);
            return ServiceResult<object>.Ok(new {
                id = seat.Id,
                status = seat.Status,
                student = seat.StudentId,
                assigned_at = seat.AssignedAt
            });
        }

        public async Task<ServiceResult<object>> AssignSeatAsync(long pk, long studentId, CancellationToken ct = default)
        {
            var seat = await _context.SeatsSeats.FindAsync(new object[] { pk }, ct);
            if (seat == null) return ServiceResult<object>.NotFound("Seat not found");

            seat.StudentId = studentId;
            seat.Status = "OCCUPIED";
            seat.AssignedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            var studentUser = await _context.AccountsCustomusers.FindAsync(new object[] { studentId }, ct);
            if (studentUser != null && !string.IsNullOrWhiteSpace(studentUser.Email))
            {
                string zone = $"{seat.Floor} - Row {seat.Row}";
                string timing = "Standard Timing"; // Or fetch from student's plan if available
                var email = studentUser.Email;
                var seatNumber = seat.SeatNumber;
                
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var emailSvc = scope.ServiceProvider.GetRequiredService<IEmailService>();
                    await emailSvc.SendSeatAllocatedEmailAsync(email, seatNumber, zone, timing);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending assign seat email: {ex}");
                }
            }

            return ServiceResult<object>.Ok(new {
                id = seat.Id,
                status = seat.Status,
                student = seat.StudentId,
                assigned_at = seat.AssignedAt
            });
        }

        public async Task<ServiceResult<object>> UnassignSeatAsync(long pk, string? reason, CancellationToken ct = default)
        {
            var seat = await _context.SeatsSeats.Include(s => s.Student).FirstOrDefaultAsync(s => s.Id == pk, ct);
            if (seat == null) return ServiceResult<object>.NotFound("Seat not found");

            string? email = seat.Student?.Email;
            var seatNumber = seat.SeatNumber;

            seat.StudentId = null;
            seat.Status = "AVAILABLE";
            seat.AssignedAt = null;
            await _context.SaveChangesAsync(ct);

            if (!string.IsNullOrWhiteSpace(email))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var emailSvc = scope.ServiceProvider.GetRequiredService<IEmailService>();
                    await emailSvc.SendSeatReleasedEmailAsync(email, seatNumber, reason ?? "Administrative reassignment.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending unassign seat email: {ex}");
                }
            }

            return ServiceResult<object>.Ok(new {
                id = seat.Id,
                status = seat.Status,
                student = seat.StudentId,
                assigned_at = seat.AssignedAt
            });
        }

        public async Task<ServiceResult<object>> GetFloorsListAsync(CancellationToken ct = default)
        {
            var data = await _context.SeatsFloors.AsNoTracking().Select(f => new { id = f.Id, name = f.Name, description = f.Description }).ToListAsync(ct);
            return ServiceResult<object>.Ok(data);
        }

        public async Task<ServiceResult<object>> AddFloorAsync(string name, string? description, int order, CancellationToken ct = default)
        {
            var floor = new SeatsFloor {
                Name = name,
                Description = description,
                Order = order,
                IsActive = true
            };
            _context.SeatsFloors.Add(floor);
            await _context.SaveChangesAsync(ct);
            return ServiceResult<object>.Ok(floor);
        }

        public async Task<ServiceResult<bool>> DeleteFloorAsync(long pk, CancellationToken ct = default)
        {
            var floor = await _context.SeatsFloors
                .Include(f => f.SeatsSeatrows)
                .ThenInclude(r => r.SeatsSeats)
                .FirstOrDefaultAsync(f => f.Id == pk, ct);
            if (floor != null)
            {
                foreach (var row in floor.SeatsSeatrows) {
                    foreach (var seat in row.SeatsSeats) {
                        var assignments = _context.SeatsSeatassignments.Where(a => a.SeatId == seat.Id);
                        _context.SeatsSeatassignments.RemoveRange(assignments);
                        var changelogs = _context.SeatsSeatchangelogs.Where(c => c.SeatId == seat.Id || c.PreviousSeatId == seat.Id);
                        _context.SeatsSeatchangelogs.RemoveRange(changelogs);
                    }
                    _context.SeatsSeats.RemoveRange(row.SeatsSeats);
                }
                _context.SeatsSeatrows.RemoveRange(floor.SeatsSeatrows);
                _context.SeatsFloors.Remove(floor);
                await _context.SaveChangesAsync(ct);
                return ServiceResult<bool>.Ok(true);
            }
            return ServiceResult<bool>.NotFound("Floor not found");
        }

        public async Task<ServiceResult<object>> GetRowsListAsync(CancellationToken ct = default)
        {
            var data = await _context.SeatsSeatrows.AsNoTracking().Select(r => new { id = r.Id, label = r.Label, floor = r.FloorId }).ToListAsync(ct);
            return ServiceResult<object>.Ok(data);
        }

        public async Task<ServiceResult<object>> AddRowAsync(long floorId, string label, int order, CancellationToken ct = default)
        {
            var row = new SeatsSeatrow {
                FloorId = floorId,
                Label = label,
                Order = order
            };
            _context.SeatsSeatrows.Add(row);
            await _context.SaveChangesAsync(ct);
            return ServiceResult<object>.Ok(row);
        }

        public async Task<ServiceResult<bool>> DeleteRowAsync(long pk, CancellationToken ct = default)
        {
            var row = await _context.SeatsSeatrows.Include(r => r.SeatsSeats).FirstOrDefaultAsync(r => r.Id == pk, ct);
            if (row != null)
            {
                foreach (var seat in row.SeatsSeats) {
                    var assignments = _context.SeatsSeatassignments.Where(a => a.SeatId == seat.Id);
                    _context.SeatsSeatassignments.RemoveRange(assignments);
                    var changelogs = _context.SeatsSeatchangelogs.Where(c => c.SeatId == seat.Id || c.PreviousSeatId == seat.Id);
                    _context.SeatsSeatchangelogs.RemoveRange(changelogs);
                }
                _context.SeatsSeats.RemoveRange(row.SeatsSeats);
                _context.SeatsSeatrows.Remove(row);
                await _context.SaveChangesAsync(ct);
                return ServiceResult<bool>.Ok(true);
            }
            return ServiceResult<bool>.NotFound("Row not found");
        }

        public async Task<ServiceResult<object>> UpdateFloorAsync(long pk, string name, string? description, int order, CancellationToken ct = default)
        {
            var floor = await _context.SeatsFloors.FindAsync(new object[] { pk }, ct);
            if (floor == null) return ServiceResult<object>.NotFound("Floor not found");

            floor.Name = name;
            floor.Description = description;
            floor.Order = order;

            await _context.SaveChangesAsync(ct);
            return ServiceResult<object>.Ok(floor);
        }

        public async Task<ServiceResult<object>> UpdateRowAsync(long pk, long floorId, string label, int order, CancellationToken ct = default)
        {
            var row = await _context.SeatsSeatrows.FindAsync(new object[] { pk }, ct);
            if (row == null) return ServiceResult<object>.NotFound("Row not found");

            row.FloorId = floorId;
            row.Label = label;
            row.Order = order;

            await _context.SaveChangesAsync(ct);
            return ServiceResult<object>.Ok(row);
        }

        public async Task<ServiceResult<object>> GetSeatDetailAsync(long pk, CancellationToken ct = default)
        {
            var seat = await _context.SeatsSeats
                .AsNoTracking()
                .Include(s => s.Student)
                .ThenInclude(st => st!.StudentsStudentprofile)
                .FirstOrDefaultAsync(s => s.Id == pk, ct);

            if (seat == null) return ServiceResult<object>.NotFound("Seat not found");

            return ServiceResult<object>.Ok(new {
                id = seat.Id,
                floor = seat.Floor,
                row = seat.Row,
                row_ref = seat.RowRefId,
                seat_number = seat.SeatNumber,
                status = seat.Status!.ToUpper(),
                student = seat.StudentId,
                student_name = seat.Student != null ? $"{seat.Student.FirstName} {seat.Student.LastName}".Trim() : null,
                assigned_at = seat.AssignedAt,
                notes = seat.Notes,
                is_reserved_for_girls = seat.IsReservedForGirls
            });
        }

        public async Task<ServiceResult<object>> GetSeatHistoryAsync(long pk, CancellationToken ct = default)
        {
            var history = await _context.SeatsSeatassignments
                .AsNoTracking()
                .Where(a => a.SeatId == pk)
                .Include(a => a.Student)
                .OrderByDescending(a => a.AssignedDate)
                .Select(a => new {
                    id = a.Id,
                    student = a.StudentId,
                    student_name = a.Student != null ? $"{a.Student.FirstName} {a.Student.LastName}".Trim() : null,
                    assigned_date = a.AssignedDate,
                    released_date = a.ReleasedDate,
                    is_active = a.ReleasedDate == null
                })
                .ToListAsync(ct);

            return ServiceResult<object>.Ok(history);
        }

        public async Task<ServiceResult<object>> GetFloorDetailAsync(long pk, CancellationToken ct = default)
        {
            var floor = await _context.SeatsFloors
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == pk, ct);

            if (floor == null) return ServiceResult<object>.NotFound("Floor not found");

            return ServiceResult<object>.Ok(new {
                id = floor.Id,
                name = floor.Name,
                description = floor.Description,
                order = floor.Order,
                is_active = floor.IsActive
            });
        }

        public async Task<ServiceResult<object>> GetRowDetailAsync(long pk, CancellationToken ct = default)
        {
            var row = await _context.SeatsSeatrows
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == pk, ct);

            if (row == null) return ServiceResult<object>.NotFound("Row not found");

            return ServiceResult<object>.Ok(new {
                id = row.Id,
                label = row.Label,
                floor = row.FloorId,
                order = row.Order
            });
        }
    }
}
