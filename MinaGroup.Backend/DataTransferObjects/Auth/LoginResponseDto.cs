namespace MinaGroup.Backend.DataTransferObjects.Auth
{
    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Expires { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshTokenExpires { get; set; }
    }
}
