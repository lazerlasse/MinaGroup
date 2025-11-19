using MinaGroup.Backend.Models;
using MinaGroup.Backend.Services.Interfaces;

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

            var digits = new string(cpr.Where(char.IsDigit).ToArray());
            return digits;
        }

        /// <summary>
        /// Returnerer fuldt dekrypteret CPR fra AppUser. 
        /// Brug denne KUN på stærkt beskyttede admin-views.
        /// </summary>
        public static string? GetDecryptedCpr(AppUser user, ICryptoService crypto)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.EncryptedPersonNumber))
                return null;

            return crypto.Unprotect(user.EncryptedPersonNumber);
        }

        /// <summary>
        /// Returnerer maskeret CPR i formatet ddMMyy-xxxx.
        /// (Dvs. første 6 cifre som fødselsdato, sidste 4 skjult som xxxx)
        /// </summary>
        public static string GetMaskedCpr(AppUser user, ICryptoService crypto)
        {
            var decrypted = GetDecryptedCpr(user, crypto);
            if (string.IsNullOrWhiteSpace(decrypted))
                return string.Empty;

            var digits = NormalizeCpr(decrypted);

            // Forventer 10 cifre, men vi er defensive
            if (digits.Length < 6)
                return string.Empty;

            var first6 = digits.Substring(0, 6);
            return $"{first6}-xxxx";
        }
    }
}
