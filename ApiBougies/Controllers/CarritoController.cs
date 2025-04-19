using Bougies.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NugetBougies.Models;
using ApiBougies.Extensions;

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
            if(prod == null)
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
            }else
            {
                return NotFound("No se ha podido actualizar la cantidad o no existe el producto con ese ID.");
            }

        }


    }
}
