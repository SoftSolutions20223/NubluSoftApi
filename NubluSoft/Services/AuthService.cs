using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;
using NubluSoft.Configuration;
using NubluSoft.Models.DTOs;
using NubluSoft.Models.Entities;

namespace NubluSoft.Services
{
    /// <summary>
    /// Servicio de autenticación contra PostgreSQL
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly string _connectionString;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IConfiguration configuration, ILogger<AuthService> logger)
        {
            _connectionString = configuration.GetConnectionString("PostgreSQL")
                ?? throw new InvalidOperationException("ConnectionString 'PostgreSQL' no configurado");
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<(Usuario? Usuario, string? Error)> ValidarCredencialesAsync(string usuario, string contraseña)
        {
            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // Consulta para validar usuario y obtener datos de entidad
                const string sql = @"
                    SELECT 
                        u.""Cod"",
                        u.""Nombres"",
                        u.""Apellidos"",
                        u.""Telefono"",
                        u.""Documento"",
                        u.""Usuario"" AS Usuario_,
                        u.""Contraseña"",
                        u.""Estado"",
                        u.""Entidad"",
                        u.""SesionActiva"",
                        u.""Token"",
                        e.""Nombre"" AS NombreEntidad,
                        e.""FechaLimite""::timestamp AS ""FechaLimite""
                    FROM usuarios.""Usuarios"" u
                    INNER JOIN usuarios.""Entidades"" e ON u.""Entidad"" = e.""Cod""
                    WHERE u.""Usuario"" = @Usuario";

                var user = await connection.QueryFirstOrDefaultAsync<Usuario>(sql, new { Usuario = usuario });

                // Usuario no encontrado
                if (user == null)
                {
                    _logger.LogWarning("Intento de login con usuario inexistente: {Usuario}", usuario);
                    return (null, "Usuario incorrecto");
                }

                // Validar contraseña
                if (user.Contraseña != contraseña)
                {
                    _logger.LogWarning("Contraseña incorrecta para usuario: {Usuario}", usuario);
                    return (null, "Contraseña incorrecta");
                }

                // Validar estado del usuario
                if (!user.Estado)
                {
                    _logger.LogWarning("Usuario inactivo intentó iniciar sesión: {Usuario}", usuario);
                    return (null, "Este usuario se encuentra inactivo, comuníquese con su administrador");
                }

                // Validar fecha límite del plan
                if (user.FechaLimite.HasValue && user.FechaLimite.Value.Date < DateTime.Today)
                {
                    _logger.LogWarning("Plan vencido para usuario: {Usuario}, FechaLimite: {FechaLimite}",
                        usuario, user.FechaLimite);
                    return (null, "El plan al que se encuentra suscrito ha caducado");
                }

                _logger.LogInformation("Login exitoso para usuario: {Usuario}", usuario);
                return (user, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando credenciales para usuario: {Usuario}", usuario);
                return (null, "Error interno del servidor, intente nuevamente");
            }
        }

        /// <inheritdoc />
        public async Task<Entidad?> ObtenerEntidadAsync(long entidadId)
        {
            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql = @"
                    SELECT 
                        ""Cod"",
                        ""Nombre"",
                        ""Telefono"",
                        ""Nit"",
                        ""Direccion"",
                        ""Correo"",
                        ""FechaLimite""::timestamp AS ""FechaLimite"",
                        ""Bd"",
                        ""Url"",
                        ""Coleccion""
                    FROM usuarios.""Entidades""
                    WHERE ""Cod"" = @Cod";

                return await connection.QueryFirstOrDefaultAsync<Entidad>(sql, new { Cod = entidadId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo entidad: {EntidadId}", entidadId);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<List<RolUsuarioInfo>> ObtenerRolesUsuarioAsync(long usuarioId)
        {
            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql = @"
                    SELECT 
                        ru.""Oficina"",
                        ru.""Rol"",
                        o.""Nombre"" AS NombreOficina,
                        r.""Nombre"" AS NombreRol
                    FROM documentos.""Roles_Usuarios"" ru
                    LEFT JOIN documentos.""Oficinas"" o ON ru.""Oficina"" = o.""Cod""
                    INNER JOIN documentos.""Roles"" r ON ru.""Rol"" = r.""Cod""
                    WHERE ru.""Usuario"" = @UsuarioId 
                      AND ru.""Estado"" = true";

                var roles = await connection.QueryAsync<RolUsuarioInfo>(sql, new { UsuarioId = usuarioId });
                return roles.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo roles para usuario: {UsuarioId}", usuarioId);
                return new List<RolUsuarioInfo>();
            }
        }

        /// <inheritdoc />
        public async Task ActualizarTokenUsuarioAsync(long usuarioId, string token)
        {
            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql = @"
                    UPDATE usuarios.""Usuarios"" 
                    SET ""Token"" = @Token, 
                        ""SesionActiva"" = true 
                    WHERE ""Cod"" = @Cod";

                await connection.ExecuteAsync(sql, new { Cod = usuarioId, Token = token });
                _logger.LogDebug("Token actualizado para usuario: {UsuarioId}", usuarioId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando token para usuario: {UsuarioId}", usuarioId);
            }
        }

        /// <inheritdoc />
        public async Task LimpiarTokenUsuarioAsync(long usuarioId)
        {
            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql = @"
                    UPDATE usuarios.""Usuarios"" 
                    SET ""Token"" = NULL, 
                        ""SesionActiva"" = false 
                    WHERE ""Cod"" = @Cod";

                await connection.ExecuteAsync(sql, new { Cod = usuarioId });
                _logger.LogDebug("Token limpiado para usuario: {UsuarioId}", usuarioId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error limpiando token para usuario: {UsuarioId}", usuarioId);
            }
        }
    }
}