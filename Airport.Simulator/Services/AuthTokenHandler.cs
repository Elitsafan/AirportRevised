using Airport.Simulator.Abstractions;
using System.Net.Http.Headers;

namespace Airport.Simulator.Services
{
    public class AuthTokenHandler : DelegatingHandler
    {
        #region Fields
        private readonly IAuthService _authService;
        private string? _token;
        private readonly string _scheme;
        #endregion

        public AuthTokenHandler(IAuthService authService)
        {
            _authService = authService;
            _scheme = "Bearer";
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(_token))
                _token = await _authService.LoginAsync(ct);

            if (!string.IsNullOrEmpty(_token))
                request.Headers.Authorization = new AuthenticationHeaderValue(_scheme, _token);

            var response = await base.SendAsync(request, ct);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _token = await _authService.LoginAsync(ct);

                if (!string.IsNullOrEmpty(_token))
                    request.Headers.Authorization = new AuthenticationHeaderValue(_scheme, _token);

                response = await base.SendAsync(request, ct);
            }

            return response;
        }
    }
}
