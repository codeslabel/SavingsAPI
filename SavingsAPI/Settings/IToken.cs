using SavingsAPI.Controllers;
using SavingsAPI.Models;

namespace SavingsAPI.Settings
{
    public class IToken
    {
        public interface ITokenGenerator
        {
            Task<TokenResponse> GenerateJwtToken(User user);
        }
    }
}
