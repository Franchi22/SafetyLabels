// Api/Controllers/AuthController.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Api.Controllers
{
    [ApiController]
    // Expone el controlador en /auth y /api/auth
    [Route("auth")]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _users;
        private readonly SignInManager<IdentityUser> _signIn;
        private readonly IConfiguration _config;

        public AuthController(
            UserManager<IdentityUser> users,
            SignInManager<IdentityUser> signIn,
            IConfiguration config)
        {
            _users = users;
            _signIn = signIn;
            _config = config;
        }

        // ======== DTOs ========
        public record RegisterDto(string UserName, string Email, string Password, string ConfirmPassword);
        public record LoginDto(string UserName, string? Password);   // puedes enviar Email en UserName
        public record ForgotDto(string UserOrEmail);

        // ======== REGISTER ========
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.UserName) ||
                string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest("UserName y Password son obligatorios.");

            if (dto.Password != dto.ConfirmPassword)
                return BadRequest("Las contraseñas no coinciden.");

            if (await _users.FindByNameAsync(dto.UserName) is not null)
                return BadRequest("El usuario ya existe.");

            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                var byEmail = await _users.FindByEmailAsync(dto.Email);
                if (byEmail is not null) return BadRequest("El email ya está registrado.");
            }

            var user = new IdentityUser
            {
                UserName = dto.UserName,
                Email = dto.Email,
                EmailConfirmed = true
            };

            var result = await _users.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                var msg = string.Join("; ", result.Errors.Select(e => e.Description));
                return BadRequest(msg);
            }

            return Ok(new { id = user.Id, user.UserName, user.Email });
        }

        // ======== LOGIN (devuelve JWT) ========
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.UserName) ||
                string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest("UserName y Password son obligatorios.");

            // Permite que el campo UserName venga con email
            IdentityUser? user =
                dto.UserName.Contains('@')
                    ? await _users.FindByEmailAsync(dto.UserName)
                    : await _users.FindByNameAsync(dto.UserName);

            if (user is null)
                return Unauthorized("Usuario o contraseña inválidos.");

            // Verifica contraseña
            if (!await _users.CheckPasswordAsync(user, dto.Password!))
                return Unauthorized("Usuario o contraseña inválidos.");

            var token = await CreateJwtAsync(user);
            return Ok(new { token });
        }

        // ======== FORGOT (placeholder seguro) ========
        // No revela si el usuario existe; en producción genera y envía el token por correo
        [HttpPost("forgot")]
        public async Task<IActionResult> Forgot([FromBody] ForgotDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.UserOrEmail))
                return BadRequest("Debes indicar tu usuario o correo.");

            IdentityUser? user = dto.UserOrEmail.Contains('@')
                ? await _users.FindByEmailAsync(dto.UserOrEmail)
                : await _users.FindByNameAsync(dto.UserOrEmail);

            // No revelamos existencia por seguridad
            // Si quieres, aquí podrías: var token = await _users.GeneratePasswordResetTokenAsync(user!)
            await Task.CompletedTask;
            return Ok("Si los datos existen, se enviaron instrucciones.");
        }

        // ======== Utilidades ========
        private async Task<string> CreateJwtAsync(IdentityUser user)
        {
            var issuer = _config["Jwt:Issuer"];
            var audience = _config["Jwt:Audience"];
            var key = _config["Jwt:Key"];

            if (string.IsNullOrWhiteSpace(issuer) ||
                string.IsNullOrWhiteSpace(audience) ||
                string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("Configuración Jwt incompleta en appsettings.json");

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var roles = await _users.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? ""),
                new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.UserName ?? "")
            };

            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
