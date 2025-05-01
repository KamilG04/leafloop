// Pełna ścieżka: Data/LeafLoopDbContext.cs (KOMPLETNY I POPRAWIONY)

using LeafLoop.Models;
using LeafLoop.Services.DTOs; // Potrzebne dla modelBuilder.Ignore<PreferencesData>()
using Microsoft.AspNetCore.Identity; // Potrzebne dla Identity
using Microsoft.AspNetCore.Identity.EntityFrameworkCore; // Potrzebne dla IdentityDbContext lub konfiguracji tabel
using Microsoft.EntityFrameworkCore;

namespace LeafLoop.Data
{
    // Jeśli używasz Identity, dziedziczenie z IdentityDbContext<User, IdentityRole<int>, int> jest często wygodniejsze
    // public class LeafLoopDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    // Jeśli nie, pozostaw dziedziczenie z DbContext i konfiguruj tabele Identity ręcznie (jak masz teraz)
    public class LeafLoopDbContext : DbContext
    {
        public LeafLoopDbContext(DbContextOptions<LeafLoopDbContext> options)
            : base(options)
        {
        }

        // --- DbSets ---
        // Użytkownicy i Role (jeśli nie dziedziczysz z IdentityDbContext)
         public DbSet<User> Users { get; set; }
        // public DbSet<IdentityRole<int>> Roles { get; set; } // Jeśli potrzebujesz dostępu do ról

