namespace LeafLoop.Models
{
    public enum ParticipationStatus
    {
        Registered,
        Confirmed,
        Attended,
        Cancelled
    }
    
    public class EventParticipant
    {
        public int EventId { get; set; }
        public int UserId { get; set; }
        public ParticipationStatus ParticipationStatus { get; set; }
        
        // Relacje
        public virtual Event Event { get; set; }
        public virtual User User { get; set; }
    }
}
