namespace MinaGroup.Backend.Models.Auth
{
    public class RefreshTokenResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshTokenExpires { get; set; }
    }
}
