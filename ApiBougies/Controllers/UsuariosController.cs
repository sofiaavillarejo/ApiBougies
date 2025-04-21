using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ApiBougies.Helpers;
using Bougies.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using NugetBougies.Models;

namespace ApiBougies.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private RepositoryBougies repo;
        private HelperActionBougies helper;

        public UsuariosController(RepositoryBougies repo, HelperActionBougies helper)
        {
            this.repo = repo;
            this.helper = helper;
        }

        [HttpPost("Registro")]
        public async Task<IActionResult> Registro([FromForm] Usuario user, [FromForm] IFormFile? imagen)
        {
            //CAMBIAR PARA CONTROLLAR EL FILE DESDE MVC SOLO, NO EN LA API -> DA ERROR
            string? fileName = null;

            // Subida de imagen
            if (imagen != null && imagen.Length > 0)
            {
                var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(imagen.FileName).ToLower();

                if (!extensionesPermitidas.Contains(extension))
                {
                    return BadRequest(new { error = "Formato de imagen no válido. Usa JPG, JPEG, PNG o GIF." });
                }

                fileName = Guid.NewGuid().ToString() + extension;
                var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "users");

                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                var filePath = Path.Combine(directoryPath, fileName);

                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imagen.CopyToAsync(stream);
                    }
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { error = "Error al guardar la imagen." });
                }
            }

            user.Imagen = fileName ?? "userprofile.jpg"; // Asignar la imagen por defecto si no se sube una nueva

            try
            {
                bool registrado = await this.repo.RegistrarUser(user.Nombre, user.Apellidos, user.Email, user.Imagen, user.Passwd);

                if (!registrado)
                {
                    return Conflict(new { error = "El email ya está registrado." });
                }

                return Ok(new { success = true, message = "¡Ya puedes iniciar sesión! 🚀" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al registrar el usuario." });
            }
        }

        [HttpPost("Login")]
        public async Task<ActionResult> Login(string email, string passwd)
        {
            if (email == null || passwd == null)
            {
                return BadRequest(new { error = "El correo y la contraseña son obligatorios" });
            }

            Usuario user = await this.repo.LoginUser(email, passwd);
            if (user == null || !BCrypt.Net.BCrypt.Verify(passwd, user.Passwd))
            {
                return Unauthorized(new { error = "Correo electrónico o contraseña incorrectos." });
            }
            else
            {
                SigningCredentials credentials = new SigningCredentials(this.helper.GetKeyToken(), SecurityAlgorithms.HmacSha256);

                string jsonUser = JsonConvert.SerializeObject(user);
                string jsonCifrado = HelperCryptography.EncryptString(jsonUser);

                Claim[] info = new[]
                {
                        new Claim("UserData", jsonCifrado),
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

        [Authorize]
        [HttpGet("PerfilUser/{iduser}")]
        public async Task<ActionResult<Usuario>> PerfilUser(int iduser)
        {
            Usuario user = await this.repo.PerfilUsuarioAsync(iduser);
            if (user == null)
            {
                return NotFound("Usuario no encontrado.");
            }
            else
            {
                return Ok(user);
            }

        }

        [HttpPost("UpdateUser/{iduser}")]
        public async Task<ActionResult> UpdateUser([FromRoute] int iduser, [FromForm] string? nuevaPasswd, [FromForm] IFormFile? nuevaImagen)
        {
            Usuario user = await this.repo.PerfilUsuarioAsync(iduser);
            if (user == null)
            {
                return NotFound("Usuario no encontrado");
            }
            else
            {
                bool updated = await this.repo.ActualizarPerfilAsync(user, nuevaPasswd, nuevaImagen);
                if (!updated)
                {
                    return BadRequest(new { error = "Hubo un error al actualizar el perfil." });
                }
                else
                {
                    return Ok(new { success = true, message = "Perfil actualizado correctamente." });

                }
            }
        }

        [HttpGet("PedidosUser/{iduser}")]
        public async Task<ActionResult<List<Pedido>>> PedidosUser(int iduser)
        {
            List<Pedido> pedidos = await this.repo.GetPedidoUserAsync(iduser);
            if (pedidos != null)
            {
                return Ok(pedidos);
            }
            else
            {
                return NotFound("Usuario no encontrado.");
            }
        }


    }
}
