// ================================================
// FileName:        AdminAppointmentsController.cs
// Project:         Restaurant Booking System
// Namespace:       RestaurantApi.AppointmentBooking.Controllers
// Description:     Admin controller for managing restaurant appointments with full CRUD, filtering, pagination, and dashboard stats.
// Created:         23/04/2026
// Modified:        23/04/2026
// API Version:     v1.0.0
// Author:          Deviprasad Rai P (deviprasadr@ccsym.com)
// Last Modified:   Deviprasad Rai P (deviprasadr@ccsym.com)
// ================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantApi.AppointmentBooking.Contracts.Requests;
using RestaurantApi.AppointmentBooking.Contracts.Responses;
using RestaurantApi.AppointmentBooking.Services;
using RestaurantApi.Dashboard.Responses;
using RestaurantApi.Shared.Responses;

namespace RestaurantApi.AppointmentBooking.Controllers;

/// <summary>
/// Admin / Staff appointment management with full filtering, pagination, and dashboard stats.
/// </summary>
[ApiController]
[Route("api/admin/appointments")]
[Authorize(Roles = "Admin,Staff")]
[Produces("application/json")]
public class AdminAppointmentsController(IAppointmentService svc) : ControllerBase
{
    private readonly IAppointmentService _svc = svc;

    /// <summary>
    /// Paginated list of all appointments with optional filters.
    /// Supports: date, status (Pending|Confirmed|Cancelled|Completed|NoShow), source, search (name/phone/email).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<AppointmentResponse>>), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTime? date = null,
        [FromQuery] string? status = null,
        [FromQuery] string? source = null,
        [FromQuery] string? search = null)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var result = await _svc.GetAllAsync(page, pageSize, date, status, search, source);
        return Ok(ApiResponse<PagedResponse<AppointmentResponse>>.Ok(result));
    }

    /// <summary>Get today's confirmed schedule ordered by start time.</summary>
    [HttpGet("today")]
    [ProducesResponseType(typeof(ApiResponse<List<AppointmentResponse>>), 200)]
    public async Task<IActionResult> Today()
    {
        var data = await _svc.GetTodayAsync();
        return Ok(ApiResponse<List<AppointmentResponse>>.Ok(data));
    }

    /// <summary>Dashboard statistics: counts, guests, sources breakdown. [Admin only]</summary>
    [HttpGet("dashboard")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<DashboardStatsResponse>), 200)]
    public async Task<IActionResult> Dashboard()
    {
        var data = await _svc.GetDashboardAsync();
        return Ok(ApiResponse<DashboardStatsResponse>.Ok(data));
    }

    [HttpGet("recent-activities")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRecentActivities()
    {
        var result = await _svc.GetRecentActivityAsync();

        return Ok(new
        {
            success = true,
            message = "Success",
            data = result
        });
    }

    [HttpGet("conversation-transcript")]
    [AllowAnonymous]
    public async Task<IActionResult> GetConversationTranscript()
    {
        var result = await _svc.GetConversationTranscriptAsync();
        return Ok(result);
    }

    /// <summary>Get a single appointment with full detail. [Admin | Staff]</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<AppointmentResponse>), 200)]
    public async Task<IActionResult> Get(int id)
    {
        var data = await _svc.GetByIdAsync(id);
        if (data is null) return NotFound(ApiResponse<object>.Fail("Appointment not found."));
        return Ok(ApiResponse<AppointmentResponse>.Ok(data));
    }

    /// <summary>
    /// Full update of an appointment (any field optional).
    /// Status values: Pending | Confirmed | Cancelled | Completed | NoShow
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<AppointmentResponse>), 200)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAppointmentRequest req)
    {
        var data = await _svc.UpdateAsync(id, req);
        if (data is null) return NotFound(ApiResponse<object>.Fail("Appointment not found."));
        return Ok(ApiResponse<AppointmentResponse>.Ok(data, "Appointment updated."));
    }

    /// <summary>Convenience endpoint – update just the status field.</summary>
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(ApiResponse<AppointmentResponse>), 200)]
    public async Task<IActionResult> SetStatus(int id, [FromBody] UpdateAppointmentRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Status))
            return BadRequest(ApiResponse<object>.Fail("status field is required."));

        var data = await _svc.UpdateAsync(id, req);
        if (data is null) return NotFound(ApiResponse<object>.Fail("Appointment not found."));
        return Ok(ApiResponse<AppointmentResponse>.Ok(data, "Status updated."));
    }

    /// <summary>Cancel an appointment with an optional reason stored in internal notes.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> Cancel(int id, [FromQuery] string? reason = null)
    {
        var ok = await _svc.CancelAsync(id, reason);
        if (!ok) return NotFound(ApiResponse<object>.Fail("Appointment not found."));
        return Ok(ApiResponse<object>.Ok(null!, "Appointment cancelled."));
    }
}