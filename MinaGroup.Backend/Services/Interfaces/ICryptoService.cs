namespace MinaGroup.Backend.Services.Interfaces
{
    public interface ICryptoService
    {
        string Protect(string plaintext);
        string Unprotect(string protectedText);
        string ComputeHmac(string plaintext); // deterministisk lookup hash
    }
}
