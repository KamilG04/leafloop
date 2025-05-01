using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LeafLoop.Models
{
    public class UserPreferences
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        
        // Zapisujemy jako JSON, aby umożliwić elastyczne dodawanie nowych preferencji
        public string PreferencesJson { get; set; }
        
        public DateTime LastUpdated { get; set; }
        
        // Relacje
        public virtual User User { get; set; }
        
        // Pomocnicze metody do deserializacji/serializacji preferencji
        [JsonIgnore]
        public PreferencesData Preferences
        {
            get
            {
                if (string.IsNullOrEmpty(PreferencesJson))
                    return new PreferencesData();
                
                try
                {
                    return JsonSerializer.Deserialize<PreferencesData>(PreferencesJson);
                }
                catch
                {
                    return new PreferencesData();
                }
            }
            set
            {
                PreferencesJson = JsonSerializer.Serialize(value);
                LastUpdated = DateTime.UtcNow;
            }
        }
    }
}