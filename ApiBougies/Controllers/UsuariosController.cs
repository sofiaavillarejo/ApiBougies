using Bougies.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NugetBougies.Models;

namespace ApiBougies.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private RepositoryBougies repo;

        public UsuariosController(RepositoryBougies repo)
        {
            this.repo = repo;
        }

        [HttpPost("Registro")]
        public async Task<IActionResult> Registro([FromForm] Usuario user, [FromForm] IFormFile? imagen)
        {
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

            try
            {
                Usuario user = await this.repo.LoginUser(email, passwd);
                if (user == null || !BCrypt.Net.BCrypt.Verify(passwd, user.Passwd))
                {
                    return Unauthorized(new { error = "Correo electrónico o contraseña incorrectos." });
                }
                return Ok(new
                {
                    success = true,
                    message = "Inicio de sesión exitoso",
                    user = new
                    {
                        user.IdUsuario,
                        user.Nombre,
                        user.Apellidos,
                        user.Email,
                        user.Imagen,
                        user.IdRol,
                        user.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error en el servidor", message = ex.Message });
            }
        }

        [HttpGet("PerfilUser/{iduser}")]
        public async Task<ActionResult<Usuario>> PerfilUser(int iduser)
        {
            Usuario user = await this.repo.PerfilUsuarioAsync(iduser);
            if(user == null)
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
            }else
            {
                bool updated = await this.repo.ActualizarPerfilAsync(user, nuevaPasswd, nuevaImagen);
                if (!updated)
                {
                    return BadRequest(new { error = "Hubo un error al actualizar el perfil." });
                }else
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
