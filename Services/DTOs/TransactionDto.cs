using System;
using System.Collections.Generic;
using LeafLoop.Models;

namespace LeafLoop.Services.DTOs
{
    public class TransactionDto
    {
        public int Id { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public TransactionStatus Status { get; set; }
        public TransactionType Type { get; set; }
        public int SellerId { get; set; }
        public string SellerName { get; set; }
        public int BuyerId { get; set; }
        public string BuyerName { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public string ItemPhotoPath { get; set; }
    }

    public class TransactionWithDetailsDto : TransactionDto
    {
        public UserDto Seller { get; set; }
        public UserDto Buyer { get; set; }
        public ItemDto Item { get; set; }
        public List<MessageDto> Messages { get; set; }
        public List<RatingDto> Ratings { get; set; }
    }

    public class TransactionCreateDto
    {
        public int ItemId { get; set; }
        public int BuyerId { get; set; }
        public TransactionType Type { get; set; }
        public string InitialMessage { get; set; }
    }
}
