public class JwtConfig
{
    public string? Secret { get; set; }
    public int TokenExpirationHours { get; set; }
}