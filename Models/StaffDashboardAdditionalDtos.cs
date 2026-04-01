namespace JWTAuthAPI.Models
{
    public class StaffQuickActionsDto
    {
        public int PendingInquiries { get; set; }
        public int OverdueInstallments { get; set; }
        public int InstallmentsDueToday { get; set; }
        public int BatchesStartingNext3Days { get; set; }
        public int UnmarkedActiveBatchesToday { get; set; }
        public int NewStudentsThisWeek { get; set; }
        public decimal PaymentsCollectedToday { get; set; }
    }

    public class StaffTimelineItemDto
    {
        public string EventType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Status { get; set; }
        public string? ActionUrl { get; set; }
    }
}
