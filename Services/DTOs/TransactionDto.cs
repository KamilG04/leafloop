using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LeafLoop.Models; 

namespace LeafLoop.Services.DTOs
{

    public class TransactionDto { 
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
        public bool BuyerConfirmed { get; set; }
        public bool SellerConfirmed { get; set; }   
     }


    public class TransactionWithDetailsDto : TransactionDto { 
        public UserDto Seller { get; set; }
        public UserDto Buyer { get; set; }
        public ItemDto Item { get; set; }
        public List<MessageDto> Messages { get; set; }
        public List<RatingDto> Ratings { get; set; }
     }


    public class TransactionCreateDto
    {
        [Required]
        public int ItemId { get; set; }

        [Required]
        public TransactionType Type { get; set; } // Typ jest wymagany

        // Usunięto: public string InitialMessage { get; set; }
        
        // public string? Offer { get; set; }
    }


    public class TransactionStatusUpdateDto { 
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
    public class TransactionRatingDto
    {
        [Required]
        [Range(1, 5, ErrorMessage = "Ocena musi być wartością od 1 do 5")]
        public int Value { get; set; }
        
        [MaxLength(500, ErrorMessage = "Komentarz nie może być dłuższy niż 500 znaków")]
        public string Comment { get; set; }
    }
}