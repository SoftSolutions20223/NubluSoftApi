using System.Security.Cryptography;
using System.Text;

namespace NubluSoft_Signature.Helpers
{
    /// <summary>
    /// Helper para generación y validación de códigos OTP
    /// </summary>
    public static class OtpHelper
    {
        /// <summary>
        /// Genera un código OTP numérico aleatorio
        /// </summary>
        /// <param name="longitud">Longitud del código (default: 6)</param>
        /// <returns>Código OTP como string</returns>
        public static string GenerarCodigo(int longitud = 6)
        {
            var random = RandomNumberGenerator.GetBytes(longitud);
            var codigo = new StringBuilder();

            for (int i = 0; i < longitud; i++)
            {
                codigo.Append(random[i] % 10);
            }

            return codigo.ToString();
        }

        /// <summary>
        /// Genera el hash SHA256 de un código con salt
        /// </summary>
        /// <param name="codigo">Código en texto plano</param>
        /// <returns>Tupla con (hash, salt)</returns>
        public static (string Hash, string Salt) HashearCodigo(string codigo)
        {
            var saltBytes = RandomNumberGenerator.GetBytes(16);
            var salt = Convert.ToBase64String(saltBytes);
            var hash = ComputeHash(codigo, salt);

            return (hash, salt);
        }

        /// <summary>
        /// Valida un código contra su hash almacenado
        /// </summary>
        /// <param name="codigoIngresado">Código ingresado por el usuario</param>
        /// <param name="hashAlmacenado">Hash almacenado en BD</param>
        /// <param name="salt">Salt usado para generar el hash</param>
        /// <returns>True si el código es válido</returns>
        public static bool ValidarCodigo(string codigoIngresado, string hashAlmacenado, string salt)
        {
            var hashCalculado = ComputeHash(codigoIngresado, salt);
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(hashCalculado),
                Encoding.UTF8.GetBytes(hashAlmacenado));
        }

        /// <summary>
        /// Calcula el hash SHA256
        /// </summary>
        private static string ComputeHash(string codigo, string salt)
        {
            var bytes = Encoding.UTF8.GetBytes(codigo + salt);
            var hashBytes = SHA256.HashData(bytes);
            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// Enmascara un email para mostrar al usuario
        /// Ejemplo: "usuario@dominio.com" -> "u***@***.com"
        /// </summary>
        public static string EnmascararEmail(string email)
        {
            if (string.IsNullOrEmpty(email) || !email.Contains('@'))
                return "***@***.***";

            var partes = email.Split('@');
            var usuario = partes[0];
            var dominio = partes[1];

            var usuarioMask = usuario.Length > 1
                ? usuario[0] + new string('*', Math.Min(3, usuario.Length - 1))
                : "*";

            var dominioPartes = dominio.Split('.');
            var dominioMask = dominioPartes.Length > 1
                ? "***." + dominioPartes[^1]
                : "***";

            return $"{usuarioMask}@{dominioMask}";
        }

        /// <summary>
        /// Enmascara un número de teléfono
        /// Ejemplo: "3001234567" -> "300***4567"
        /// </summary>
        public static string EnmascararTelefono(string telefono)
        {
            if (string.IsNullOrEmpty(telefono) || telefono.Length < 6)
                return "***";

            return telefono[..3] + "***" + telefono[^4..];
        }
    }
}