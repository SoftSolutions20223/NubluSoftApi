using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace NubluSoft_Signature.Helpers
{
    /// <summary>
    /// Helper para operaciones criptográficas
    /// </summary>
    public static class CryptoHelper
    {
        private const int SaltSize = 32;
        private const int KeySize = 32;
        private const int Iterations = 100000;

        /// <summary>
        /// Encripta una clave privada con AES-256
        /// </summary>
        /// <param name="privateKey">Clave privada en bytes</param>
        /// <param name="password">Contraseña del usuario</param>
        /// <returns>Tupla con (datos encriptados, salt)</returns>
        public static (byte[] Encrypted, byte[] Salt) EncryptPrivateKey(byte[] privateKey, string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var key = DeriveKey(password, salt);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            var encrypted = encryptor.TransformFinalBlock(privateKey, 0, privateKey.Length);

            // Combinar IV + datos encriptados
            var result = new byte[aes.IV.Length + encrypted.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(encrypted, 0, result, aes.IV.Length, encrypted.Length);

            return (result, salt);
        }

        /// <summary>
        /// Desencripta una clave privada
        /// </summary>
        /// <param name="encryptedWithIv">Datos encriptados (IV + ciphertext)</param>
        /// <param name="salt">Salt usado en la encriptación</param>
        /// <param name="password">Contraseña del usuario</param>
        /// <returns>Clave privada en bytes</returns>
        public static byte[] DecryptPrivateKey(byte[] encryptedWithIv, byte[] salt, string password)
        {
            var key = DeriveKey(password, salt);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            // Extraer IV (primeros 16 bytes)
            var iv = new byte[16];
            Buffer.BlockCopy(encryptedWithIv, 0, iv, 0, 16);
            aes.IV = iv;

            // Extraer datos encriptados
            var encrypted = new byte[encryptedWithIv.Length - 16];
            Buffer.BlockCopy(encryptedWithIv, 16, encrypted, 0, encrypted.Length);

            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
        }

        /// <summary>
        /// Intenta desencriptar para validar la contraseña
        /// </summary>
        public static bool ValidarContrasena(byte[] encryptedWithIv, byte[] salt, string password)
        {
            try
            {
                DecryptPrivateKey(encryptedWithIv, salt, password);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Deriva una clave a partir de la contraseña usando PBKDF2
        /// </summary>
        private static byte[] DeriveKey(string password, byte[] salt)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(
                password, salt, Iterations, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(KeySize);
        }

        /// <summary>
        /// Calcula el hash SHA256 de un certificado
        /// </summary>
        public static string CalcularHuella(byte[] certificateBytes)
        {
            var hashBytes = SHA256.HashData(certificateBytes);
            return BitConverter.ToString(hashBytes).Replace("-", ":");
        }

        /// <summary>
        /// Genera un número de serie único para el certificado
        /// </summary>
        public static string GenerarNumeroSerie()
        {
            return Guid.NewGuid().ToString().ToUpper();
        }

        /// <summary>
        /// Valida la complejidad de una contraseña
        /// </summary>
        /// <param name="password">Contraseña a validar</param>
        /// <returns>Lista de errores (vacía si es válida)</returns>
        public static List<string> ValidarComplejidadContrasena(string password)
        {
            var errores = new List<string>();

            if (string.IsNullOrEmpty(password))
            {
                errores.Add("La contraseña es requerida");
                return errores;
            }

            if (password.Length < 8)
                errores.Add("Debe tener al menos 8 caracteres");

            if (password.Length > 50)
                errores.Add("No puede exceder 50 caracteres");

            if (!Regex.IsMatch(password, @"[A-Z]"))
                errores.Add("Debe incluir al menos una mayúscula");

            if (!Regex.IsMatch(password, @"[a-z]"))
                errores.Add("Debe incluir al menos una minúscula");

            if (!Regex.IsMatch(password, @"[0-9]"))
                errores.Add("Debe incluir al menos un número");

            if (!Regex.IsMatch(password, @"[!@#$%^&*(),.?""':{}|<>]"))
                errores.Add("Debe incluir al menos un carácter especial (!@#$%^&*...)");

            return errores;
        }

        /// <summary>
        /// Convierte bytes a Base64
        /// </summary>
        public static string ToBase64(byte[] bytes) => Convert.ToBase64String(bytes);

        /// <summary>
        /// Convierte Base64 a bytes
        /// </summary>
        public static byte[] FromBase64(string base64) => Convert.FromBase64String(base64);
    }
}