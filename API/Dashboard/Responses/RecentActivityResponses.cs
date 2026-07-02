using System;

namespace RestaurantApi.Dashboard.Responses;

public class RecentActivityResponses
{
    public int Id { get; set; }
    public string CallerPhone { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}