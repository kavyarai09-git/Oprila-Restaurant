// ================================================
// FileName:        AppointmentService.cs
// Project:         Restaurant Booking System
// Namespace:       RestaurantApi.AppointmentBooking.Services
// Description:     Service for handling appointment logic.
// Created:         22/04/2026
// Modified:        22/04/2026
// API Version:     v1.0.0
// Author:          Deviprasad Rai P (deviprasadr@ccsym.com)
// Last Modified:   Deviprasad Rai P (deviprasadr@ccsym.com)
// ================================================

using Microsoft.EntityFrameworkCore;
using RestaurantApi.AppointmentBooking.Contracts.Requests;
using RestaurantApi.AppointmentBooking.Contracts.Responses;
using RestaurantApi.AppointmentBooking.Enums;
using RestaurantApi.AppointmentBooking.Models;
using RestaurantApi.Dashboard.Responses;
using RestaurantApi.Shared.Responses;
using RestaurantApi.Shared.Data;

namespace RestaurantApi.AppointmentBooking.Services;

public class AppointmentService(AppDbContext db) : IAppointmentService
{
    private static readonly TimeSpan DefaultSlotDuration = TimeSpan.FromHours(1.5);
    private static readonly TimeSpan SlotStep = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan OpeningTime = new(12, 0, 0);
    private static readonly TimeSpan ClosingTime = new(22, 0, 0);

    private readonly AppDbContext _db = db;

    // ═══════════════════════════════════════════════════════════════
    //  QUERIES
    // ═══════════════════════════════════════════════════════════════

    public async Task<PagedResponse<AppointmentResponse>> GetAllAsync(
        int page, int pageSize,
        DateTime? date, string? status, string? search, string? source)
    {
        var q = _db.Appointments.Include(a => a.Table).AsQueryable();

        if (date.HasValue)
            q = q.Where(a => a.AppointmentDate.Date == date.Value.Date);

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<AppointmentStatus>(status, true, out var st))
            q = q.Where(a => a.Status == st);

