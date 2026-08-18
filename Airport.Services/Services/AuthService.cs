using Airport.Services.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Airport.Services.Services
{
    public class AuthService : IAuthService
    {
        #region Fields
        private readonly JwtSettings _jwtSettings;
        private readonly LoginCredentials _loginCredentials;
        #endregion

        public AuthService(IOptions<JwtSettings> jwtSettings, IOptions<LoginCredentials> loginCredentials)
        {
            _jwtSettings = jwtSettings.Value;
            _loginCredentials = loginCredentials.Value;
        }

        public string? Login(string username, string password)
        {
            var adminUsername = _loginCredentials.Username;
            var adminPassword = _loginCredentials.Password;

            if (username != adminUsername || password != adminPassword)
                return null;

            var keyBytes = Encoding.UTF8.GetBytes(_jwtSettings.Key);
            var securityKey = new SymmetricSecurityKey(keyBytes);
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var expiry = DateTime.Now.AddMinutes(_jwtSettings.ExpiryMinutes);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: expiry,
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
