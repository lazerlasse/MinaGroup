namespace MinaGroup.Backend.DataTransferObjects.Auth
{
    public class RefreshTokenResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshTokenExpires { get; set; }
    }
}
