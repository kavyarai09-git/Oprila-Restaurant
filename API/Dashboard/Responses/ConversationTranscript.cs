namespace RestaurantApi.Dashboard.Responses
{
    public class ConversationTranscript
    {
        public int Id { get; set; }

        public int CallHistoryId { get; set; }

        public string Speaker { get; set; }

        public string Message { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
