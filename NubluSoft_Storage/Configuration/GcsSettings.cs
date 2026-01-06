using Microsoft.Extensions.Configuration;
using NubluSoft_Storage.Configuration;

namespace NubluSoft_Storage.Configuration
{
    /// <summary>
    /// Configuración de Google Cloud Storage
    /// </summary>
    public class GcsSettings
    {
        public const string SectionName = "GoogleCloudStorage";

        /// <summary>
        /// Nombre del bucket en GCS
        /// </summary>
        public string BucketName { get; set; } = string.Empty;

        /// <summary>
        /// ID del proyecto en Google Cloud
        /// </summary>
        public string ProjectId { get; set; } = string.Empty;

        /// <summary>
        /// Ruta al archivo de credenciales JSON (opcional si usa ADC)
        /// </summary>
        public string? CredentialsPath { get; set; }

        /// <summary>
        /// Tiempo de expiración para URLs firmadas (minutos)
        /// </summary>
        public int SignedUrlExpiration { get; set; } = 15;

        /// <summary>
        /// Tamaño de chunk para uploads resumibles (MB)
        /// </summary>
        public int ChunkSizeMB { get; set; } = 5;

        /// <summary>
        /// Tamaño máximo de archivo permitido (MB)
        /// </summary>
        public int MaxFileSizeMB { get; set; } = 500;

        /// <summary>
        /// Usar emulador local (solo desarrollo)
        /// </summary>
        public bool UseEmulator { get; set; } = false;

        /// <summary>
        /// Tamaño de chunk en bytes (calculado)
        /// </summary>
        public int ChunkSizeBytes => ChunkSizeMB * 1024 * 1024;

        /// <summary>
        /// Tamaño máximo en bytes (calculado)
        /// </summary>
        public long MaxFileSizeBytes => MaxFileSizeMB * 1024L * 1024L;
    }
}