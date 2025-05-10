using System.Globalization;

namespace MustafaviManagementApp.Services
{

    public interface IFileStorageService
    {
        /// <summary>
        /// Saves an IFormFile under wwwroot/uploads/[subfolder]/yyyy/MM/dd/filename.ext
        /// Returns the web-relative path (e.g. "uploads/medicines/2025/05/med123.png").
        /// </summary>
        Task<string> SaveAsync(IFormFile file, string subfolder);

        /// <summary>
        /// Deletes the file given its relative web path (e.g. "uploads/medicines/…").
        /// </summary>
        void Delete(string? relativePath);
    }

    public class FileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _env;

        public FileStorageService(IWebHostEnvironment env) => _env = env;

        public async Task<string> SaveAsync(IFormFile file, string subfolder)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty.", nameof(file));

            // uploads/medicines/2025/05  (year/month folder keeps directory sizes reasonable)
            var dateSegment = DateTime.UtcNow.ToString("yyyy/MM", CultureInfo.InvariantCulture);
            string uploadsRoot = Path.Combine(_env.WebRootPath, "uploads", subfolder, dateSegment);
            Directory.CreateDirectory(uploadsRoot);

            string uniqueName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            string savePath = Path.Combine(uploadsRoot, uniqueName);

            await using var stream = File.Create(savePath);
            await file.CopyToAsync(stream);

            // Return relative path in URL-friendly format
            string relative = Path.Combine("uploads", subfolder, dateSegment, uniqueName)
                              .Replace("\\", "/");
            return relative;
        }

        public void Delete(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return;

            string fullPath = Path.Combine(_env.WebRootPath,
                                           relativePath.TrimStart('/', '\\'));
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
    }
}
