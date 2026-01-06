namespace NubluSoft_Storage.Helpers
{
    /// <summary>
    /// Helper para determinar tipos MIME basado en extensión de archivo
    /// </summary>
    public static class MimeTypeHelper
    {
        private static readonly Dictionary<string, string> MimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            // Documentos
            { ".pdf", "application/pdf" },
            { ".doc", "application/msword" },
            { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
            { ".xls", "application/vnd.ms-excel" },
            { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
            { ".ppt", "application/vnd.ms-powerpoint" },
            { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
            { ".odt", "application/vnd.oasis.opendocument.text" },
            { ".ods", "application/vnd.oasis.opendocument.spreadsheet" },
            { ".odp", "application/vnd.oasis.opendocument.presentation" },
            { ".rtf", "application/rtf" },
            
            // Texto
            { ".txt", "text/plain" },
            { ".csv", "text/csv" },
            { ".xml", "application/xml" },
            { ".json", "application/json" },
            { ".html", "text/html" },
            { ".htm", "text/html" },
            { ".css", "text/css" },
            { ".js", "application/javascript" },
            
            // Imágenes
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".png", "image/png" },
            { ".gif", "image/gif" },
            { ".bmp", "image/bmp" },
            { ".webp", "image/webp" },
            { ".svg", "image/svg+xml" },
            { ".ico", "image/x-icon" },
            { ".tiff", "image/tiff" },
            { ".tif", "image/tiff" },
            
            // Audio
            { ".mp3", "audio/mpeg" },
            { ".wav", "audio/wav" },
            { ".ogg", "audio/ogg" },
            { ".m4a", "audio/mp4" },
            { ".flac", "audio/flac" },
            { ".aac", "audio/aac" },
            
            // Video
            { ".mp4", "video/mp4" },
            { ".avi", "video/x-msvideo" },
            { ".mov", "video/quicktime" },
            { ".wmv", "video/x-ms-wmv" },
            { ".flv", "video/x-flv" },
            { ".webm", "video/webm" },
            { ".mkv", "video/x-matroska" },
            { ".mpeg", "video/mpeg" },
            { ".mpg", "video/mpeg" },
            
            // Comprimidos
            { ".zip", "application/zip" },
            { ".rar", "application/vnd.rar" },
            { ".7z", "application/x-7z-compressed" },
            { ".tar", "application/x-tar" },
            { ".gz", "application/gzip" },
            { ".bz2", "application/x-bzip2" },
            
            // Otros
            { ".exe", "application/x-msdownload" },
            { ".dll", "application/x-msdownload" },
            { ".iso", "application/x-iso9660-image" },
            { ".eml", "message/rfc822" },
            { ".msg", "application/vnd.ms-outlook" }
        };

        /// <summary>
        /// Obtiene el tipo MIME basado en la extensión del archivo
        /// </summary>
        public static string GetMimeType(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "application/octet-stream";

            var extension = Path.GetExtension(fileName);

            if (string.IsNullOrEmpty(extension))
                return "application/octet-stream";

            return MimeTypes.TryGetValue(extension, out var mimeType)
                ? mimeType
                : "application/octet-stream";
        }

        /// <summary>
        /// Obtiene la extensión basada en el tipo MIME
        /// </summary>
        public static string GetExtension(string mimeType)
        {
            if (string.IsNullOrEmpty(mimeType))
                return ".bin";

            var entry = MimeTypes.FirstOrDefault(x =>
                x.Value.Equals(mimeType, StringComparison.OrdinalIgnoreCase));

            return !string.IsNullOrEmpty(entry.Key) ? entry.Key : ".bin";
        }

        /// <summary>
        /// Verifica si el tipo MIME corresponde a una imagen
        /// </summary>
        public static bool IsImage(string mimeType)
        {
            return !string.IsNullOrEmpty(mimeType) &&
                   mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifica si el tipo MIME corresponde a un documento visualizable
        /// </summary>
        public static bool IsViewableDocument(string mimeType)
        {
            if (string.IsNullOrEmpty(mimeType))
                return false;

            var viewableTypes = new[]
            {
                "application/pdf",
                "text/plain",
                "text/html",
                "text/csv",
                "image/jpeg",
                "image/png",
                "image/gif",
                "image/webp",
                "image/svg+xml"
            };

            return viewableTypes.Contains(mimeType.ToLowerInvariant());
        }

        /// <summary>
        /// Verifica si el tipo MIME corresponde a video
        /// </summary>
        public static bool IsVideo(string mimeType)
        {
            return !string.IsNullOrEmpty(mimeType) &&
                   mimeType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifica si el tipo MIME corresponde a audio
        /// </summary>
        public static bool IsAudio(string mimeType)
        {
            return !string.IsNullOrEmpty(mimeType) &&
                   mimeType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Obtiene categoría del archivo basado en MIME type
        /// </summary>
        public static string GetCategory(string mimeType)
        {
            if (string.IsNullOrEmpty(mimeType))
                return "Otro";

            if (IsImage(mimeType)) return "Imagen";
            if (IsVideo(mimeType)) return "Video";
            if (IsAudio(mimeType)) return "Audio";
            if (mimeType.Contains("pdf")) return "PDF";
            if (mimeType.Contains("word") || mimeType.Contains("document")) return "Documento";
            if (mimeType.Contains("excel") || mimeType.Contains("spreadsheet")) return "Hoja de cálculo";
            if (mimeType.Contains("powerpoint") || mimeType.Contains("presentation")) return "Presentación";
            if (mimeType.Contains("zip") || mimeType.Contains("rar") || mimeType.Contains("compressed")) return "Comprimido";
            if (mimeType.StartsWith("text/")) return "Texto";

            return "Otro";
        }
    }
}