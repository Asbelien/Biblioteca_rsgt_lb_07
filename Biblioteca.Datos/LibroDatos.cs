using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Biblioteca.Entidades;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Datos
{
    public class LibroDatos
    {
        private const string SelectBase =
            "SELECT l.LibroId, l.Titulo, l.ISBN, l.AutorId, a.Nombre, l.Ejemplares, l.Activo " +
            "FROM Libros l INNER JOIN Autores a ON a.AutorId = l.AutorId ";

        public async Task<List<Libro>> BuscarActivosAsync(string filtro)
        {
            var lista = new List<Libro>();
            const string sql = SelectBase +
                "WHERE l.Activo = 1 AND (l.Titulo LIKE @Filtro OR a.Nombre LIKE @Filtro) ORDER BY l.Titulo";

            using (var cn = Conexion.Crear())
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@Filtro", SqlDbType.NVarChar, 150).Value = "%" + (filtro ?? string.Empty) + "%";
                await cn.OpenAsync();
                using (var dr = await cmd.ExecuteReaderAsync())
                {
                    while (await dr.ReadAsync())
                    {
                        lista.Add(Mapear(dr));
                    }
                }
            }

            return lista;
        }

        public async Task<Libro> ObtenerPorIdAsync(int libroId)
        {
            const string sql = SelectBase + "WHERE l.LibroId = @LibroId";

            using (var cn = Conexion.Crear())
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@LibroId", SqlDbType.Int).Value = libroId;
                await cn.OpenAsync();
                using (var dr = await cmd.ExecuteReaderAsync())
                {
                    if (await dr.ReadAsync())
                    {
                        return Mapear(dr);
                    }
                }
            }

            return null;
        }

        public async Task<bool> ExisteIsbnAsync(string isbn, int libroIdExcluido)
        {
            const string sql = "SELECT COUNT(1) FROM Libros WHERE ISBN = @ISBN AND LibroId <> @LibroId";

            using (var cn = Conexion.Crear())
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@ISBN", SqlDbType.VarChar, 20).Value = isbn;
                cmd.Parameters.Add("@LibroId", SqlDbType.Int).Value = libroIdExcluido;
                await cn.OpenAsync();
                return (int)await cmd.ExecuteScalarAsync() > 0;
            }
        }

        public async Task<int> ContarPendientesAsync(int libroId)
        {
            const string sql = "SELECT COUNT(1) FROM DetallePrestamo WHERE LibroId = @LibroId AND FechaDevolucion IS NULL";

            using (var cn = Conexion.Crear())
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@LibroId", SqlDbType.Int).Value = libroId;
                await cn.OpenAsync();
                return (int)await cmd.ExecuteScalarAsync();
            }
        }

        public async Task<int> InsertarAsync(Libro libro)
        {
            const string sql =
                "INSERT INTO Libros (Titulo, ISBN, AutorId, Ejemplares) " +
                "OUTPUT INSERTED.LibroId VALUES (@Titulo, @ISBN, @AutorId, @Ejemplares)";

            using (var cn = Conexion.Crear())
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@Titulo", SqlDbType.NVarChar, 150).Value = libro.Titulo;
                cmd.Parameters.Add("@ISBN", SqlDbType.VarChar, 20).Value = libro.ISBN;
                cmd.Parameters.Add("@AutorId", SqlDbType.Int).Value = libro.AutorId;
                cmd.Parameters.Add("@Ejemplares", SqlDbType.Int).Value = libro.Ejemplares;
                await cn.OpenAsync();
                return (int)await cmd.ExecuteScalarAsync();
            }
        }

        public async Task<int> ActualizarAsync(Libro libro)
        {
            const string sql =
                "UPDATE Libros SET Titulo = @Titulo, ISBN = @ISBN, AutorId = @AutorId, Ejemplares = @Ejemplares " +
                "WHERE LibroId = @LibroId";

            using (var cn = Conexion.Crear())
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@Titulo", SqlDbType.NVarChar, 150).Value = libro.Titulo;
                cmd.Parameters.Add("@ISBN", SqlDbType.VarChar, 20).Value = libro.ISBN;
                cmd.Parameters.Add("@AutorId", SqlDbType.Int).Value = libro.AutorId;
                cmd.Parameters.Add("@Ejemplares", SqlDbType.Int).Value = libro.Ejemplares;
                cmd.Parameters.Add("@LibroId", SqlDbType.Int).Value = libro.LibroId;
                await cn.OpenAsync();
                return await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task<int> DesactivarAsync(int libroId)
        {
            const string sql = "UPDATE Libros SET Activo = 0 WHERE LibroId = @LibroId";

            using (var cn = Conexion.Crear())
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@LibroId", SqlDbType.Int).Value = libroId;
                await cn.OpenAsync();
                return await cmd.ExecuteNonQueryAsync();
            }
        }

        private static Libro Mapear(SqlDataReader dr)
        {
            return new Libro
            {
                LibroId = dr.GetInt32(0),
                Titulo = dr.GetString(1),
                ISBN = dr.GetString(2),
                AutorId = dr.GetInt32(3),
                AutorNombre = dr.GetString(4),
                Ejemplares = dr.GetInt32(5),
                Activo = dr.GetBoolean(6)
            };
        }
    }
}