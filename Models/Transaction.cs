namespace LeafLoop.Models
{
    public enum TransactionStatus
    {
        Pending,
        InProgress,
        Completed,
        Cancelled
    }

    public enum TransactionType
    {
        Exchange, // 0
        Donation, // 1
        Sale      // 2
    }

    public class Transaction
    {
        public int Id { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public TransactionStatus Status { get; set; }

        public DateTime LastUpdateDate { get; set; }
        public TransactionType Type { get; set; }

        public int SellerId { get; set; }

        public int BuyerId { get; set; }

        public int ItemId { get; set; }
        
        public bool BuyerConfirmed { get; set; }
        public bool SellerConfirmed { get; set; }
        
        public virtual User Seller { get; set; }
        public virtual User Buyer { get; set; }
        public virtual Item Item { get; set; }
        public virtual ICollection<Rating> Ratings { get; set; }
        public virtual ICollection<Message> Messages { get; set; }
    }
}