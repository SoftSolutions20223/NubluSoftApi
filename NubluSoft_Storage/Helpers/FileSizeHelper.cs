using NubluSoft_Storage.Helpers;

namespace NubluSoft_Storage.Helpers
{
    /// <summary>
    /// Helper para formatear y calcular tamaños de archivo
    /// </summary>
    public static class FileSizeHelper
    {
        private static readonly string[] SizeUnits = { "B", "KB", "MB", "GB", "TB", "PB" };

        /// <summary>
        /// Formatea bytes a una representación legible (KB, MB, GB, etc.)
        /// </summary>
        public static string FormatSize(long bytes)
        {
            if (bytes < 0)
                return "0 B";

            var size = (double)bytes;
            var unitIndex = 0;

            while (size >= 1024 && unitIndex < SizeUnits.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }

            return $"{size:0.##} {SizeUnits[unitIndex]}";
        }

        /// <summary>
        /// Convierte MB a bytes
        /// </summary>
        public static long MBToBytes(int megabytes)
        {
            return megabytes * 1024L * 1024L;
        }

        /// <summary>
        /// Convierte GB a bytes
        /// </summary>
        public static long GBToBytes(int gigabytes)
        {
            return gigabytes * 1024L * 1024L * 1024L;
        }

        /// <summary>
        /// Convierte bytes a MB
        /// </summary>
        public static double BytesToMB(long bytes)
        {
            return bytes / (1024.0 * 1024.0);
        }

        /// <summary>
        /// Convierte bytes a GB
        /// </summary>
        public static double BytesToGB(long bytes)
        {
            return bytes / (1024.0 * 1024.0 * 1024.0);
        }

        /// <summary>
        /// Verifica si el tamaño excede el límite
        /// </summary>
        public static bool ExceedsLimit(long bytes, int limitMB)
        {
            return bytes > MBToBytes(limitMB);
        }

        /// <summary>
        /// Calcula tiempo estimado de transferencia
        /// </summary>
        public static TimeSpan EstimateTransferTime(long bytes, double bytesPerSecond)
        {
            if (bytesPerSecond <= 0)
                return TimeSpan.MaxValue;

            var seconds = bytes / bytesPerSecond;
            return TimeSpan.FromSeconds(seconds);
        }
    }
}