        // Główne Encje Aplikacji
        public DbSet<Item> Items { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<ItemTag> ItemTags { get; set; } // Tabela pośrednicząca
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<EventParticipant> EventParticipants { get; set; } // Tabela pośrednicząca
        public DbSet<Message> Messages { get; set; }
        public DbSet<Photo> Photos { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Badge> Badges { get; set; }
        public DbSet<UserBadge> UserBadges { get; set; } // Tabela pośrednicząca
        public DbSet<Report> Reports { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<SavedSearch> SavedSearches { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<UserPreferences> UserPreferences { get; set; }

        // --- Konfiguracja Modelu ---
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // WAŻNE: Jeśli *nie* dziedziczysz z IdentityDbContext, musisz wywołać base.OnModelCreating()
            // aby skonfigurować klucze i relacje IdentityUser. Jeśli dziedziczysz, jest to robione automatycznie.
             base.OnModelCreating(modelBuilder);

            // Ignoruj DTO, jeśli przypadkiem zostało dodane do modelu
            modelBuilder.Ignore<PreferencesData>(); // OK

            // --- Relacje Wiele-do-Wielu (Klucze złożone) ---
            modelBuilder.Entity<ItemTag>()
                .HasKey(it => new { it.ItemId, it.TagId });

            modelBuilder.Entity<EventParticipant>()
                .HasKey(ep => new { ep.EventId, ep.UserId });

            modelBuilder.Entity<UserBadge>()
                .HasKey(ub => new { ub.UserId, ub.BadgeId });

            // --- Konfiguracje Relacji (wybrane, ważne lub wymagające uwagi) ---

            // User <-> Address (Opcjonalna relacja 1-do-wielu)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Address)
                .WithMany(a => a.Users) // Zakładając, że Address ma ICollection<User> Users
                .HasForeignKey(u => u.AddressId)
                .IsRequired(false); // AddressId jest nullable

            // Item <-> User (Wymagana relacja 1-do-wielu)
            modelBuilder.Entity<Item>()
                .HasOne(i => i.User)
                .WithMany(u => u.Items) // Zakładając, że User ma ICollection<Item> Items
                .HasForeignKey(i => i.UserId)
                .IsRequired(); // Zakładamy, że każdy Item musi mieć Usera

            // Item <-> Category (Wymagana relacja 1-do-wielu)
            modelBuilder.Entity<Item>()
                .HasOne(i => i.Category)
                .WithMany(c => c.Items) // Zakładając, że Category ma ICollection<Item> Items
                .HasForeignKey(i => i.CategoryId)
                .IsRequired();

            // Transaction <-> User (Seller & Buyer) - Ograniczone usuwanie
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Seller)
                .WithMany(u => u.SellingTransactions) // Zakładając User.SellingTransactions
                .HasForeignKey(t => t.SellerId)
                .OnDelete(DeleteBehavior.Restrict); // Nie usuwaj Usera, jeśli jest Sprzedawcą

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Buyer)
                .WithMany(u => u.BuyingTransactions) // Zakładając User.BuyingTransactions
                .HasForeignKey(t => t.BuyerId)
                .OnDelete(DeleteBehavior.Restrict); // Nie usuwaj Usera, jeśli jest Kupującym

             // Transaction <-> Item
             modelBuilder.Entity<Transaction>()
                 .HasOne(t => t.Item)
                 .WithMany(i => i.Transactions) // Zakładając Item.Transactions
                 .HasForeignKey(t => t.ItemId)
                 .OnDelete(DeleteBehavior.Restrict); // Nie usuwaj Itemu, jeśli jest w Transakcji? Może lepiej Cascade lub SetNull?

            // Message <-> User (Sender & Receiver) - Ograniczone usuwanie
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages) // Zakładając User.SentMessages
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany(u => u.ReceivedMessages) // Zakładając User.ReceivedMessages
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            // UserBadge (Many-to-Many Configuration - częściowo już jest przez klucz złożony)
            modelBuilder.Entity<UserBadge>()
                .HasOne(ub => ub.User)
                .WithMany(u => u.UserBadges) // <<<--- UPEWNIJ SIĘ, że User ma UserBadges
                .HasForeignKey(ub => ub.UserId);

            modelBuilder.Entity<UserBadge>()
                .HasOne(ub => ub.Badge)
                .WithMany(b => b.Users) // Zakładając, że Badge ma ICollection<UserBadge> Users
                .HasForeignKey(ub => ub.BadgeId);

            // --- POPRAWKA DLA Rating ---
            modelBuilder.Entity<Rating>(entity =>
            {
                // Relacja dla Rater (kto ocenił)
                entity.HasOne(r => r.Rater) // Rating ma jednego Rater (typu User)
                      .WithMany(u => u.RatingsGiven) // User ma wiele RatingsGiven (ocen wystawionych) <<<--- UPEWNIJ SIĘ, ŻE User.RatingsGiven ISTNIEJE
                      .HasForeignKey(r => r.RaterId) // Klucz obcy to RaterId
                      .OnDelete(DeleteBehavior.Restrict); // Nie usuwaj Usera, jeśli wystawił oceny

                // Relacja dla Transaction (opcjonalna)
                entity.HasOne(r => r.Transaction) // Rating może (ale nie musi) należeć do jednej Transakcji
                      .WithMany(t => t.Ratings) // Transakcja ma wiele Ocen <<<--- UPEWNIJ SIĘ, ŻE Transaction.Ratings ISTNIEJE
                      .HasForeignKey(r => r.TransactionId) // Klucz obcy to TransactionId
                      .IsRequired(false) // Relacja jest opcjonalna (TransactionId jest nullable)
                      .OnDelete(DeleteBehavior.SetNull); // Jeśli usuniesz Transakcję, ustaw TransactionId w Rating na null

                // Relacja dla RatedEntity (User/Company) - NIE konfigurujemy przez HasOne/WithMany,
                // bo nie ma bezpośrednich właściwości nawigacyjnych w Rating.cs.
                // Relacja jest zdefiniowana przez RatedEntityId i RatedEntityType.
            });
            // --- KONIEC POPRAWKI DLA Rating ---


            // Relacje dla Report, Comment, Subscription, SavedSearch (zakładając, że User jest tylko po jednej stronie)
            modelBuilder.Entity<Report>()
                .HasOne(r => r.Reporter)
                .WithMany(u => u.ReportsMade) // Zakładając User.ReportsMade
                .HasForeignKey(r => r.ReporterId);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany(u => u.CommentsMade) // Zakładając User.CommentsMade
                .HasForeignKey(c => c.UserId);

            modelBuilder.Entity<Subscription>()
                .HasOne(s => s.User)
                .WithMany(u => u.Subscriptions) // Zakładając User.Subscriptions
                .HasForeignKey(s => s.UserId);

            modelBuilder.Entity<SavedSearch>()
                .HasOne(ss => ss.User)
                .WithMany(u => u.SavedSearches) // Zakładając User.SavedSearches
                .HasForeignKey(ss => ss.UserId);

            // UserSession <-> User
            modelBuilder.Entity<UserSession>()
                .HasOne(us => us.User)
                .WithMany(u => u.Sessions) // Zakładając User.Sessions
                .HasForeignKey(us => us.UserId);

            // UserPreferences <-> User (One-to-One)
            modelBuilder.Entity<UserPreferences>()
                .HasOne(up => up.User)
                .WithOne(u => u.Preferences) // Zakładając User.Preferences
                .HasForeignKey<UserPreferences>(up => up.UserId);

            // Photo <-> Item
             modelBuilder.Entity<Photo>()
                 .HasOne(p => p.Item)
                 .WithMany(i => i.Photos) // Zakładając Item.Photos
                 .HasForeignKey(p => p.ItemId)
                 .OnDelete(DeleteBehavior.Cascade); // Usuń zdjęcia, jeśli usunięto przedmiot

            // Konfiguracja tabel Identity (jeśli nie dziedziczysz z IdentityDbContext)
            // Te linie mogą być zbędne, jeśli base.OnModelCreating() je konfiguruje.
            // Sprawdź, czy nie powodują konfliktów.
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<IdentityRole<int>>().ToTable("Roles");
            modelBuilder.Entity<IdentityUserRole<int>>().ToTable("UserRoles");
            modelBuilder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
            modelBuilder.Entity<IdentityUserLogin<int>>().HasKey(l => new { l.LoginProvider, l.ProviderKey });
            modelBuilder.Entity<IdentityUserRole<int>>().HasKey(r => new { r.UserId, r.RoleId });
            modelBuilder.Entity<IdentityUserToken<int>>().HasKey(t => new { t.UserId, t.LoginProvider, t.Name });

            // TODO: Dodaj inne potrzebne konfiguracje (indeksy, ograniczenia unikalności itp.)
        }
    }
}