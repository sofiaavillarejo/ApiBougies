using System.Runtime.CompilerServices;
using Bougies.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NugetBougies.Models;

namespace ApiBougies.Controllers
{
    [Route("api/")]
    [ApiController]
    public class TiendaController : ControllerBase
    {
        private RepositoryBougies repo;

        public TiendaController(RepositoryBougies repo)
        {
            this.repo = repo;
        }

        [HttpGet]
        [Route("Productos")]
        public async Task<ActionResult<List<Producto>>> GetProductos()
        {
            return await this.repo.GetProductosAsync();
        }

        //[HttpGet]
        //[Route("Producto/{idproducto}")]
        //public async Task<Producto> FindProducto (int idproducto)
        //{
        //    return await this.repo.FindProducto(idproducto);
        //}

        [HttpGet("DetalleProducto/{idproducto}")]
        public async Task<ActionResult<Producto>> DetalleProducto(int idproducto)
        {
            Producto prod = await this.repo.DetalleProducto(idproducto);
            if (prod == null)
            {
                return NotFound("Producto no encontrado");
            }
            else
            {
                return Ok(prod);
            }
        }

        [HttpGet]
        [Route("ValorDescuento/{iddescuento}")]
        public async Task<int> GetValorDescuento(int iddescuento)
        {
            return await this.repo.GetValorDescuentoAsync(iddescuento);
        }

        [HttpGet]
        [Route("ProductosRebajados")]
        public async Task<ActionResult<List<Producto>>> GetProductosRebajados()
        {
            return await this.repo.GetProductosRebajadosAsync();
        }
    }
}
