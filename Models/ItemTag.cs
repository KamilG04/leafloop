namespace LeafLoop.Models
{
    public class ItemTag
    {
        public int ItemId { get; set; }
        public int TagId { get; set; }
        
        // Relacje
        public virtual Item Item { get; set; }
        public virtual Tag Tag { get; set; }
    }
}
