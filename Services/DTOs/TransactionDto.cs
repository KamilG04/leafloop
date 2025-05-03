using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LeafLoop.Models; // Dla TransactionStatus, TransactionType

namespace LeafLoop.Services.DTOs
{
    // TransactionDto (bez zmian)
    public class TransactionDto { /* ... jak poprzednio ... */
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

    // TransactionWithDetailsDto (bez zmian)
    public class TransactionWithDetailsDto : TransactionDto { /* ... jak poprzednio ... */
        public UserDto Seller { get; set; }
        public UserDto Buyer { get; set; }
        public ItemDto Item { get; set; }
        public List<MessageDto> Messages { get; set; }
        public List<RatingDto> Ratings { get; set; }
     }

    // TransactionCreateDto (POPRAWIONE)
    public class TransactionCreateDto
    {
        [Required]
        public int ItemId { get; set; }

        [Required]
        public TransactionType Type { get; set; } // Typ jest wymagany

        // Usunięto: public string InitialMessage { get; set; }
        // Opcjonalnie dodaj Offer, jeśli dodałeś do modelu Transaction
        // public string? Offer { get; set; }
    }

    // TransactionStatusUpdateDto (bez zmian)
    public class TransactionStatusUpdateDto { /* ... jak poprzednio ... */
          [Required]
        public TransactionStatus Status { get; set; }
     }

    // TransactionMessageDto (bez zmian)
    public class TransactionMessageDto { /* ... jak poprzednio ... */
         [Required]
        [MaxLength(1000)]
        public string Content { get; set; } = null!;
    }

    // TransactionRatingDto (bez zmian)
    public class TransactionRatingDto { /* ... jak poprzednio ... */
         [Required]
        [Range(1, 5)]
        public int Value { get; set; }
        [MaxLength(500)]
        public string? Comment { get; set; }
    }
}