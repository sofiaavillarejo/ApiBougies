using System.Data;
using Bougies.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NugetBougies.Models;

namespace Bougies.Repositories
{
    public class RepositoryBougies : IRepositoryBougies
    {
        private BougiesContext context;

        public RepositoryBougies(BougiesContext context)
        {
            this.context = context;
        }

        #region REPOSITORY TIENDA
        public async Task<List<Producto>> GetProductosAsync()
        {
            var consulta = from datos in this.context.Producto select datos;
            return await consulta.ToListAsync();
        }

        public async Task CreateProducto(string nombre, string descripcion, decimal precio, int stock, int idCategoria, int idDescuento, string imagen)
        {
            var nuevoProducto = new Producto
            {
                Nombre = nombre,
                Descripcion = descripcion,
                Precio = precio,
                Stock = stock,
                IdCategoria = idCategoria,
                IdDescuento = idDescuento,
                Imagen = imagen
            };

            this.context.Producto.Add(nuevoProducto);
            await this.context.SaveChangesAsync();
        }
        public async Task<Producto> FindProducto(int id)
        {
            var consulta = from datos in this.context.Producto where datos.Id == id select datos;
            return await consulta.FirstOrDefaultAsync();
        }

        public async Task UpdateProducto(int id, string nombre, string descripcion, decimal precio, int stock, int idCategoria, int idDescuento, string imagen)
        {
            Producto prod = await this.FindProducto(id);

            if (prod != null)
            {
                prod.Nombre = nombre;
                prod.Descripcion = descripcion;
                prod.Precio = precio;
                prod.Stock = stock;
                prod.IdCategoria = idCategoria;
                prod.IdDescuento = idDescuento;
                prod.Imagen = imagen;

                await this.context.SaveChangesAsync();
            }
        }

        //extraer los descuentos de la bbdd
        public async Task<List<Descuento>> GetDescuentosAsync()
        {
            var consulta = from datos in this.context.Descuento select datos;
            return await consulta.ToListAsync();
        }

        public async Task DeleteProducto(int id)
        {
            Producto prod = await this.FindProducto(id);
            this.context.Producto.Remove(prod);
            await this.context.SaveChangesAsync();
        }

        //METODO PARA COGER LOS ROLES QUE HAY E IMPLANTARLO EN LOS MENUS DE PLANTILLAS
        public async Task<List<Roles>> GetRolesAsync()
        {
            var consulta = from datos in this.context.Roles select datos;
            return await consulta.ToListAsync();
        }

        public async Task<int> GetValorDescuentoAsync(int idDescuento)
        {
            var descuento = await this.context.Descuento
                                .Where(d => d.Id == idDescuento)
                                .Select(d => d.Valor)
                                .FirstOrDefaultAsync();

            return descuento; // Si no encuentra el descuento, devolverá 0 por defecto.
        }

        public async Task<Producto> DetalleProducto(int idproducto)
        {
            Producto prod = await this.FindProducto(idproducto);
            if(prod != null && prod.IdDescuento != null)
            {
                int descuento = await this.GetValorDescuentoAsync(prod.IdDescuento);
                prod.PrecioDescuento = prod.Precio - (prod.Precio * ((decimal)descuento / 100));
            }else if (prod != null){
                prod.PrecioDescuento = prod.Precio;
            }
            return prod;
        }
        #endregion

        #region REPOSITORY CARRITO

        //sacar el id mas alto registrado
        public async Task<int> GetMaxIdPedido()
        {
            if (this.context.Pedidos.Count() == 0)
            {
                return 1;
            }
            else
            {
                return await this.context.Pedidos.MaxAsync(p => p.IdPedido) + 1;
            }
        }

        public async Task<int> GetMaxIdDetallesPedido()
        {
            if (this.context.DetallesPedido.Count() == 0)
            {
                return 1;
            }
            else
            {
                return await this.context.DetallesPedido.MaxAsync(p => p.Id) + 1;
            }
        }

        public async Task<int> TramitarPedido(int idUsuario, int idMetodoPago, string direccion, string ciudad, string codigoPostal, string poblacion, List<Carrito> carrito)
        {
            if (carrito == null || carrito.Count == 0)
            {
                throw new InvalidOperationException("No hay productos en el carrito");
            }

            decimal totalPedido = carrito.Sum(item => (item.Precio - (item.Precio * (item.Descuento / 100m))) * item.Cantidad);

            Pedido pedido = new Pedido
            {
                IdPedido = await this.GetMaxIdPedido(),
                IdUsuario = idUsuario,
                FechaPedido = DateTime.Now,
                Total = totalPedido,
                Estado = "Pendiente",
                IdMetodoPago = idMetodoPago,
                Direccion = direccion,
                Ciudad = ciudad,
                CodigoPostal = codigoPostal,
                Poblacion = poblacion
            };

            await this.context.Pedidos.AddAsync(pedido);
            await this.context.SaveChangesAsync();

            int iddetPedido = await this.GetMaxIdDetallesPedido();

            // Recorrer los productos agrupados por IdProducto
            foreach (var item in carrito.GroupBy(x => x.IdProducto))
            {
                DetallesPedido detallePed = new DetallesPedido
                {
                    Id = iddetPedido++,
                    IdPedido = pedido.IdPedido, 
                    IdProducto = item.Key,
                    Cantidad = item.Sum(x => x.Cantidad),
                    Total = item.Sum(x => (x.Precio - (x.Precio * (x.Descuento / 100m))) * x.Cantidad)
                };

                await this.context.DetallesPedido.AddAsync(detallePed);
            }
            await this.context.SaveChangesAsync();
            return pedido.IdPedido;
        }

