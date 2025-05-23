﻿using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ApiBougies.DTO;
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
        private HelperUserToken helperuser;

        public UsuariosController(RepositoryBougies repo, HelperActionBougies helper, HelperUserToken helperuser)
        {
            this.repo = repo;
            this.helper = helper;
            this.helperuser = helperuser;
        }

        [HttpPost("Registro")]
        public async Task<IActionResult> Registro([FromBody] RegisterModel user)
        {
            try
            {
                bool registrado = await this.repo.RegistrarUser(user.Nombre, user.Apellidos, user.Email, user.Passwd, user.Imagen);

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
                return StatusCode(500, new { error = "Error al registrar el usuario.", detail = ex.Message });
            }
        }


        //[HttpPost("Login")]
        //public async Task<ActionResult> Login(string email, string passwd)
        //{
        //    if (email == null || passwd == null)
        //    {
        //        return BadRequest(new { error = "El correo y la contraseña son obligatorios" });
        //    }

        //    Usuario user = await this.repo.LoginUser(email, passwd);
        //    if (user == null || !BCrypt.Net.BCrypt.Verify(passwd, user.Passwd))
        //    {
        //        return Unauthorized(new { error = "Correo electrónico o contraseña incorrectos." });
        //    }
        //    else
        //    {
        //        SigningCredentials credentials = new SigningCredentials(this.helper.GetKeyToken(), SecurityAlgorithms.HmacSha256);
        //        string jsonUser = JsonConvert.SerializeObject(user);
        //        string jsonCifrado = HelperCryptography.EncryptString(jsonUser);

        //        Claim[] info = new[]
        //        {
        //                new Claim("UserData", jsonCifrado),
        //            };
        //        JwtSecurityToken token = new JwtSecurityToken(
        //            claims: info,
        //            issuer: this.helper.Issuer,
        //            audience: this.helper.Audience,
        //            signingCredentials: credentials,
        //            expires: DateTime.UtcNow.AddMinutes(20),
        //            notBefore: DateTime.UtcNow
        //        );

        //        return Ok(new { response = new JwtSecurityTokenHandler().WriteToken(token) });
        //    }
        //}

        //[Authorize]
        //[HttpGet("PerfilUser")]
        //public async Task<ActionResult<Usuario>> PerfilUser()
        //{
        //    Usuario user = this.helperuser.GetUser();
        //    if (user == null)
        //    {
        //        return NotFound("Usuario no encontrado.");
        //    }
        //    else
        //    {
        //        return Ok(user);
        //    }
        //}

        [Authorize]
        [HttpGet("PerfilBlob")]
        public async Task<ActionResult<UserModel>> PerfilUsuarioBlob()
        {

            UserModel model = this.helperuser.GetUser();
            var userblob = await this.repo.PerfilUsuarioBlobAsync(model.IdUsuario);
            return userblob;
        }

        [Authorize]
        [HttpPost("UpdateUser")]
        public async Task<ActionResult> UpdateUser([FromForm] Usuario usuario, [FromForm] string? nuevaPasswd, [FromForm] IFormFile? nuevaImagen)
        {
            UserModel userModel = this.helperuser.GetUser();
            Usuario user = await this.repo.PerfilUsuarioAsync(userModel.IdUsuario);
            if (user == null)
            {
                return NotFound("Usuario no encontrado");
            }
            else
            {
                user.Nombre = usuario.Nombre;
                user.Apellidos = usuario.Apellidos;
                user.Email = usuario.Email;
                bool updated = await this.repo.ActualizarPerfilAsync(user, nuevaPasswd, nuevaImagen);
                if (!updated)
                {
                    return BadRequest(new { error = "Hubo un error al actualizar el perfil." });
                }
                else
                {
                    var identity = HttpContext.User.Identity as ClaimsIdentity;
                    if (identity != null)
                    {
                        // Actualizar los claims con los nuevos valores
                        identity.RemoveClaim(identity.FindFirst("userName"));
                        identity.AddClaim(new Claim("userName", user.Nombre));

                        identity.RemoveClaim(identity.FindFirst("userImage"));
                        identity.AddClaim(new Claim("userImage", user.Imagen ?? "default_image.jpg"));
                    }

                    return Ok(new { success = true, message = "Perfil actualizado correctamente." });

                }
            }
        }

        [Authorize]
        [HttpGet("PedidosUser")]
        public async Task<ActionResult<List<Pedido>>> PedidosUser()
        {
            UserModel user = this.helperuser.GetUser();
            List<Pedido> pedidos = await this.repo.GetPedidoUserAsync(user.IdUsuario);
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


//[Authorize]
//[HttpPost("UpdateUser")]
//public async Task<ActionResult> UpdateUser(
//[FromForm] string? nombre,
//[FromForm] string? apellidos,
//[FromForm] string? email,
//[FromForm] string? nuevaPasswd,
//[FromForm] IFormFile? nuevaImagen)
//{
//    UserModel model = this.helperuser.GetUser();
//    if (model == null)
//    {
//        return NotFound("Usuario no encontrado");
//    }

//    // Obtener el usuario original desde la BD
//    Usuario userOriginal = await this.repo.PerfilUsuarioAsync(model.IdUsuario);
//    if (userOriginal == null)
//    {
//        return NotFound("Usuario no encontrado en base de datos");
//    }

//    // Solo actualizar si se recibe un valor nuevo
//    if (!string.IsNullOrEmpty(nombre)) userOriginal.Nombre = nombre;
//    if (!string.IsNullOrEmpty(apellidos)) userOriginal.Apellidos = apellidos;
//    if (!string.IsNullOrEmpty(email)) userOriginal.Email = email;

//    bool updated = await this.repo.ActualizarPerfilAsync(userOriginal, nuevaPasswd, nuevaImagen);
//    if (!updated)
//    {
//        return BadRequest(new { error = "Hubo un error al actualizar el perfil." });
//    }

//    return Ok(new { success = true, message = "Perfil actualizado correctamente." });
//}