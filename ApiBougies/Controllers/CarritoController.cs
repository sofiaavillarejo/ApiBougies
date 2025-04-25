using Bougies.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NugetBougies.Models;
using ApiBougies.Extensions;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Authorization;

namespace ApiBougies.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CarritoController : ControllerBase
    {
        private RepositoryBougies repo;
        private const string SessionKeyCarrito = "Carrito";
        private const string SessionGastosEnvio = "GastosEnvio";

        public CarritoController(RepositoryBougies repo)
        {
            this.repo = repo;
        }

        [HttpGet]
        public async Task<ActionResult<List<Carrito>>> ObtenerCarrito()
        {
            if (HttpContext.Session.GetInt32("GastosEnvio") == null)
            {
                int gastosEnvio = 6;
                HttpContext.Session.SetInt32(SessionGastosEnvio, gastosEnvio);
            }

            var carrito = HttpContext.Session.GetObject<List<Carrito>>(SessionKeyCarrito) ?? new List<Carrito>();
            return Ok(carrito);
        }

        [HttpPost("AddProducto/{idproducto}")]
        public async Task<ActionResult> AddProductCarrito(int idproducto)
        {
            Producto? prod = await this.repo.FindProducto(idproducto);
            if (prod == null)
            {
                return NotFound("Producto no encontrado.");
            }

            var carrito = HttpContext.Session.GetObject<List<Carrito>>(SessionKeyCarrito)
                      ?? new List<Carrito>();

            var descuentos = await this.repo.GetDescuentosAsync();
            var desc = descuentos.FirstOrDefault(d => d.Id == prod.IdDescuento);
            int idDescuento = desc?.Id ?? 0;
            int valorDescuento = desc?.Valor ?? 0;

            var item = carrito.FirstOrDefault(c => c.IdProducto == idproducto);
            if (item != null)
            {
                item.Cantidad++;
            }
            else
            {
                carrito.Add(new Carrito
                {
                    IdProducto = prod.Id,
                    Nombre = prod.Nombre,
                    Precio = prod.Precio,
                    Cantidad = 1,
                    Imagen = prod.Imagen,
                    IdDescuento = idDescuento,
                    Descuento = valorDescuento
                });
            }

            decimal importeDesc = (prod.Precio * valorDescuento) / 100m;
            HttpContext.Session.SetDecimal("ImporteDescuento", importeDesc);

            HttpContext.Session.SetObject(SessionKeyCarrito, carrito);

            return Ok(carrito);
        }

        [HttpPost("UpdateCantidad/{idproducto}/{accion}")]
        public ActionResult UpdateCantidad(int idproducto, string accion)
        {
            const string SessionKeyCarrito = "Carrito";
            List<Carrito> carrito = HttpContext.Session.GetObject<List<Carrito>>(SessionKeyCarrito)
                                    ?? new List<Carrito>();

            var item = carrito.FirstOrDefault(p => p.IdProducto == idproducto);
            if (item != null)
            {
                if (accion == "sumar")
                {
                    item.Cantidad++;
                }
                else if (accion == "restar")
                {
                    item.Cantidad--;

                    if (item.Cantidad <= 0)
                    {
                        carrito.Remove(item);
                    }
                }

                HttpContext.Session.SetObject(SessionKeyCarrito, carrito);
                return Ok(carrito);
            }
            else
            {
                return NotFound("No se ha podido actualizar la cantidad o no existe el producto con ese ID.");
            }

        }

        [HttpPost("DeleteProduct/{idproducto}")]
        public async Task<ActionResult> DeleteProduct(int idproducto)
        {
            var carrito = HttpContext.Session.GetObject<List<Carrito>>(SessionKeyCarrito) ?? new List<Carrito>();
            var cupon = HttpContext.Session.GetString("DESCUENTO");

            carrito.RemoveAll(p => p.IdProducto == idproducto);
            HttpContext.Session.Remove(SessionKeyCarrito);
            HttpContext.Session.Remove("DescuentoTotal");
            HttpContext.Session.Remove("TotalConDescuento");

            if (!carrito.Any())
            {
                HttpContext.Session.Remove("DESCUENTO");
                await this.repo.DesmarcarCuponUsado(cupon);
            }

            HttpContext.Session.SetObject(SessionKeyCarrito, carrito);
            return Ok(carrito);
        }

        [Authorize]
        [HttpPost("TramitarPedido")]
        public async Task<IActionResult> TramitarPedido(int idUsuario,int idMetodoPago,string direccion,string ciudad,string codigoPostal,string poblacion)
        {
            List<Carrito> carrito = HttpContext.Session.GetObject<List<Carrito>>(SessionKeyCarrito) ?? new List<Carrito>();

            if (carrito.Count == 0)
            {
                return BadRequest("El carrito está vacío.");
            }

            try
            {
                int idPedido = await this.repo.TramitarPedido(
                    idUsuario, idMetodoPago, direccion, ciudad, codigoPostal, poblacion, carrito
                );

                HttpContext.Session.Remove(SessionKeyCarrito);

                return Ok(new
                {
                    mensaje = "Pedido tramitado con éxito.",
                    idPedido = idPedido
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al tramitar el pedido: {ex.Message}");
            }
        }

        [HttpGet("GetCuponDescuento/{cupon}")]
        public async Task<ActionResult<CuponDescuento>> GetCuponDescuento(string cupon)
        {
            try
            {
                CuponDescuento cuponDescuento = await this.repo.FindCuponDescuentoAsync(cupon);

                if (cuponDescuento == null)
                {
                    return NotFound($"No se encontró un cupón con el código: {cupon}");
                }

                return Ok(cuponDescuento);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ocurrió un error al buscar el cupón: {ex.Message}");
            }
        }

        [HttpPost("CuponUsado/{cupon}")]
        public async Task<ActionResult> CuponUsado(string cupon)
        {
            try
            {
                await this.repo.CuponUsado(cupon);
                return Ok($"El cupón '{cupon}' ha sido marcado como usado.");
            }
            catch (Exception ex)
            {
                return NotFound($"Error al marcar el cupón como usado: {ex.Message}");
            }
        }

        [HttpPost("VaciarCarrito")]
        public async Task<ActionResult> VaciarCarrito()
        {
            string codigoDescuento = HttpContext.Session.GetString("DESCUENTO");
            if (string.IsNullOrEmpty(codigoDescuento))
            {
                HttpContext.Session.Remove(SessionKeyCarrito);
                HttpContext.Session.Remove("DESCUENTO");
                HttpContext.Session.Remove("DescuentoTotal");
                HttpContext.Session.Remove("TotalConDescuento");

                return RedirectToAction("Productos", "Tienda");
            }
            else
            {
                await this.repo.DesmarcarCuponUsado(codigoDescuento);
                HttpContext.Session.Remove(SessionKeyCarrito);
                HttpContext.Session.Remove("DESCUENTO");
                HttpContext.Session.Remove("DescuentoTotal");
                HttpContext.Session.Remove("TotalConDescuento");

                return RedirectToAction("Productos", "Tienda");
            }
        }

        [HttpPost("AplicarCupon/{cupon}")]
        public async Task<ActionResult> AplicarCupon(string cupon)
        {
            if (HttpContext.Session.GetString("DESCUENTO") != null)
            {
                return BadRequest(new { message = "El cupón ya ha sido aplicado anteriormente." });
            }

            CuponDescuento codigoCupon = await this.repo.FindCuponDescuentoAsync(cupon);
            if (codigoCupon == null || !codigoCupon.Activo || codigoCupon.Usado)
            {
                return BadRequest(new { message = "El código de cupón no es válido o ya ha sido utilizado." });
            }

            HttpContext.Session.SetString("DESCUENTO", codigoCupon.Codigo);
            List<Carrito> carrito = HttpContext.Session.GetObject<List<Carrito>>(SessionKeyCarrito) ?? new List<Carrito>();
            decimal total = carrito.Sum(item => item.Precio * item.Cantidad);
            decimal descuentoTotal = (total * codigoCupon.Descuento) / 100;
            decimal totalConDescuento = total - descuentoTotal;

            HttpContext.Session.SetDecimal("DescuentoTotal", descuentoTotal);
            HttpContext.Session.SetDecimal("TotalConDescuento", totalConDescuento);

            await this.repo.CuponUsado(codigoCupon.Codigo);

            HttpContext.Session.SetObject(SessionKeyCarrito, carrito);

            return Ok(new
            {
                message = "Cupón aplicado correctamente.",
                carritoActualizado = carrito, 
                descuentoTotal = descuentoTotal,
                totalConDescuento = totalConDescuento
            });
        }


    }
}
