using NugetBougies.Models;

namespace Bougies.Repositories
{
    public interface IRepositoryBougies
    {
        #region TIENDA
        Task<List<Producto>> GetProductosAsync();
        Task<Producto> FindProducto(int id);
        Task<int> GetValorDescuentoAsync(int idDescuento);
        Task<List<Producto>> GetProductosRebajadosAsync();
        #endregion

        Task CreateProducto(string nombre, string descripcion, decimal precio, int stock, int idCategoria, int idDescuento, string imagen);
        Task UpdateProducto(int id, string nombre, string descripcion, decimal precio, int stock, int idCategoria, int idDescuento, string imagen);
        Task DeleteProducto(int id);
        #region CARRITO
        Task<List<Descuento>> GetDescuentosAsync();
        //findProducto también
        Task<int> GetMaxIdDetallesPedido();
        Task<int> TramitarPedido(int idUsuario, int idMetodoPago, string direccion, string ciudad, string codigoPostal, string poblacion, List<Carrito> carrito);
        Task<CuponDescuento> FindCuponDescuentoAsync(string cupon);
        Task CuponUsado(string cupon);
        #endregion

        Task<List<Roles>> GetRolesAsync();
        Task<int> GetMaxIdPedido();
        Task<bool> RegistrarUser(string nombre, string apellidos, string email, string? fotoPerfil, string passwd);
        Task<Usuario> LoginUser(string email, string passwd);
        Task<Usuario> PerfilUsuarioAsync(int idUsuario);
        Task<bool> ActualizarPerfilAsync(Usuario usuario, string nuevaPasswd, IFormFile nuevaImagen);
        Task<List<Pedido>> GetPedidoUserAsync(int idUsuario);
    }
}
