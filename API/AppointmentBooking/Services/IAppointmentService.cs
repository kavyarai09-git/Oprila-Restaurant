// ================================================
// FileName:        IAppointmentService.cs
// Project:         Restaurant Booking System
// Namespace:       RestaurantApi.AppointmentBooking.Services
// Description:     Service for handling appointment logic.
// Created:         22/04/2026
// Modified:        22/04/2026
// API Version:     v1.0.0
// Author:          Deviprasad Rai P (deviprasadr@ccsym.com)
// Last Modified:   Deviprasad Rai P (deviprasadr@ccsym.com)
// ================================================

using RestaurantApi.AppointmentBooking.Contracts.Requests;
using RestaurantApi.AppointmentBooking.Contracts.Responses;
using RestaurantApi.Dashboard.Responses;
using RestaurantApi.Shared.Responses;

namespace RestaurantApi.AppointmentBooking.Services;

public interface IAppointmentService
{
    Task<PagedResponse<AppointmentResponse>> GetAllAsync(
        int page, int pageSize,
        DateTime? date = null,
        string? status = null,
        string? search = null,
        string? source = null);

    Task<AppointmentResponse?> GetByIdAsync(int id);
    Task<List<AppointmentResponse>> GetTodayAsync();
    Task<DashboardStatsResponse> GetDashboardAsync();
    Task<List<RecentActivityResponses>>GetRecentActivityAsync();
    Task<List<ConversationTranscript>> GetConversationTranscriptAsync();

    Task<List<AvailableSlotResponse>> GetAvailabilityAsync(AvailabilityQueryRequest req);

    Task<AppointmentResponse> CreateAsync(CreateAppointmentRequest req, string? callSessionId = null);
    Task<AppointmentResponse?> UpdateAsync(int id, UpdateAppointmentRequest req);
    Task<bool> CancelAsync(int id, string? reason = null);
}