namespace Habitera.DTOs
{

    public class ImageUploadResultDTO
    {
        public string PublicId { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Format { get; set; } = string.Empty;
        public string ResourceType { get; set; } = string.Empty;
    }
}
