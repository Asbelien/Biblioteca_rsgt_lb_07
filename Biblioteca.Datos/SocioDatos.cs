using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Biblioteca.Entidades;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Datos
{
    public class SocioDatos
    {
        private const string SelectBase =
            "SELECT SocioId, DNI, Nombre, Email, Activo FROM Socios ";

        public async Task<List<Socio>> BuscarActivosAsync(string filtro)
        {
            var lista = new List<Socio>();
            const string sql = SelectBase +
                "WHERE Activo = 1 AND (Nombre LIKE @Filtro OR DNI LIKE @Filtro) ORDER BY Nombre";

            using (var cn = Conexion.Crear())
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@Filtro", SqlDbType.NVarChar, 100).Value = "%" + (filtro ?? string.Empty) + "%";
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

        public async Task<Socio> ObtenerPorIdAsync(int socioId)
        {
            const string sql = SelectBase + "WHERE SocioId = @SocioId";

            using (var cn = Conexion.Crear())
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@SocioId", SqlDbType.Int).Value = socioId;
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

        public async Task<bool> ExisteDniAsync(string dni, int socioIdExcluido)
        {
            const string sql = "SELECT COUNT(1) FROM Socios WHERE DNI = @DNI AND SocioId <> @SocioId";

            using (var cn = Conexion.Crear())
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@DNI", SqlDbType.Char, 8).Value = dni;
                cmd.Parameters.Add("@SocioId", SqlDbType.Int).Value = socioIdExcluido;
                await cn.OpenAsync();
                return (int)await cmd.ExecuteScalarAsync() > 0;
            }
        }

        public async Task<int> ContarPendientesAsync(int socioId)
        {
            const string sql =
                "SELECT COUNT(1) FROM DetallePrestamo d " +
                "INNER JOIN Prestamos p ON p.PrestamoId = d.PrestamoId " +
                "WHERE p.SocioId = @SocioId AND d.FechaDevolucion IS NULL";

            using (var cn = Conexion.Crear())
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@SocioId", SqlDbType.Int).Value = socioId;
                await cn.OpenAsync();
                return (int)await cmd.ExecuteScalarAsync();
            }
        }

        public async Task<int> InsertarAsync(Socio socio)
        {
            const string sql =
                "INSERT INTO Socios (DNI, Nombre, Email) " +
                "OUTPUT INSERTED.SocioId VALUES (@DNI, @Nombre, @Email)";

            using (var cn = Conexion.Crear())
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@DNI", SqlDbType.Char, 8).Value = socio.DNI;
                cmd.Parameters.Add("@Nombre", SqlDbType.NVarChar, 100).Value = socio.Nombre;
                cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 120).Value = socio.Email;
                await cn.OpenAsync();
                return (int)await cmd.ExecuteScalarAsync();
            }
        }

        public async Task<int> ActualizarAsync(Socio socio)
        {
            const string sql =
                "UPDATE Socios SET DNI = @DNI, Nombre = @Nombre, Email = @Email WHERE SocioId = @SocioId";

            using (var cn = Conexion.Crear())
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@DNI", SqlDbType.Char, 8).Value = socio.DNI;
                cmd.Parameters.Add("@Nombre", SqlDbType.NVarChar, 100).Value = socio.Nombre;
                cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 120).Value = socio.Email;
                cmd.Parameters.Add("@SocioId", SqlDbType.Int).Value = socio.SocioId;
                await cn.OpenAsync();
                return await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task<int> DesactivarAsync(int socioId)
        {
            const string sql = "UPDATE Socios SET Activo = 0 WHERE SocioId = @SocioId";

            using (var cn = Conexion.Crear())
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@SocioId", SqlDbType.Int).Value = socioId;
                await cn.OpenAsync();
                return await cmd.ExecuteNonQueryAsync();
            }
        }

        private static Socio Mapear(SqlDataReader dr)
        {
            return new Socio
            {
                SocioId = dr.GetInt32(0),
                DNI = dr.GetString(1),
                Nombre = dr.GetString(2),
                Email = dr.GetString(3),
                Activo = dr.GetBoolean(4)
            };
        }
    }
}