        if (!string.IsNullOrWhiteSpace(source) &&
            Enum.TryParse<BookingSource>(source, true, out var src))
            q = q.Where(a => a.Source == src);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            q = q.Where(a => a.CustomerName.ToLower().Contains(s)
                           || a.CustomerPhone.Contains(s)
                           || (a.CustomerEmail != null && a.CustomerEmail.ToLower().Contains(s)));
        }

        var total = await q.CountAsync();

  var allItems = await q.ToListAsync();

  var items = allItems
      .OrderByDescending(a => a.AppointmentDate)
      .ThenBy(a => a.StartTime)
      .Skip((page - 1) * pageSize)
      .Take(pageSize)
      .ToList();
        return new PagedResponse<AppointmentResponse>
        {
            Items = items.Select(Map).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }

    public async Task<AppointmentResponse?> GetByIdAsync(int id)
    {
        var a = await _db.Appointments.Include(x => x.Table).FirstOrDefaultAsync(x => x.Id == id);
        return a is null ? null : Map(a);
    }

    public async Task<List<AppointmentResponse>> GetTodayAsync()
    {
        var today = DateTime.UtcNow.Date;
        var list = await _db.Appointments
            .Include(a => a.Table)
            .Where(a => a.AppointmentDate.Date == today && a.Status != AppointmentStatus.Cancelled)
            .OrderBy(a => a.StartTime)
            .ToListAsync();
        return list.Select(Map).ToList();
    }

    public async Task<DashboardStatsResponse> GetDashboardAsync()
    {
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var weekStart = todayStart.AddDays(-(int)now.DayOfWeek);
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var all = await _db.Appointments.Include(a => a.Table).ToListAsync();

        var upcoming = all
            .Where(a => a.AppointmentDate.Date == todayStart
                     && a.Status == AppointmentStatus.Confirmed
                     && a.StartTime >= now.TimeOfDay)
            .OrderBy(a => a.StartTime)
            .Take(10)
            .Select(Map)
            .ToList();

        return new DashboardStatsResponse
        {
            TotalToday = all.Count(a => a.AppointmentDate.Date == todayStart),
            TotalThisWeek = all.Count(a => a.AppointmentDate >= weekStart),
            TotalThisMonth = all.Count(a => a.AppointmentDate >= monthStart),
            PendingConfirmations = all.Count(a => a.Status == AppointmentStatus.Pending),
            CancelledToday = all.Count(a => a.AppointmentDate.Date == todayStart && a.Status == AppointmentStatus.Cancelled),
            TotalGuestsConfirmed = all.Where(a => a.Status == AppointmentStatus.Confirmed).Sum(a => a.GuestCount),
            AppointmentsBySource = all.GroupBy(a => a.Source.ToString()).ToDictionary(g => g.Key, g => g.Count()),
            AppointmentsByStatus = all.GroupBy(a => a.Status.ToString()).ToDictionary(g => g.Key, g => g.Count()),
            UpcomingToday = upcoming
        };
    }

    public async Task<List<AvailableSlotResponse>> GetAvailabilityAsync(AvailabilityQueryRequest req)
    {
        var slots = GenerateAllSlots();

        // All active bookings on the requested date
        var bookings = await _db.Appointments
            .Where(a => a.AppointmentDate.Date == req.Date.Date
                     && a.Status != AppointmentStatus.Cancelled)
            .ToListAsync();

        // Tables that can accommodate the party
        var tables = await _db.Tables
            .Where(t => t.IsActive && t.Capacity >= req.GuestCount)
            .OrderBy(t => t.Capacity)
            .ToListAsync();

        var result = new List<AvailableSlotResponse>();

        foreach (var (start, end) in slots)
        {
            foreach (var table in tables)
            {
                var occupied = bookings.Any(b =>
                    b.TableId == table.Id &&
                    b.StartTime < end && b.EndTime > start);

                if (!occupied)
                {
                    result.Add(new AvailableSlotResponse
                    {
                        StartTime = Fmt(start),
                        EndTime = Fmt(end),
                        TableId = table.Id,
                        TableNumber = table.TableNumber,
                        Location = table.Location,
                        Capacity = table.Capacity
                    });
                    break; // one table per slot is enough for availability listing
                }
            }
        }

        return result;
    }

    // ═══════════════════════════════════════════════════════════════
    //  COMMANDS
    // ═══════════════════════════════════════════════════════════════

    public async Task<AppointmentResponse> CreateAsync(CreateAppointmentRequest req, string? callSessionId = null)
    {
        if (req.AppointmentDate.Date < DateTime.UtcNow.Date)
            throw new InvalidOperationException("Cannot book for past dates.");

        var start = TimeSpan.Parse(req.StartTime);
        var end = TimeSpan.Parse(req.EndTime);

        if (end <= start)
            throw new InvalidOperationException("End time must be after start time.");

        // Auto-assign table if not specified
        var tableId = req.TableId;
        if (!tableId.HasValue)
            tableId = await FindAvailableTableAsync(req.AppointmentDate.Date, start, end, req.GuestCount);

        // Validate chosen table is free
        if (tableId.HasValue)
        {
            var conflict = await _db.Appointments.AnyAsync(a =>
                a.TableId == tableId &&
                a.AppointmentDate.Date == req.AppointmentDate.Date &&
                a.Status != AppointmentStatus.Cancelled &&
                a.StartTime < end && a.EndTime > start);

            if (conflict)
                throw new InvalidOperationException("The selected table is not available for this time slot.");
        }

        if (!Enum.TryParse<BookingSource>(req.Source, true, out var source))
            source = BookingSource.Web;

        var entity = new AppointmentDetails
        {
            CustomerName = req.CustomerName.Trim(),
            CustomerPhone = req.CustomerPhone.Trim(),
            CustomerEmail = req.CustomerEmail?.Trim().ToLowerInvariant(),
            GuestCount = req.GuestCount,
            AppointmentDate = req.AppointmentDate.Date,
            StartTime = start,
            EndTime = end,
            TableId = tableId,
            Status = AppointmentStatus.Confirmed,
            Source = source,
            SpecialRequests = req.SpecialRequests?.Trim(),
            CallSessionId = callSessionId
        };

        _db.Appointments.Add(entity);
        await _db.SaveChangesAsync();
        await _db.Entry(entity).Reference(a => a.Table).LoadAsync();
        return Map(entity);
    }

    public async Task<AppointmentResponse?> UpdateAsync(int id, UpdateAppointmentRequest req)
    {
        var entity = await _db.Appointments.Include(a => a.Table).FirstOrDefaultAsync(a => a.Id == id);
        if (entity is null) return null;

        if (req.CustomerName is not null) entity.CustomerName = req.CustomerName.Trim();
        if (req.CustomerPhone is not null) entity.CustomerPhone = req.CustomerPhone.Trim();
        if (req.CustomerEmail is not null) entity.CustomerEmail = req.CustomerEmail.Trim().ToLowerInvariant();
        if (req.GuestCount.HasValue) entity.GuestCount = req.GuestCount.Value;
        if (req.AppointmentDate.HasValue) entity.AppointmentDate = req.AppointmentDate.Value.Date;
        if (req.StartTime is not null) entity.StartTime = TimeSpan.Parse(req.StartTime);
        if (req.EndTime is not null) entity.EndTime = TimeSpan.Parse(req.EndTime);
        if (req.TableId.HasValue) entity.TableId = req.TableId.Value;
        if (req.SpecialRequests is not null) entity.SpecialRequests = req.SpecialRequests.Trim();
        if (req.InternalNotes is not null) entity.InternalNotes = req.InternalNotes.Trim();

        if (!string.IsNullOrWhiteSpace(req.Status) &&
            Enum.TryParse<AppointmentStatus>(req.Status, true, out var st))
            entity.Status = st;

        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _db.Entry(entity).Reference(a => a.Table).LoadAsync();
        return Map(entity);
    }

    public async Task<bool> CancelAsync(int id, string? reason = null)
    {
        var entity = await _db.Appointments.FindAsync(id);
        if (entity is null) return false;

        entity.Status = AppointmentStatus.Cancelled;
        entity.InternalNotes = reason ?? entity.InternalNotes;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<RecentActivityResponses>> GetRecentActivityAsync()
    {
        return await Task.FromResult(new List<RecentActivityResponses>
    {
        new()
        {
            Id = 1,
            CallerPhone = "+91 9876543210",
            Status = "Completed",
            StartedAt = DateTime.UtcNow.AddMinutes(-30),
            EndedAt = DateTime.UtcNow.AddMinutes(-25)
        },
        new()
        {
            Id = 2,
            CallerPhone = "+91 9876501234",
            Status = "Pending",
            StartedAt = DateTime.UtcNow.AddMinutes(-20),
            EndedAt = null
        },
        new()
        {
            Id = 3,
            CallerPhone = "+91 9123456789",
            Status = "Failed",
            StartedAt = DateTime.UtcNow.AddMinutes(-10),
            EndedAt = DateTime.UtcNow.AddMinutes(-9)
        }
    });
    }

    public async Task<List<ConversationTranscript>> GetConversationTranscriptAsync()
    {
        return await Task.FromResult(new List<ConversationTranscript>
    {
        new()
        {
            Id = 1,
            Speaker = "AI Assistant",
            Message = "Good afternoon. Thank you for calling Maître D' Pro Bistro. How may I assist you with your reservation today?",
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        },
        new()
        {
            Id = 2,
            Speaker = "Customer",
            Message = "Hi, I'd like to book a table for four people tonight around 8 PM.",
            CreatedAt = DateTime.UtcNow.AddMinutes(-9)
        },
        new()
        {
            Id = 3,
            Speaker = "AI Assistant",
            Message = "Certainly. May I have your name, please?",
            CreatedAt = DateTime.UtcNow.AddMinutes(-8)
        },
        new()
        {
            Id = 4,
            Speaker = "Customer",
            Message = "My name is Rahul.",
            CreatedAt = DateTime.UtcNow.AddMinutes(-7)
        }
    });
    }

    // ═══════════════════════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════════════════════

    private async Task<int?> FindAvailableTableAsync(
        DateTime date, TimeSpan start, TimeSpan end, int guestCount)
    {
        var booked = await _db.Appointments
            .Where(a => a.AppointmentDate.Date == date
                     && a.Status != AppointmentStatus.Cancelled
                     && a.StartTime < end && a.EndTime > start)
            .Select(a => a.TableId)
            .ToListAsync();

        return await _db.Tables
            .Where(t => t.IsActive && t.Capacity >= guestCount && !booked.Contains(t.Id))
            .OrderBy(t => t.Capacity)
            .Select(t => (int?)t.Id)
            .FirstOrDefaultAsync();
    }

    private static IEnumerable<(TimeSpan start, TimeSpan end)> GenerateAllSlots()
    {
        var current = OpeningTime;
        while (current + DefaultSlotDuration <= ClosingTime)
        {
            yield return (current, current + DefaultSlotDuration);
            current += SlotStep;
        }
    }

    private static string Fmt(TimeSpan t) => $"{t.Hours:D2}:{t.Minutes:D2}";

    private static AppointmentResponse Map(AppointmentDetails a) => new()
    {
        Id = a.Id,
        CustomerName = a.CustomerName,
        CustomerPhone = a.CustomerPhone,
        CustomerEmail = a.CustomerEmail,
        GuestCount = a.GuestCount,
        AppointmentDate = a.AppointmentDate.ToString("yyyy-MM-dd"),
        StartTime = Fmt(a.StartTime),
        EndTime = Fmt(a.EndTime),
        TableId = a.TableId,
        TableNumber = a.Table?.TableNumber,
        TableLocation = a.Table?.Location,
        Status = a.Status.ToString(),
        Source = a.Source.ToString(),
        SpecialRequests = a.SpecialRequests,
        InternalNotes = a.InternalNotes,
        CallSessionId = a.CallSessionId,
        CreatedAt = a.CreatedAt,
        UpdatedAt = a.UpdatedAt
    };
}