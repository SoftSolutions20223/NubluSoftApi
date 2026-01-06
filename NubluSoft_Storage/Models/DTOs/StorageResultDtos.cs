using System.Reflection;

namespace NubluSoft_Storage.Models.DTOs
{
    /// <summary>
    /// Resultado genérico de operación de storage
    /// </summary>
    public class StorageResult
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;

        public static StorageResult Success(string mensaje = "Operación exitosa")
            => new() { Exito = true, Mensaje = mensaje };

        public static StorageResult Failure(string mensaje)
            => new() { Exito = false, Mensaje = mensaje };
    }

    /// <summary>
    /// Resultado genérico con datos
    /// </summary>
    public class StorageResult<T> : StorageResult
    {
        public T? Data { get; set; }

        public static StorageResult<T> Success(T data, string mensaje = "Operación exitosa")
            => new() { Exito = true, Mensaje = mensaje, Data = data };

        public new static StorageResult<T> Failure(string mensaje)
            => new() { Exito = false, Mensaje = mensaje };
    }

    /// <summary>
    /// Información de archivo almacenado en GCS
    /// </summary>
    public class GcsFileInfo
    {
        public string ObjectName { get; set; } = string.Empty;
        public string Bucket { get; set; } = string.Empty;
        public string MediaLink { get; set; } = string.Empty;
        public string? SelfLink { get; set; }
        public long Size { get; set; }
        public string? ContentType { get; set; }
        public string? Hash { get; set; }
        public string? Md5Hash { get; set; }
        public string? Crc32c { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? Updated { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
    }

    /// <summary>
    /// Estadísticas de storage de una entidad
    /// </summary>
    public class StorageStats
    {
        public long EntidadId { get; set; }
        public long TotalArchivos { get; set; }
        public long TotalBytes { get; set; }
        public string TotalFormateado { get; set; } = string.Empty;
        public long ArchivosPorTipo { get; set; }
        public Dictionary<string, long> BytesPorTipo { get; set; } = new();
    }

    /// <summary>
    /// Progreso de operación (para uploads/downloads grandes)
    /// </summary>
    public class ProgressInfo
    {
        public string OperationId { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public long BytesProcesados { get; set; }
        public long BytesTotales { get; set; }
        public int PorcentajeCompletado { get; set; }
        public double VelocidadBytesSegundo { get; set; }
        public TimeSpan? TiempoRestante { get; set; }
    }
}