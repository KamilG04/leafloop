using System;
using System.ComponentModel.DataAnnotations;

namespace LeafLoop.Models
{
    public class UserSession
    {
        public int Id { get; set; }
        
        public int UserId { get; set; }
        
        [Required]
        public string Token { get; set; }  // Identyfikator sesji lub token
        
        public string RefreshToken { get; set; }  // Opcjonalny refresh token
        
        public string UserAgent { get; set; }  // Informacje o przeglądarce/urządzeniu
        
        public string IpAddress { get; set; }  // Adres IP
        
        public DateTime LoginTime { get; set; }
        
        public DateTime? LogoutTime { get; set; }  // Null jeśli sesja wciąż aktywna
        
        public DateTime LastActivity { get; set; }  // Ostatnia aktywność w ramach sesji
        
        public bool IsActive { get; set; }  // Czy sesja jest aktywna
        
        // Relacje
        public virtual User User { get; set; }
    }
}