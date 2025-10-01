using Microsoft.AspNetCore.DataProtection;
using MinaGroup.Backend.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace MinaGroup.Backend.Services
{
    public class DataProtectionCryptoService : ICryptoService
    {
        private readonly IDataProtector _protector;
        private readonly byte[] _hmacKey;

        // HMAC key bør komme fra konfiguration / KeyVault i produktion
        public DataProtectionCryptoService(IDataProtectionProvider provider, IConfiguration config)
        {
            _protector = provider.CreateProtector("MinaGroup.Backend.CPRProtector");

            // HMAC nøgle: i produktion hent fra secure store (KeyVault / env var / managed identity)
            // Min anbefaling: en 32+ byte base64-nøgle i appsettings (kun dev) - production: KeyVault
            var hmacKeyBase64 = config["Crypto:HmacKeyBase64"];
            if (string.IsNullOrEmpty(hmacKeyBase64))
            {
                // fallback: generer (kun dev). I prod: IKKE generer - fejl i startup.
                using var rng = RandomNumberGenerator.Create();
                var bytes = new byte[64];
                rng.GetBytes(bytes);
                hmacKeyBase64 = Convert.ToBase64String(bytes);
            }
            _hmacKey = Convert.FromBase64String(hmacKeyBase64);
        }

        public string Protect(string plaintext)
        {
            if (plaintext == null) return string.Empty;
            return _protector.Protect(plaintext);
        }

        public string Unprotect(string protectedText)
        {
            if (string.IsNullOrEmpty(protectedText)) return string.Empty;
            try
            {
                return _protector.Unprotect(protectedText);
            }
            catch
            {
                // håndter fejl (fx log). Return empty for sikkerheds skyld.
                return string.Empty;
            }
        }

        public string ComputeHmac(string plaintext)
        {
            if (plaintext == null) return string.Empty;
            using var hmac = new HMACSHA256(_hmacKey);
            var bytes = Encoding.UTF8.GetBytes(plaintext);
            var hash = hmac.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
