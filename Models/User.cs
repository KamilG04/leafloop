// Pełna ścieżka: Models/User.cs (POPRAWIONY)

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace LeafLoop.Models
{
    public class User : IdentityUser<int>
    {
        // IdentityUser already includes: Id, UserName, Email, PasswordHash, etc.

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastActivity { get; set; }
        public int EcoScore { get; set; }
        public string AvatarPath { get; set; }
        public bool IsActive { get; set; }
        public int? AddressId { get; set; } // Klucz obcy dla opcjonalnego adresu
        
        
        public decimal SearchRadius { get; set; } = 10; 

        // --- Relacje ---

        // One-to-One / One-to-Many Navigation Properties
        public virtual Address Address { get; set; }
        public virtual UserPreferences Preferences { get; set; } // Zakładając istnienie UserPreferences
        public virtual ICollection<Item> Items { get; set; }
        public virtual ICollection<Transaction> SellingTransactions { get; set; }
        public virtual ICollection<Transaction> BuyingTransactions { get; set; }
        public virtual ICollection<Message> SentMessages { get; set; }
        public virtual ICollection<Message> ReceivedMessages { get; set; }
        public virtual ICollection<EventParticipant> EventParticipations { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; }
        public virtual ICollection<UserSession> Sessions { get; set; } // Zakładając istnienie UserSession
        public virtual ICollection<Report> ReportsMade { get; set; } // Zakładając, że User może zgłaszać
        public virtual ICollection<Comment> CommentsMade { get; set; } // Zakładając, że User może komentować
        public virtual ICollection<Subscription> Subscriptions { get; set; } // Zakładając istnienie Subscription
        public virtual ICollection<SavedSearch> SavedSearches { get; set; } // Zakładając istnienie SavedSearch
        public virtual ICollection<Rating> RatingsGiven { get; set; } // Oceny wystawione przez użytkownika
        public virtual ICollection<Rating> RatingsReceived { get; set; } // Oceny otrzymane przez użytkownika (wymaga konfiguracji w DbContext)


        // --- Many-to-Many Navigation Property (Junction Entity Collection) ---
        // <<< --- DODAJ TĘ WŁAŚCIWOŚĆ --- >>>
        public virtual ICollection<UserBadge> UserBadges { get; set; }
        // --- KONIEC DODANEJ WŁAŚCIWOŚCI ---


        // --- Konstruktor ---
        public User()
        {
            CreatedDate = DateTime.UtcNow;
            LastActivity = DateTime.UtcNow;
            EcoScore = 0;
            IsActive = true;

            // Inicjalizacja wszystkich kolekcji
            Items = new List<Item>();
            SellingTransactions = new List<Transaction>();
            BuyingTransactions = new List<Transaction>();
            SentMessages = new List<Message>();
            ReceivedMessages = new List<Message>();
            EventParticipations = new List<EventParticipant>();
            Notifications = new List<Notification>();
            Sessions = new List<UserSession>();
            ReportsMade = new List<Report>();
            CommentsMade = new List<Comment>();
            Subscriptions = new List<Subscription>();
            SavedSearches = new List<SavedSearch>();
            RatingsGiven = new List<Rating>();
            RatingsReceived = new List<Rating>();

            // <<< --- DODAJ INICJALIZACJĘ UserBadges --- >>>
            UserBadges = new List<UserBadge>();
        }
    }
}