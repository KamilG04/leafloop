namespace LeafLoop.Services.DTOs.Preferences
{
    public class ThemeUpdateDto
    {
        public string Theme { get; set; }
    }

    public class EmailNotificationsUpdateDto
    {
        public bool Enabled { get; set; }
    }

    public class LanguageUpdateDto
    {
        public string Language { get; set; }
    }
    
    public class ThemeResponseDto
    {
        public string Theme { get; set; }
    }
}