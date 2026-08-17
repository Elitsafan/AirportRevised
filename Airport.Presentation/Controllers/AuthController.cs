using Airport.Models.DTOs;
using Airport.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Airport.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        #region Fields
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;
        #endregion

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        // GET: api/Auth/Login
        [HttpPost("[action]")]
        public IActionResult Login([FromBody] LoginCredentials loginDto)
        {
            if (string.IsNullOrWhiteSpace(loginDto.Username) || string.IsNullOrWhiteSpace(loginDto.Password))
                _logger.LogWarning("Invalid login credentials.");

            var token = _authService.Login(loginDto.Username, loginDto.Password);

            if (token is null)
                return Unauthorized();

            return Ok(new { Token = token });
        }
    }
}
