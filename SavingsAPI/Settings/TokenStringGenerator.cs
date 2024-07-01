using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SavingsAPI.Controllers;
using SavingsAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using static SavingsAPI.Settings.IToken;

public class TokenStringGenerator : ITokenGenerator
{

    private readonly JwtConfig _jwtConfig;
    private readonly ILogger<TokenStringGenerator> _logger;
    private readonly UserManager<User> _userManager;

    public TokenStringGenerator(IOptions<JwtConfig> jwtConfig, ILogger<TokenStringGenerator> logger, UserManager<User> userManager)
    {
        _jwtConfig = jwtConfig.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    }
    public class TokenResponse
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
    public async Task<TokenResponse> GenerateJwtToken(User user)
    {
        if (user == null)
        {
            _logger.LogError("User object is null.");
            throw new ArgumentNullException(nameof(user), "User object is null.");
        }

        if (string.IsNullOrEmpty(_jwtConfig.Secret))
        {
            _logger.LogWarning("JWT secret is null or empty. Using default secret.");
        }

        var jwtTokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtConfig.Secret);

        var userRoles = await _userManager.GetRolesAsync(user);
        var roleClaims = userRoles.Select(role => new Claim(ClaimTypes.Role, role)).ToList();

        var claims = new List<Claim>
        {
            new Claim("id", user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, user.Email),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTime.UtcNow.ToString())
        };
        claims.AddRange(roleClaims);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(_jwtConfig.TokenExpirationHours),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
        };

        var token = jwtTokenHandler.CreateToken(tokenDescriptor);
        var jwtToken = jwtTokenHandler.WriteToken(token);

        var refreshToken = GenerateRefreshToken();

        // Save the refresh token in the database
        await SaveRefreshTokenAsync(user, refreshToken);

        return new TokenResponse
        {
            AccessToken = jwtToken,
            RefreshToken = refreshToken
        };
    }
   
    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
    private async Task SaveRefreshTokenAsync(User user, string refreshToken)
    {
        // Save the refresh token using UserManager.SetAuthenticationTokenAsync
        await _userManager.SetAuthenticationTokenAsync(
            user,
            TokenConstants.TokenProvider,
            TokenConstants.RefreshTokenName,
            refreshToken
        );

        // Optionally, store the expiry time if you want to handle refresh token expiration separately
        var refreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _userManager.SetAuthenticationTokenAsync(
            user,
            TokenConstants.TokenProvider,
            $"{TokenConstants.RefreshTokenName}_Expiry",
            refreshTokenExpiryTime.ToString()
        );
    }

    Task<SavingsAPI.Controllers.TokenResponse> ITokenGenerator.GenerateJwtToken(User user)
    {
        throw new NotImplementedException();
    }

    public static class TokenConstants
    {
        public const string TokenProvider = "MyApp";
        public const string RefreshTokenName = "RefreshToken";
    }
 

}