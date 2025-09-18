namespace BidNest.Services
{
    public interface IImageService
    {
        Task<string> UploadImageAsync(IFormFile file, string folder = "items");
        Task<List<string>> UploadImagesAsync(List<IFormFile> files, string folder = "items");
        Task<bool> DeleteImageAsync(string imagePath);
        Task<bool> DeleteImagesAsync(List<string> imagePaths);
        string GetImageUrl(string imagePath);
        bool IsValidImageFile(IFormFile file);
        Task<string> ResizeImageAsync(string imagePath, int maxWidth = 800, int maxHeight = 600);
    }
}
