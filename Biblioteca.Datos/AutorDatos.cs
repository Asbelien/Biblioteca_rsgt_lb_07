using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Biblioteca.Entidades;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Datos
{
    public class AutorDatos
    {
        public async Task<List<Autor>> ListarActivosAsync()
        {
            var lista = new List<Autor>();
            const string sql = "SELECT AutorId, Nombre, Nacionalidad, Activo FROM Autores WHERE Activo = 1 ORDER BY Nombre";

            using (var cn = Conexion.Crear())
            using (var cmd = new SqlCommand(sql, cn))
            {
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

        public async Task<Autor> ObtenerPorIdAsync(int autorId)
        {
            const string sql = "SELECT AutorId, Nombre, Nacionalidad, Activo FROM Autores WHERE AutorId = @AutorId";

            using (var cn = Conexion.Crear())
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@AutorId", SqlDbType.Int).Value = autorId;
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

        public async Task<int> InsertarAsync(Autor autor)
        {
            const string sql = "INSERT INTO Autores (Nombre, Nacionalidad) OUTPUT INSERTED.AutorId VALUES (@Nombre, @Nacionalidad)";

            using (var cn = Conexion.Crear())
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@Nombre", SqlDbType.NVarChar, 100).Value = autor.Nombre;
                cmd.Parameters.Add("@Nacionalidad", SqlDbType.NVarChar, 50).Value = autor.Nacionalidad;
                await cn.OpenAsync();
                return (int)await cmd.ExecuteScalarAsync();
            }
        }

        private static Autor Mapear(SqlDataReader dr)
        {
            return new Autor
            {
                AutorId = dr.GetInt32(0),
                Nombre = dr.GetString(1),
                Nacionalidad = dr.GetString(2),
                Activo = dr.GetBoolean(3)
            };
        }
    }
}