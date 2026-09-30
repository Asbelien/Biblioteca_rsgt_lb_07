using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Biblioteca.Entidades;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Datos
{
    public class PrestamoDatos
    {
        private const string SelectPrestamos =
            "SELECT p.PrestamoId, p.SocioId, s.Nombre, p.FechaPrestamo, p.FechaLimite, p.Estado, " +
            "d.LibroId, l.Titulo, d.FechaDevolucion " +
            "FROM Prestamos p " +
            "INNER JOIN DetallePrestamo d ON d.PrestamoId = p.PrestamoId " +
            "INNER JOIN Libros l ON l.LibroId = d.LibroId " +
            "INNER JOIN Socios s ON s.SocioId = p.SocioId ";

        public async Task<int> RegistrarAsync(Prestamo prestamo)
        {
            const string sqlCabecera =
                "INSERT INTO Prestamos (SocioId, FechaPrestamo, FechaLimite, Estado) " +
                "OUTPUT INSERTED.PrestamoId VALUES (@SocioId, @FechaPrestamo, @FechaLimite, @Estado)";
            const string sqlDetalle =
                "INSERT INTO DetallePrestamo (PrestamoId, LibroId) VALUES (@PrestamoId, @LibroId)";
            const string sqlStock =
                "UPDATE Libros SET Ejemplares = Ejemplares - 1 WHERE LibroId = @LibroId AND Ejemplares > 0";

            using (var cn = Conexion.Crear())
            {
                await cn.OpenAsync();
                var tx = (SqlTransaction)await cn.BeginTransactionAsync();

                try
                {
                    int prestamoId;

                    using (var cmd = new SqlCommand(sqlCabecera, cn, tx))
                    {
                        cmd.Parameters.Add("@SocioId", SqlDbType.Int).Value = prestamo.SocioId;
                        cmd.Parameters.Add("@FechaPrestamo", SqlDbType.Date).Value = prestamo.FechaPrestamo;
                        cmd.Parameters.Add("@FechaLimite", SqlDbType.Date).Value = prestamo.FechaLimite;
                        cmd.Parameters.Add("@Estado", SqlDbType.VarChar, 20).Value = prestamo.Estado;
                        prestamoId = (int)await cmd.ExecuteScalarAsync();
                    }

                    foreach (var detalle in prestamo.Detalles)
                    {
                        using (var cmd = new SqlCommand(sqlDetalle, cn, tx))
                        {
                            cmd.Parameters.Add("@PrestamoId", SqlDbType.Int).Value = prestamoId;
                            cmd.Parameters.Add("@LibroId", SqlDbType.Int).Value = detalle.LibroId;
                            await cmd.ExecuteNonQueryAsync();
                        }

                        using (var cmd = new SqlCommand(sqlStock, cn, tx))
                        {
                            cmd.Parameters.Add("@LibroId", SqlDbType.Int).Value = detalle.LibroId;
                            int filas = await cmd.ExecuteNonQueryAsync();
                            if (filas == 0)
                            {
                                throw new InvalidOperationException("No se pudo descontar el ejemplar del libro " + detalle.LibroId + ".");
                            }
                        }
                    }

                    await tx.CommitAsync();
                    return prestamoId;
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            }
        }

        public async Task DevolverLibroAsync(int prestamoId, int libroId, DateTime fechaDevolucion, bool cerrarPrestamo)
        {
            const string sqlDetalle =
                "UPDATE DetallePrestamo SET FechaDevolucion = @Fecha " +
                "WHERE PrestamoId = @PrestamoId AND LibroId = @LibroId AND FechaDevolucion IS NULL";
            const string sqlStock = "UPDATE Libros SET Ejemplares = Ejemplares + 1 WHERE LibroId = @LibroId";
            const string sqlEstado = "UPDATE Prestamos SET Estado = 'Devuelto' WHERE PrestamoId = @PrestamoId";

            using (var cn = Conexion.Crear())
            {
                await cn.OpenAsync();
                var tx = (SqlTransaction)await cn.BeginTransactionAsync();

                try
                {
                    using (var cmd = new SqlCommand(sqlDetalle, cn, tx))
                    {
                        cmd.Parameters.Add("@Fecha", SqlDbType.Date).Value = fechaDevolucion;
                        cmd.Parameters.Add("@PrestamoId", SqlDbType.Int).Value = prestamoId;
                        cmd.Parameters.Add("@LibroId", SqlDbType.Int).Value = libroId;
                        int filas = await cmd.ExecuteNonQueryAsync();
                        if (filas == 0)
                        {
                            throw new InvalidOperationException("El libro ya fue devuelto o no pertenece al préstamo.");
                        }
                    }

                    using (var cmd = new SqlCommand(sqlStock, cn, tx))
                    {
                        cmd.Parameters.Add("@LibroId", SqlDbType.Int).Value = libroId;
                        await cmd.ExecuteNonQueryAsync();
                    }

                    if (cerrarPrestamo)
                    {
                        using (var cmd = new SqlCommand(sqlEstado, cn, tx))
                        {
                            cmd.Parameters.Add("@PrestamoId", SqlDbType.Int).Value = prestamoId;
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }

                    await tx.CommitAsync();
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            }
        }

        public async Task<int> ContarPendientesPorPrestamoAsync(int prestamoId)
        {
            const string sql =
                "SELECT COUNT(1) FROM DetallePrestamo WHERE PrestamoId = @PrestamoId AND FechaDevolucion IS NULL";

            using (var cn = Conexion.Crear())
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@PrestamoId", SqlDbType.Int).Value = prestamoId;
                await cn.OpenAsync();
                return (int)await cmd.ExecuteScalarAsync();
            }
        }

        public async Task<Prestamo> ObtenerPorIdAsync(int prestamoId)
        {
            const string sql = SelectPrestamos + "WHERE p.PrestamoId = @PrestamoId ORDER BY l.Titulo";

            using (var cn = Conexion.Crear())
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@PrestamoId", SqlDbType.Int).Value = prestamoId;
                await cn.OpenAsync();
                using (var dr = await cmd.ExecuteReaderAsync())
                {
                    var lista = await LeerPrestamosAsync(dr);
                    return lista.Count > 0 ? lista[0] : null;
                }
            }
        }

        public async Task<List<Prestamo>> ListarPendientesPorSocioAsync(int socioId)
        {
            const string sql = SelectPrestamos +
                "WHERE p.SocioId = @SocioId AND d.FechaDevolucion IS NULL ORDER BY p.PrestamoId, l.Titulo";

            using (var cn = Conexion.Crear())
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@SocioId", SqlDbType.Int).Value = socioId;
                await cn.OpenAsync();
                using (var dr = await cmd.ExecuteReaderAsync())
                {
                    return await LeerPrestamosAsync(dr);
                }
            }
        }

        public async Task<List<Prestamo>> ReportePorFechasAsync(DateTime desde, DateTime hasta)
        {
            const string sql = SelectPrestamos +
                "WHERE p.FechaPrestamo >= @Desde AND p.FechaPrestamo <= @Hasta " +
                "ORDER BY p.FechaPrestamo, p.PrestamoId, l.Titulo";

            using (var cn = Conexion.Crear())
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@Desde", SqlDbType.Date).Value = desde;
                cmd.Parameters.Add("@Hasta", SqlDbType.Date).Value = hasta;
                await cn.OpenAsync();
                using (var dr = await cmd.ExecuteReaderAsync())
                {
                    return await LeerPrestamosAsync(dr);
                }
            }
        }

        private static async Task<List<Prestamo>> LeerPrestamosAsync(SqlDataReader dr)
        {
            var lista = new List<Prestamo>();
            var indice = new Dictionary<int, Prestamo>();

            while (await dr.ReadAsync())
            {
                int prestamoId = dr.GetInt32(0);

                if (!indice.TryGetValue(prestamoId, out var prestamo))
                {
                    prestamo = new Prestamo
                    {
                        PrestamoId = prestamoId,
                        SocioId = dr.GetInt32(1),
                        SocioNombre = dr.GetString(2),
                        FechaPrestamo = dr.GetDateTime(3),
                        FechaLimite = dr.GetDateTime(4),
                        Estado = dr.GetString(5)
                    };
                    indice.Add(prestamoId, prestamo);
                    lista.Add(prestamo);
                }

                prestamo.Detalles.Add(new DetallePrestamo
                {
                    PrestamoId = prestamoId,
                    LibroId = dr.GetInt32(6),
                    LibroTitulo = dr.GetString(7),
                    FechaDevolucion = dr.IsDBNull(8) ? (DateTime?)null : dr.GetDateTime(8)
                });
            }

            return lista;
        }
    }
}