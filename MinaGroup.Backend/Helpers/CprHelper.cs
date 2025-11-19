using MinaGroup.Backend.Models;
using MinaGroup.Backend.Services.Interfaces;
using System.Linq;

namespace MinaGroup.Backend.Helpers
{
    public static class CprHelper
    {
        /// <summary>
        /// Fjerner alle ikke-cifre (så vi håndterer både xxxxxx-xxxx og xxxxxxxxxx).
        /// </summary>
        private static string NormalizeCpr(string? cpr)
        {
            if (string.IsNullOrWhiteSpace(cpr))
                return string.Empty;

            return new string(cpr.Where(char.IsDigit).ToArray());
        }

        /// <summary>
        /// Returnerer fuldt dekrypteret CPR fra AppUser.
        /// Bruges på stærkt beskyttede admin-views.
        /// </summary>
        private static string? GetDecryptedCpr(AppUser user, ICryptoService crypto)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.EncryptedPersonNumber))
                return null;

            try
            {
                return crypto.Unprotect(user.EncryptedPersonNumber);
            }
            catch
            {
                return "[Fejl ved dekryptering]";
            }
        }

        /// <summary>
        /// Returnerer CPR i standardformat ddMMyy-xxxx
        /// efter dekryptering og normalisering.
        /// </summary>
        public static string? GetFullCpr(AppUser user, ICryptoService crypto)
        {
            var decrypted = GetDecryptedCpr(user, crypto);
            if (string.IsNullOrWhiteSpace(decrypted))
                return null;

            var digits = NormalizeCpr(decrypted);
            if (digits.Length != 10)
                return decrypted; // fallback: vis raw, men det burde aldrig ske

            var first6 = digits.Substring(0, 6);
            var last4 = digits.Substring(6, 4);

            return $"{first6}-{last4}";
        }

        /// <summary>
        /// Returnerer maskeret CPR i formatet ddMMyy-xxxx
        /// (Første 6 cifre synlige, sidste 4 skjules).
        /// Bruges på selvevalueringer og andre medarbejder-views.
        /// </summary>
        public static string GetMaskedCpr(AppUser user, ICryptoService crypto)
        {
            var decrypted = GetDecryptedCpr(user, crypto);
            if (string.IsNullOrWhiteSpace(decrypted))
                return string.Empty;

            var digits = NormalizeCpr(decrypted);

            if (digits.Length < 6)
                return string.Empty;

            var first6 = digits.Substring(0, 6);
            return $"{first6}-xxxx";
        }
    }
}