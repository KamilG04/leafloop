// Create new file: Utilities/FileValidationHelper.cs


namespace LeafLoop.Middleware
{
    public static class FileValidationHelper
    {
        public static async Task<bool> IsValidImageFileAsync(IFormFile file, int maxSizeMB = 5)
        {
            if (file == null || file.Length == 0)
                return false;
                
            // Check file size (default 5MB max)
            if (file.Length > maxSizeMB * 1024 * 1024)
                return false;
                
            // Check MIME type
            var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
                return false;
                
            // Check file header (magic bytes)
            byte[] fileHeaderBytes = new byte[12]; // Enough for most image headers
            
            using (var stream = file.OpenReadStream())
            {
                if (stream.Length < 12)
                    return false;
                    
                await stream.ReadExactlyAsync(fileHeaderBytes, 0, 12);
                
                // Reset stream position for later use
                stream.Position = 0;
            }
            
            // Check JPG header (FF D8 FF)
            bool isJpg = fileHeaderBytes[0] == 0xFF && fileHeaderBytes[1] == 0xD8 && fileHeaderBytes[2] == 0xFF;
            
            // Check PNG header (89 50 4E 47 0D 0A 1A 0A)
            bool isPng = fileHeaderBytes[0] == 0x89 && fileHeaderBytes[1] == 0x50 && 
                         fileHeaderBytes[2] == 0x4E && fileHeaderBytes[3] == 0x47 &&
                         fileHeaderBytes[4] == 0x0D && fileHeaderBytes[5] == 0x0A &&
                         fileHeaderBytes[6] == 0x1A && fileHeaderBytes[7] == 0x0A;
            
            // Check WEBP header
            bool isWebp = fileHeaderBytes[0] == 0x52 && fileHeaderBytes[1] == 0x49 && 
                          fileHeaderBytes[2] == 0x46 && fileHeaderBytes[3] == 0x46 &&
                          fileHeaderBytes[8] == 0x57 && fileHeaderBytes[9] == 0x45 && 
                          fileHeaderBytes[10] == 0x42 && fileHeaderBytes[11] == 0x50;
            
            return isJpg || isPng || isWebp;
        }
    }
}