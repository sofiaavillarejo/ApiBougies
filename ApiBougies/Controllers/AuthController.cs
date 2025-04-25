using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ApiBougies.Helpers;
using ApiBougies.DTO;
using Bougies.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using NugetBougies.Models;

namespace ApiBougies.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private RepositoryBougies repo;
        private HelperActionBougies helper;

        public AuthController(RepositoryBougies repo, HelperActionBougies helper)
        {
            this.repo = repo;
            this.helper = helper;
        }

        [HttpPost("Login")]
        public async Task<ActionResult> LoginUser(LoginModel model)
        {
            Usuario user = await this.repo.LoginUserAsync(model.Email, model.Passwd);
            if (user == null)
            {
                return Unauthorized(new { error = "Correo electrónico o contraseña incorrectos." });
            }else
            {
                SigningCredentials credentials = new SigningCredentials(this.helper.GetKeyToken(), SecurityAlgorithms.HmacSha256);
                UserModel userModel = new UserModel ();
                userModel.IdUsuario = user.IdUsuario;
                userModel.Nombre = user.Nombre;
                userModel.Apellidos = user.Apellidos;
                userModel.Email = user.Email;
                userModel.Imagen = user.Imagen;
                userModel.Passwd = user.Passwd;
                userModel.IdRol = user.IdRol;
                userModel.CreatedAt = user.CreatedAt;

                string jsonEmpleado = JsonConvert.SerializeObject(userModel);
                string jsonCrifado = HelperCryptography.EncryptString(jsonEmpleado);
                Claim[] info = new[]
                {
                    new Claim("UserData", jsonCrifado)
                };
                JwtSecurityToken token = new JwtSecurityToken(
                    claims: info,
                     issuer: this.helper.Issuer,
                    audience: this.helper.Audience,
                    signingCredentials: credentials,
                    expires: DateTime.UtcNow.AddMinutes(20),
                    notBefore: DateTime.UtcNow
                );

                return Ok(new { response = new JwtSecurityTokenHandler().WriteToken(token) });
            }
        }
    }
}