        // ----------------- BUSCAR CUPON EN LA BBDD -----------------
        public async Task<CuponDescuento> FindCuponDescuentoAsync(string cupon)
        {
            var consulta = from datos in this.context.CuponDescuento where datos.Codigo == cupon select datos;
            return await consulta.FirstOrDefaultAsync();
        }

        public async Task CuponUsado(string cupon)
        {
            CuponDescuento codigoCupon = await this.FindCuponDescuentoAsync(cupon);
            if (codigoCupon != null)
            {
                codigoCupon.Usado = true;

                await this.context.SaveChangesAsync();
            }
            else
            {
                throw new Exception("El cupón no se encontró en la base de datos.");
            }
        }

        public async Task DesmarcarCuponUsado(string cupon)
        {
            var cuponUsado = await this.context.CuponDescuento.FirstOrDefaultAsync(c => c.Codigo == cupon && c.Usado == true);
            if (cuponUsado != null)
            {
                cuponUsado.Usado = false;
                this.context.Update(cuponUsado);
                await this.context.SaveChangesAsync();
            }
        }

        #endregion

        #region REPOSITORY OFERTAS
        public async Task<List<Producto>> GetProductosRebajadosAsync()
        {
            var consulta = from datos in this.context.Producto where datos.IdDescuento != 1 select datos;
            return await consulta.ToListAsync();
        }
        #endregion

        #region REPOSITORY USUARIOS
        private async Task<int> GetMaxIdUserAsync()
        {
            if (this.context.Usuarios.Count() == 0)
            {
                return 1;
            }
            else
            {
                return await this.context.Usuarios.MaxAsync(x => x.IdUsuario) + 1;
            }
        }
        public async Task<bool> RegistrarUser(string nombre, string apellidos, string email, string? fotoPerfil, string passwd)
        {
            if (await this.context.Usuarios.AnyAsync(u => u.Email == email))
            {
                return false; // Retorna false si el email ya existe
            }

            if (!ValidarPwd(passwd))
            {
                throw new InvalidOperationException("La contraseña no cumple con los requisitos de seguridad.");
            }

            Usuario user = new Usuario
            {
                IdUsuario = await this.GetMaxIdUserAsync(),
                Nombre = nombre,
                Apellidos = apellidos,
                Email = email,
                Imagen = fotoPerfil ?? "images/users/userprofile.jpg",
                Passwd = HashPwd(passwd),
                IdRol = 2,
                CreatedAt = DateTime.Now
            };

            await this.context.Usuarios.AddAsync(user);
            await this.context.SaveChangesAsync();

            return true; // Retorna true si el usuario fue registrado con éxito
        }


        //hashear contraseña
        private string HashPwd(string passwd)
        {
            return BCrypt.Net.BCrypt.HashPassword(passwd);
        }

        //validar contraseña
        private bool ValidarPwd(string passwd)
        {
            if (string.IsNullOrEmpty(passwd))
            {
                return false; // Contraseña vacía no válida
            }

            // Longitud mínima de 8 caracteres
            if (passwd.Length < 8)
            {
                return false;
            }

            // Debe tener al menos una letra mayúscula
            if (!passwd.Any(char.IsUpper))
            {
                return false;
            }

            // Debe tener al menos un carácter especial
            if (!passwd.Any(ch => "!@#$%^&*()_+-=<>?/".Contains(ch)))
            {
                return false;
            }

            return true;
        }

        //LOGIN------------------
        public async Task<Usuario> LoginUser(string email, string passwd)
        {
            var user = await this.context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                return null;
            }

            bool isPasswordCorrect = BCrypt.Net.BCrypt.Verify(passwd, user.Passwd);

            if (isPasswordCorrect)
            {
                return user;
            }

            return null;
        }

        //--------USER PERFIL---------------//
        public async Task<Usuario> PerfilUsuarioAsync(int idUsuario)
        {
            var user = await this.context.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);
            return user;
        }

        public async Task<bool> ActualizarPerfilAsync(Usuario usuario, string nuevaPasswd, IFormFile nuevaImagen)
        {
            var user = await this.context.Usuarios.FindAsync(usuario.IdUsuario);
            if (user == null)
                return false;

            // Actualizar datos
            user.Nombre = usuario.Nombre;
            user.Apellidos = usuario.Apellidos;
            user.Email = usuario.Email;

            // Si el usuario ingresó una nueva contraseña, se actualiza
            if (!string.IsNullOrEmpty(nuevaPasswd))
            {
                user.Passwd = BCrypt.Net.BCrypt.HashPassword(nuevaPasswd);
            }

            // Si hay nueva imagen, se actualiza
            if (nuevaImagen != null)
            {
                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(nuevaImagen.FileName);
                string filePath = Path.Combine("wwwroot/images/users", uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await nuevaImagen.CopyToAsync(stream);
                }

                user.Imagen = uniqueFileName;
            }

            this.context.Usuarios.Update(user);
            await this.context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Pedido>> GetPedidoUserAsync(int idUsuario)
        {
            List<Pedido> pedidos = await this.context.Pedidos.Where(p => p.IdUsuario == idUsuario).ToListAsync();
            return pedidos;
        }
        #endregion
        

    }
}