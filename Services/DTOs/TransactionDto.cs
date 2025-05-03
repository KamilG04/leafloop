using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        public TransactionType Type { get; set; }
        public string InitialMessage { get; set; }
    }
    public class TransactionStatusUpdateDto
    {
        // TransactionId może być niepotrzebne, bo mamy je w URL
        // public int TransactionId { get; set; }

        [Required]
        public TransactionStatus Status { get; set; } // Upewnij się, że TransactionStatus jest zdefiniowane
    }
    public class TransactionMessageDto
    {
        [Required]
        [MaxLength(1000)] // Przykładowy limit długości
        public string Content { get; set; } = null!;
    }

    public class TransactionRatingDto
    {
        [Required]
        [Range(1, 5)] // Zakładając ocenę 1-5
        public int Value { get; set; }

        [MaxLength(500)] // Przykładowy limit
        public string? Comment { get; set; } // Komentarz opcjonalny
    }
}
