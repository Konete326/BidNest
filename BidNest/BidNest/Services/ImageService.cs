namespace BidNest.Services
{
    public class ImageService : IImageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ImageService> _logger;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private readonly long _maxFileSize = 5 * 1024 * 1024; // 5MB

        public ImageService(IWebHostEnvironment environment, ILogger<ImageService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<string> UploadImageAsync(IFormFile file, string folder = "items")
        {
            if (!IsValidImageFile(file))
            {
                throw new ArgumentException("Invalid image file");
            }

            try
            {
                // Create upload directory if it doesn't exist
                var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", folder);
                Directory.CreateDirectory(uploadPath);

                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName).ToLower()}";
                var filePath = Path.Combine(uploadPath, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Return relative path for database storage
                return $"/uploads/{folder}/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image: {FileName}", file.FileName);
                throw new InvalidOperationException("Failed to upload image", ex);
            }
        }

        public async Task<List<string>> UploadImagesAsync(List<IFormFile> files, string folder = "items")
        {
            var uploadedPaths = new List<string>();

            foreach (var file in files)
            {
                try
                {
                    var path = await UploadImageAsync(file, folder);
                    uploadedPaths.Add(path);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading image: {FileName}", file.FileName);
                   
                }
            }

            return uploadedPaths;
        }

        public async Task<bool> DeleteImageAsync(string imagePath)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath))
                    return false;

                var fullPath = Path.Combine(_environment.WebRootPath, imagePath.TrimStart('/'));
                
                if (File.Exists(fullPath))
                {
                    await Task.Run(() => File.Delete(fullPath));
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image: {ImagePath}", imagePath);
                return false;
            }
        }

        public async Task<bool> DeleteImagesAsync(List<string> imagePaths)
        {
            var allDeleted = true;

            foreach (var imagePath in imagePaths)
            {
                var deleted = await DeleteImageAsync(imagePath);
                if (!deleted)
                    allDeleted = false;
            }

            return allDeleted;
        }

        public string GetImageUrl(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
                return "/images/no-image.png"; // Default placeholder

            return imagePath.StartsWith("/") ? imagePath : $"/{imagePath}";
        }

        public bool IsValidImageFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;

            if (file.Length > _maxFileSize)
                return false;

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!_allowedExtensions.Contains(extension))
                return false;

            // Check MIME type
            var allowedMimeTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
            if (!allowedMimeTypes.Contains(file.ContentType.ToLower()))
                return false;

            return true;
        }

        public async Task<string> ResizeImageAsync(string imagePath, int maxWidth = 800, int maxHeight = 600)
        {
            // For now, return the original path
            // In a production app, you'd implement image resizing using ImageSharp or similar
            await Task.CompletedTask;
            return imagePath;
        }
    }
}
