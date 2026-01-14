using Dapper;
using Microsoft.Extensions.Options;
using NubluSoft_Signature.Configuration;
using NubluSoft_Signature.Helpers;
using NubluSoft_Signature.Models.DTOs;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using System.Security.Cryptography.X509Certificates;

namespace NubluSoft_Signature.Services
{
    /// <summary>
    /// Implementación del servicio de certificados usando BouncyCastle
    /// </summary>
    public class CertificadoService : ICertificadoService
    {
        private readonly IPostgresConnectionFactory _connectionFactory;
        private readonly SignatureSettings _settings;
        private readonly ILogger<CertificadoService> _logger;

        public CertificadoService(
            IPostgresConnectionFactory connectionFactory,
            IOptions<SignatureSettings> settings,
            ILogger<CertificadoService> logger)
        {
            _connectionFactory = connectionFactory;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<CertificadoResponse?> ObtenerCertificadoActivoAsync(long entidadId, long usuarioId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var certificado = await connection.QueryFirstOrDefaultAsync<CertificadoDto>(@"
                    SELECT 
                        ""Cod"",
                        ""NumeroSerie"",
                        ""SubjectName"",
                        ""Huella"",
                        ""AlgoritmoFirma"",
                        ""TamanoClave"",
                        ""FechaEmision"",
                        ""FechaVencimiento"",
                        ""Estado"",
                        ""UltimoUso"",
                        ""VecesUsado""
                    FROM documentos.""Certificados_Usuarios""
                    WHERE ""Usuario"" = @UsuarioId 
                      AND ""Entidad"" = @EntidadId
                      AND ""Estado"" = 'ACTIVO'",
                    new { UsuarioId = usuarioId, EntidadId = entidadId });

                if (certificado == null)
                    return null;

                return new CertificadoResponse
                {
                    Cod = certificado.Cod,
                    NumeroSerie = certificado.NumeroSerie,
                    SubjectName = certificado.SubjectName,
                    Huella = certificado.Huella,
                    AlgoritmoFirma = certificado.AlgoritmoFirma ?? "RSA",
                    TamanoClave = certificado.TamanoClave,
                    FechaEmision = certificado.FechaEmision,
                    FechaVencimiento = certificado.FechaVencimiento,
                    Estado = certificado.Estado ?? "ACTIVO",
                    UltimoUso = certificado.UltimoUso,
                    VecesUsado = certificado.VecesUsado,
                    DiasParaVencer = (certificado.FechaVencimiento - DateTime.Now).Days,
                    EstaVigente = certificado.FechaVencimiento > DateTime.Now
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo certificado activo");
                return null;
            }
        }

        public async Task<GenerarCertificadoResponse> GenerarCertificadoAsync(
            long entidadId,
            long usuarioId,
            string contrasena)
        {
            try
            {
                // 1. Validar complejidad de contraseña
                var errores = CryptoHelper.ValidarComplejidadContrasena(contrasena);
                if (errores.Count > 0)
                {
                    return new GenerarCertificadoResponse
                    {
                        Exito = false,
                        Mensaje = string.Join(". ", errores)
                    };
                }

                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // 2. Verificar que no exista certificado activo
                var existeActivo = await connection.QueryFirstOrDefaultAsync<bool>(@"
                    SELECT EXISTS(
                        SELECT 1 FROM documentos.""Certificados_Usuarios""
                        WHERE ""Usuario"" = @UsuarioId AND ""Entidad"" = @EntidadId
                        AND ""Estado"" = 'ACTIVO'
                    )",
                    new { UsuarioId = usuarioId, EntidadId = entidadId });

                if (existeActivo)
                {
                    return new GenerarCertificadoResponse
                    {
                        Exito = false,
                        Mensaje = "Ya existe un certificado activo. Revóquelo primero para generar uno nuevo."
                    };
                }

                // 3. Obtener información del usuario
                var usuario = await connection.QueryFirstOrDefaultAsync<UsuarioCertificadoDto>(@"
                    SELECT 
                        u.""Nombres"" || ' ' || u.""Apellidos"" AS NombreCompleto,
                        u.""Documento"",
                        u.""Correo"",
                        e.""Nombre"" AS NombreEntidad
                    FROM documentos.""Usuarios"" u
                    INNER JOIN usuarios.""Entidades"" e ON u.""Entidad"" = e.""Cod""
                    WHERE u.""Cod"" = @UsuarioId AND u.""Entidad"" = @EntidadId",
                    new { UsuarioId = usuarioId, EntidadId = entidadId });

                if (usuario == null)
                {
                    return new GenerarCertificadoResponse
                    {
                        Exito = false,
                        Mensaje = "Usuario no encontrado"
                    };
                }

                // 4. Generar par de claves RSA con BouncyCastle
                var keyPairGenerator = new RsaKeyPairGenerator();
                keyPairGenerator.Init(new KeyGenerationParameters(
                    new SecureRandom(),
                    _settings.Certificado.TamanoClave));
                var keyPair = keyPairGenerator.GenerateKeyPair();

                // 5. Crear certificado X.509
                var numeroSerie = CryptoHelper.GenerarNumeroSerie();
                var fechaEmision = DateTime.UtcNow;
                var fechaVencimiento = fechaEmision.AddDays(_settings.Certificado.VigenciaDias);

                var subjectName = $"CN={usuario.NombreCompleto}, O={usuario.NombreEntidad}, C=CO";
                var issuerName = _settings.Certificado.IssuerName;

                var certificateGenerator = new X509V3CertificateGenerator();
                certificateGenerator.SetSerialNumber(new BigInteger(Guid.NewGuid().ToByteArray()).Abs());
                certificateGenerator.SetSubjectDN(new X509Name(subjectName));
                certificateGenerator.SetIssuerDN(new X509Name(issuerName));
                certificateGenerator.SetNotBefore(fechaEmision);
                certificateGenerator.SetNotAfter(fechaVencimiento);
                certificateGenerator.SetPublicKey(keyPair.Public);

                // Agregar extensiones
                certificateGenerator.AddExtension(
                    X509Extensions.KeyUsage,
                    true,
                    new KeyUsage(KeyUsage.DigitalSignature | KeyUsage.NonRepudiation));

                certificateGenerator.AddExtension(
                    X509Extensions.ExtendedKeyUsage,
                    false,
                    new ExtendedKeyUsage(KeyPurposeID.IdKPEmailProtection));

                // Firmar el certificado
                var signatureFactory = new Asn1SignatureFactory("SHA256WithRSA", keyPair.Private);
                var certificate = certificateGenerator.Generate(signatureFactory);

                // 6. Exportar certificado público
                var certBytes = certificate.GetEncoded();
                var certBase64 = Convert.ToBase64String(certBytes);
                var huella = CryptoHelper.CalcularHuella(certBytes);

                // 7. Exportar y encriptar clave privada
                var privateKeyInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(keyPair.Private);
                var privateKeyBytes = privateKeyInfo.GetEncoded();

                var (encryptedKey, salt) = CryptoHelper.EncryptPrivateKey(privateKeyBytes, contrasena);
                var encryptedKeyBase64 = Convert.ToBase64String(encryptedKey);
                var saltBase64 = Convert.ToBase64String(salt);

                // 8. Guardar en BD
                await connection.ExecuteAsync(@"
                    INSERT INTO documentos.""Certificados_Usuarios"" (
                        ""Usuario"", ""Entidad"", ""NumeroSerie"", ""SubjectName"", ""Huella"",
                        ""CertificadoPublico"", ""ClavePrivadaEncriptada"", ""SaltEncriptacion"",
                        ""AlgoritmoEncriptacion"", ""AlgoritmoFirma"", ""TamanoClave"",
                        ""FechaEmision"", ""FechaVencimiento"", ""Estado""
                    ) VALUES (
                        @UsuarioId, @EntidadId, @NumeroSerie, @SubjectName, @Huella,
                        @CertificadoPublico, @ClavePrivadaEncriptada, @SaltEncriptacion,
                        'AES256', 'RSA', @TamanoClave,
                        @FechaEmision, @FechaVencimiento, 'ACTIVO'
                    )",
                    new
                    {
                        UsuarioId = usuarioId,
                        EntidadId = entidadId,
                        NumeroSerie = numeroSerie,
                        SubjectName = subjectName,
                        Huella = huella,
                        CertificadoPublico = certBase64,
                        ClavePrivadaEncriptada = encryptedKeyBase64,
                        SaltEncriptacion = saltBase64,
                        TamanoClave = _settings.Certificado.TamanoClave,
                        FechaEmision = fechaEmision,
                        FechaVencimiento = fechaVencimiento
                    });

                _logger.LogInformation(
                    "Certificado generado para usuario {UsuarioId}, serie: {NumeroSerie}",
                    usuarioId, numeroSerie);

                return new GenerarCertificadoResponse
                {
                    Exito = true,
                    Mensaje = "Certificado generado exitosamente",
                    NumeroSerie = numeroSerie,
                    Huella = huella,
                    VigenciaHasta = fechaVencimiento
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando certificado para usuario {UsuarioId}", usuarioId);
                return new GenerarCertificadoResponse
                {
                    Exito = false,
                    Mensaje = "Error al generar el certificado"
                };
            }
        }

        public async Task<RevocarCertificadoResponse> RevocarCertificadoAsync(
            long entidadId,
            long usuarioId,
            string motivo,
            string contrasena)
        {
            try
            {
                // 1. Validar contraseña primero
                var contrasenaValida = await ValidarContrasenaAsync(entidadId, usuarioId, contrasena);
                if (!contrasenaValida)
                {
                    return new RevocarCertificadoResponse
                    {
                        Exito = false,
                        Mensaje = "Contraseña incorrecta"
                    };
                }

                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var rowsAffected = await connection.ExecuteAsync(@"
                    UPDATE documentos.""Certificados_Usuarios""
                    SET ""Estado"" = 'REVOCADO',
                        ""FechaRevocacion"" = CURRENT_TIMESTAMP,
                        ""MotivoRevocacion"" = @Motivo,
                        ""RevocadoPor"" = @UsuarioId
                    WHERE ""Usuario"" = @UsuarioId 
                      AND ""Entidad"" = @EntidadId
                      AND ""Estado"" = 'ACTIVO'",
                    new { UsuarioId = usuarioId, EntidadId = entidadId, Motivo = motivo });

                if (rowsAffected == 0)
                {
                    return new RevocarCertificadoResponse
                    {
                        Exito = false,
                        Mensaje = "No se encontró certificado activo para revocar"
                    };
                }

                _logger.LogInformation("Certificado revocado para usuario {UsuarioId}", usuarioId);

                return new RevocarCertificadoResponse
                {
                    Exito = true,
                    Mensaje = "Certificado revocado exitosamente"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revocando certificado del usuario {UsuarioId}", usuarioId);
                return new RevocarCertificadoResponse
                {
                    Exito = false,
                    Mensaje = "Error al revocar el certificado"
                };
            }
        }

        public async Task<X509Certificate2?> ObtenerCertificadoParaFirmarAsync(
            long entidadId,
            long usuarioId,
            string contrasena)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var datos = await connection.QueryFirstOrDefaultAsync<CertificadoDatosDto>(@"
                    SELECT 
                        ""Cod"",
                        ""CertificadoPublico"",
                        ""ClavePrivadaEncriptada"",
                        ""SaltEncriptacion"",
                        ""FechaVencimiento""
                    FROM documentos.""Certificados_Usuarios""
                    WHERE ""Usuario"" = @UsuarioId 
                      AND ""Entidad"" = @EntidadId
                      AND ""Estado"" = 'ACTIVO'",
                    new { UsuarioId = usuarioId, EntidadId = entidadId });

                if (datos == null)
                    return null;

                // Verificar vigencia
                if (datos.FechaVencimiento < DateTime.Now)
                {
                    _logger.LogWarning("Certificado vencido para usuario {UsuarioId}", usuarioId);
                    return null;
                }

                // Desencriptar clave privada
                var encryptedKey = Convert.FromBase64String(datos.ClavePrivadaEncriptada);
                var salt = Convert.FromBase64String(datos.SaltEncriptacion);

                byte[] privateKeyBytes;
                try
                {
                    privateKeyBytes = CryptoHelper.DecryptPrivateKey(encryptedKey, salt, contrasena);
                }
                catch
                {
                    _logger.LogWarning("Contraseña incorrecta para certificado del usuario {UsuarioId}", usuarioId);
                    return null;
                }

                // Reconstruir certificado con clave privada
                var certBytes = Convert.FromBase64String(datos.CertificadoPublico);

                // Usar BouncyCastle para cargar la clave privada
                var privateKeyInfo = PrivateKeyInfo.GetInstance(privateKeyBytes);
                var privateKey = PrivateKeyFactory.CreateKey(privateKeyInfo);

                // Cargar certificado
                var parser = new X509CertificateParser();
                var bcCert = parser.ReadCertificate(certBytes);

                // Crear PKCS12 en memoria
                var store = new Pkcs12StoreBuilder().Build();
                var certEntry = new X509CertificateEntry(bcCert);
                store.SetCertificateEntry("cert", certEntry);
                store.SetKeyEntry("cert", new AsymmetricKeyEntry(privateKey), new[] { certEntry });

                using var pfxStream = new MemoryStream();
                store.Save(pfxStream, contrasena.ToCharArray(), new SecureRandom());

                // Crear X509Certificate2 desde PKCS12
                var certificate = new X509Certificate2(
                    pfxStream.ToArray(),
                    contrasena,
                    X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet);

                return certificate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo certificado para firmar del usuario {UsuarioId}", usuarioId);
                return null;
            }
        }

        public async Task<bool> ValidarContrasenaAsync(long entidadId, long usuarioId, string contrasena)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var datos = await connection.QueryFirstOrDefaultAsync<CertificadoDatosDto>(@"
                    SELECT ""ClavePrivadaEncriptada"", ""SaltEncriptacion""
                    FROM documentos.""Certificados_Usuarios""
                    WHERE ""Usuario"" = @UsuarioId 
                      AND ""Entidad"" = @EntidadId
                      AND ""Estado"" = 'ACTIVO'",
                    new { UsuarioId = usuarioId, EntidadId = entidadId });

                if (datos == null)
                    return false;

                var encryptedKey = Convert.FromBase64String(datos.ClavePrivadaEncriptada);
                var salt = Convert.FromBase64String(datos.SaltEncriptacion);

                return CryptoHelper.ValidarContrasena(encryptedKey, salt, contrasena);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando contraseña del certificado");
                return false;
            }
        }

