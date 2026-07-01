using System;
using System.Security.Cryptography;
using System.Text;

namespace WebApplication1.Utils
{
    public static class PasswordHasher
    {
        public static bool VerifyDjangoPassword(string password, string djangoHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(djangoHash))
                return false;

            var parts = djangoHash.Split('$');
            if (parts.Length != 4)
            {
                return false; // Not a standard Django pbkdf2_sha256 hash
            }

            var algorithm = parts[0];
            var iterationsStr = parts[1];
            var salt = parts[2];
            var hashBase64 = parts[3];

            if (algorithm != "pbkdf2_sha256")
            {
                return false;
            }

            if (!int.TryParse(iterationsStr, out var iterations))
            {
                return false;
            }

            var saltBytes = Encoding.UTF8.GetBytes(salt);
            var hashBytes = Convert.FromBase64String(hashBase64);

            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var generatedHash = Rfc2898DeriveBytes.Pbkdf2(passwordBytes, saltBytes, iterations, HashAlgorithmName.SHA256, hashBytes.Length);
            
            // Removed debug logging of hashes in production

            // Constant-time comparison
            return CryptographicOperations.FixedTimeEquals(generatedHash, hashBytes);
        }

        public static string HashDjangoPassword(string password, int iterations = 390000)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be empty.");

            var saltBytes = new byte[12]; // 12 characters base64 usually
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            var salt = Convert.ToBase64String(saltBytes).Substring(0, 12).Replace('+', '-').Replace('/', '_'); // A valid Django base64 alphabet subset

            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var saltBytesActual = Encoding.UTF8.GetBytes(salt);
            var hashBytes = Rfc2898DeriveBytes.Pbkdf2(passwordBytes, saltBytesActual, iterations, HashAlgorithmName.SHA256, 32);
            var hashBase64 = Convert.ToBase64String(hashBytes);

            return $"pbkdf2_sha256${iterations}${salt}${hashBase64}";
        }
    }
}
