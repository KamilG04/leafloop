using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeafLoop.Models
{
    [NotMapped] // This tells EF Core not to treat this class as an entity
    public class PreferencesData
    {
        public string Theme { get; set; } = "light"; 
        public string PrimaryColor { get; set; } = "green"; 
        
        // Ustawienia powiadomień
        public bool EmailNotifications { get; set; } = true;
        public bool PushNotifications { get; set; } = true;
        public bool TransactionUpdates { get; set; } = true;
        public bool NewMessageNotifications { get; set; } = true;
        public bool EventReminders { get; set; } = true;
        
        // Ustawienia prywatności
        public bool ShowEcoScore { get; set; } = true;
        public bool ShowLocation { get; set; } = false;
        public bool ShowLastActivity { get; set; } = true;
        
        // Preferencje językowe
        public string Language { get; set; } = "pl-PL";
        
        // Preferencje wyświetlania
        public int PageSize { get; set; } = 12; // Liczba elementów na stronę
        public string DefaultSort { get; set; } = "newest"; // newest, oldest, price_asc, price_desc
        
        // Preferencje transakcji
        public bool AutoAcceptMessages { get; set; } = true;
        public decimal MaxAutoAcceptValue { get; set; } = 100.00M; // Maksymalna wartość do auto-akceptacji
    }
}