using Npgsql;

namespace NubluSoft_Storage.Services
{
    /// <summary>
    /// Factory para crear conexiones a PostgreSQL
    /// </summary>
    public interface IPostgresConnectionFactory
    {
        /// <summary>
        /// Crea una nueva conexión a PostgreSQL
        /// </summary>
        NpgsqlConnection CreateConnection();

        /// <summary>
        /// Obtiene el connection string (para logging/diagnóstico)
        /// </summary>
        string GetConnectionInfo();
    }
}