using Airport.Models.DTOs;
using Airport.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Airport.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService) => _authService = authService;

        // GET: api/Auth/Login
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginCredentials loginDto)
        {
            var token = _authService.Login(loginDto.Username, loginDto.Password);

            if (token is null)
                return Unauthorized();

            return Ok(new { Token = token });
        }
    }
}
