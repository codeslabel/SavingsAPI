namespace SavingsAPI.Settings
{
    public class AuthResult
    {
        public string ResultToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; }
        public bool Result { get; set; }
        public List<string> Errors { get; set; }
    }
}