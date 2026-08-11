using Airport.Simulator.Abstractions;
using Airport.Simulator.Configurations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace Airport.Simulator.Services
{
    public class AuthService : IAuthService
    {
        #region Fields
        private readonly HttpClient _client;
        private readonly AuthEndpoints _authEndpoints;
        private readonly LoginCredentials _loginCredentials;
        private readonly ILogger<AuthService> _logger;
        #endregion

        public AuthService(
            HttpClient client,
            IOptions<AuthEndpoints> authEndpoints,
            IOptions<LoginCredentials> loginCredentials,
            ILogger<AuthService> logger)
        {
            _client = client;
            _authEndpoints = authEndpoints.Value;
            _loginCredentials = loginCredentials.Value;
            _client.BaseAddress = new Uri(_authEndpoints.BaseUrl);
            _logger = logger;
        }

        public async Task<string?> LoginAsync(CancellationToken ct = default)
        {
            var response = await _client.PostAsJsonAsync(_authEndpoints.Login, _loginCredentials, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to login to API. Status: {StatusCode}", response.StatusCode);

                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();

            return result?.Token;
        }

        public void Dispose() => _client.Dispose();

        private record AuthResponse(string Token);
    }
}
