using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Habitera.DTOs;


namespace Habitera.Services
{
    public interface ICloudinaryService
    {
        Task<OperationResult<ImageUploadResultDTO>> UploadImageAsync(IFormFile file, string folder = "properties");
        Task<OperationResult<ImageUploadResultDTO>> UploadImageWithThumbnailAsync(IFormFile file, string folder = "properties");
        Task<OperationResult<string>> DeleteImageAsync(string publicId);
        Task<OperationResult<List<ImageUploadResultDTO>>> UploadMultipleImagesAsync(IFormFileCollection files, string folder = "properties");
    }

    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<CloudinaryService> _logger;

        public CloudinaryService(IConfiguration configuration, ILogger<CloudinaryService> logger)
        {
            var cloudName = configuration["Cloudinary:CloudName"];
            var apiKey = configuration["Cloudinary:ApiKey"];
            var apiSecret = configuration["Cloudinary:ApiSecret"];

            if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            {
                throw new InvalidOperationException("Cloudinary credentials are not configured");
            }

            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Secure = true;
            _logger = logger;
        }

        public async Task<OperationResult<ImageUploadResultDTO>> UploadImageAsync(
            IFormFile file,
            string folder = "properties")
        {
            try
            {
                // Validate file
                var validation = ValidateImageFile(file);
                if (!validation.Success)
                {
                    return OperationResult<ImageUploadResultDTO>.Failure(validation.Message, 400);
                }

                using var stream = file.OpenReadStream();

                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = folder,
                    Transformation = new Transformation()
                        .Quality("auto:good")
                        .FetchFormat("auto"),
                    UniqueFilename = true,
                    Overwrite = false
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.Error != null)
                {
                    _logger.LogError("Cloudinary upload error: {Error}", uploadResult.Error.Message);
                    return OperationResult<ImageUploadResultDTO>.Failure(
                        "Failed to upload image", 500);
                }

                var result = new ImageUploadResultDTO
                {
                    PublicId = uploadResult.PublicId,
                    Url = uploadResult.SecureUrl.ToString(),
                    Width = uploadResult.Width,
                    Height = uploadResult.Height,
                    Format = uploadResult.Format,
                    ResourceType = uploadResult.ResourceType
                };

                return OperationResult<ImageUploadResultDTO>.Successful(
                    result,
                    "Image uploaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image to Cloudinary");
                return OperationResult<ImageUploadResultDTO>.Failure(
                    "An error occurred while uploading the image", 500);
            }
        }

        public async Task<OperationResult<ImageUploadResultDTO>> UploadImageWithThumbnailAsync(
            IFormFile file,
            string folder = "properties")
        {
            try
            {
                var validation = ValidateImageFile(file);
                if (!validation.Success)
                {
                    return OperationResult<ImageUploadResultDTO>.Failure(validation.Message, 400);
                }

                using var stream = file.OpenReadStream();

                // Upload main image with transformations
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = folder,
                    Transformation = new Transformation()
                        .Width(1920)
                        .Height(1080)
                        .Crop("limit")
                        .Quality("auto:good")
                        .FetchFormat("auto"),
                    EagerTransforms = new List<Transformation>
                    {
                        // Create thumbnail transformation
                        new Transformation()
                            .Width(400)
                            .Height(300)
                            .Crop("fill")
                            .Gravity("auto")
                            .Quality("auto:good")
                            .FetchFormat("auto")
                    },
                    UniqueFilename = true,
                    Overwrite = false
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.Error != null)
                {
                    _logger.LogError("Cloudinary upload error: {Error}", uploadResult.Error.Message);
                    return OperationResult<ImageUploadResultDTO>.Failure(
                        "Failed to upload image", 500);
                }

                // Get thumbnail URL
                string thumbnailUrl = uploadResult.SecureUrl.ToString();
                if (uploadResult.Eager.Count() > 0)
                {
                    thumbnailUrl = uploadResult.Eager[0].SecureUrl.ToString();
                }

                var result = new ImageUploadResultDTO
                {
                    PublicId = uploadResult.PublicId,
                    Url = uploadResult.SecureUrl.ToString(),
                    ThumbnailUrl = thumbnailUrl,
                    Width = uploadResult.Width,
                    Height = uploadResult.Height,
                    Format = uploadResult.Format,
                    ResourceType = uploadResult.ResourceType
                };

                return OperationResult<ImageUploadResultDTO>.Successful(
                    result,
                    "Image uploaded successfully with thumbnail");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image with thumbnail");
                return OperationResult<ImageUploadResultDTO>.Failure(
                    "An error occurred while uploading the image", 500);
            }
        }

        public async Task<OperationResult<string>> DeleteImageAsync(string publicId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(publicId))
                {
                    return OperationResult<string>.Failure("Public ID is required", 400);
                }

                var deleteParams = new DeletionParams(publicId)
                {
                    ResourceType = ResourceType.Image
                };

                var result = await _cloudinary.DestroyAsync(deleteParams);

                if (result.Result == "ok")
                {
                    return OperationResult<string>.Successful(
                        publicId,
                        "Image deleted successfully");
                }

                _logger.LogWarning("Cloudinary delete failed: {Result}", result.Result);
                return OperationResult<string>.Failure(
                    $"Failed to delete image: {result.Result}", 500);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image from Cloudinary");
                return OperationResult<string>.Failure(
                    "An error occurred while deleting the image", 500);
            }
        }

        public async Task<OperationResult<List<ImageUploadResultDTO>>> UploadMultipleImagesAsync(
            IFormFileCollection files,
            string folder = "properties")
        {
            try
            {
                if (files == null || files.Count == 0)
                {
                    return OperationResult<List<ImageUploadResultDTO>>.Failure(
                        "No files provided", 400);
                }

                if (files.Count > 10)
                {
                    return OperationResult<List<ImageUploadResultDTO>>.Failure(
                        "Maximum 10 images allowed per upload", 400);
                }

                var results = new List<ImageUploadResultDTO>();
                var errors = new List<string>();

                foreach (var file in files)
                {
                    var uploadResult = await UploadImageWithThumbnailAsync(file, folder);

                    if (uploadResult.Success && uploadResult.Data != null)
                    {
                        results.Add(uploadResult.Data);
                    }
                    else
                    {
                        errors.Add($"{file.FileName}: {uploadResult.Message}");
                    }
                }

                if (results.Count == 0)
                {
                    return OperationResult<List<ImageUploadResultDTO>>.Failure(
                        "Failed to upload any images", 500, errors);
                }

                var message = results.Count == files.Count
                    ? "All images uploaded successfully"
                    : $"{results.Count} of {files.Count} images uploaded successfully";

                return OperationResult<List<ImageUploadResultDTO>>.Successful(
                    results, message, 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading multiple images");
                return OperationResult<List<ImageUploadResultDTO>>.Failure(
                    "An error occurred while uploading images", 500);
            }
        }

        private OperationResult<string> ValidateImageFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return OperationResult<string>.Failure("File is empty", 400);
            }

            // Check file size (max 10MB)
            const long maxFileSize = 10 * 1024 * 1024;
            if (file.Length > maxFileSize)
            {
                return OperationResult<string>.Failure(
                    "File size must not exceed 10MB", 400);
            }

            // Check file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var allowedContentTypes = new[]
            {
                "image/jpeg",
                "image/jpg",
                "image/png",
                "image/webp",
                "image/gif"
            };

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var contentType = file.ContentType.ToLowerInvariant();

            if (!allowedExtensions.Contains(extension) || !allowedContentTypes.Contains(contentType))
            {
                return OperationResult<string>.Failure(
                    "Invalid file type. Only JPG, PNG, WEBP, and GIF are allowed", 400);
            }

            return OperationResult<string>.Successful("Valid");
        }
    }

}