        public async Task RegistrarUsoAsync(long certificadoId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                await connection.ExecuteAsync(@"
                    UPDATE documentos.""Certificados_Usuarios""
                    SET ""UltimoUso"" = CURRENT_TIMESTAMP,
                        ""VecesUsado"" = ""VecesUsado"" + 1
                    WHERE ""Cod"" = @CertificadoId",
                    new { CertificadoId = certificadoId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registrando uso del certificado {CertificadoId}", certificadoId);
            }
        }

        public async Task<byte[]?> DescargarCertificadoPublicoAsync(long entidadId, long usuarioId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var certBase64 = await connection.QueryFirstOrDefaultAsync<string>(@"
                    SELECT ""CertificadoPublico""
                    FROM documentos.""Certificados_Usuarios""
                    WHERE ""Usuario"" = @UsuarioId 
                      AND ""Entidad"" = @EntidadId
                      AND ""Estado"" = 'ACTIVO'",
                    new { UsuarioId = usuarioId, EntidadId = entidadId });

                if (string.IsNullOrEmpty(certBase64))
                    return null;

                return Convert.FromBase64String(certBase64);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error descargando certificado público");
                return null;
            }
        }

        // ==================== DTOs internos ====================

        private class CertificadoDto
        {
            public long Cod { get; set; }
            public string NumeroSerie { get; set; } = string.Empty;
            public string SubjectName { get; set; } = string.Empty;
            public string Huella { get; set; } = string.Empty;
            public string? AlgoritmoFirma { get; set; }
            public int TamanoClave { get; set; }
            public DateTime FechaEmision { get; set; }
            public DateTime FechaVencimiento { get; set; }
            public string? Estado { get; set; }
            public DateTime? UltimoUso { get; set; }
            public int VecesUsado { get; set; }
        }

        private class CertificadoDatosDto
        {
            public long Cod { get; set; }
            public string CertificadoPublico { get; set; } = string.Empty;
            public string ClavePrivadaEncriptada { get; set; } = string.Empty;
            public string SaltEncriptacion { get; set; } = string.Empty;
            public DateTime FechaVencimiento { get; set; }
        }

        private class UsuarioCertificadoDto
        {
            public string NombreCompleto { get; set; } = string.Empty;
            public string? Documento { get; set; }
            public string? Correo { get; set; }
            public string NombreEntidad { get; set; } = string.Empty;
        }
    }
}