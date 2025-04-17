using NugetBougies.Models;
using Microsoft.EntityFrameworkCore;

namespace Bougies.Data
{
    public class BougiesContext : DbContext
    {
        public BougiesContext(DbContextOptions<BougiesContext> options) : base(options) { }
        public DbSet<Producto> Producto { get; set; }
        public DbSet<Descuento> Descuento { get; set; }
        public DbSet<Roles> Roles { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<DetallesPedido> DetallesPedido {get; set;}
        public DbSet<CuponDescuento> CuponDescuento { get; set; }

    }
}
