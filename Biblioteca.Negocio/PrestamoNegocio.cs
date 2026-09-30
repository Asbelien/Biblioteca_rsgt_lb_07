using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Biblioteca.Datos;
using Biblioteca.Entidades;

namespace Biblioteca.Negocio
{
    public class ResultadoDevolucion
    {
        public int DiasRetraso { get; set; }
        public decimal Multa { get; set; }
        public bool PrestamoCerrado { get; set; }
    }

    public class PrestamoNegocio
    {
        private const int MaxLibrosPendientes = 3;
        private const int DiasPrestamo = 14;
        private const decimal MultaPorDia = 1.50m;

        private readonly PrestamoDatos _prestamoDatos = new PrestamoDatos();
        private readonly SocioDatos _socioDatos = new SocioDatos();
        private readonly LibroDatos _libroDatos = new LibroDatos();

        public async Task<int> RegistrarAsync(int socioId, List<int> libroIds)
        {
            if (libroIds == null || libroIds.Count == 0)
            {
                throw new ReglaNegocioException("Debe agregar al menos un libro al préstamo.");
            }

            if (libroIds.Distinct().Count() != libroIds.Count)
            {
                throw new ReglaNegocioException("No se puede repetir un libro en el mismo préstamo.");
            }

            var socio = await _socioDatos.ObtenerPorIdAsync(socioId);
            if (socio == null || !socio.Activo)
            {
                throw new ReglaNegocioException("El socio no existe o está dado de baja.");
            }

            int pendientes = await _socioDatos.ContarPendientesAsync(socioId);
            if (pendientes + libroIds.Count > MaxLibrosPendientes)
            {
                throw new ReglaNegocioException("Un socio no puede tener más de " + MaxLibrosPendientes +
                    " libros pendientes. Tiene " + pendientes + " y quiere llevar " + libroIds.Count + ".");
            }

            var prestamo = new Prestamo
            {
                SocioId = socioId,
                FechaPrestamo = DateTime.Today,
                FechaLimite = DateTime.Today.AddDays(DiasPrestamo),
                Estado = "Pendiente"
            };

            foreach (int libroId in libroIds)
            {
                var libro = await _libroDatos.ObtenerPorIdAsync(libroId);
                if (libro == null || !libro.Activo)
                {
                    throw new ReglaNegocioException("El libro seleccionado no existe o está dado de baja.");
                }

                if (libro.Ejemplares <= 0)
                {
                    throw new ReglaNegocioException("El libro \"" + libro.Titulo + "\" no tiene ejemplares disponibles.");
                }

                prestamo.Detalles.Add(new DetallePrestamo { LibroId = libroId });
            }

            try
            {
                return await _prestamoDatos.RegistrarAsync(prestamo);
            }
            catch (InvalidOperationException ex)
            {
                throw new ReglaNegocioException(ex.Message);
            }
        }

        public async Task<ResultadoDevolucion> DevolverAsync(int prestamoId, int libroId)
        {
            var prestamo = await _prestamoDatos.ObtenerPorIdAsync(prestamoId);
            if (prestamo == null)
            {
                throw new ReglaNegocioException("El préstamo no existe.");
            }

            var detalle = prestamo.Detalles.FirstOrDefault(d => d.LibroId == libroId);
            if (detalle == null)
            {
                throw new ReglaNegocioException("El libro no pertenece a este préstamo.");
            }

            if (detalle.FechaDevolucion.HasValue)
            {
                throw new ReglaNegocioException("Este libro ya fue devuelto.");
            }

            DateTime hoy = DateTime.Today;
            int pendientes = prestamo.Detalles.Count(d => !d.FechaDevolucion.HasValue);
            bool cerrar = pendientes == 1;

            try
            {
                await _prestamoDatos.DevolverLibroAsync(prestamoId, libroId, hoy, cerrar);
            }
            catch (InvalidOperationException ex)
            {
                throw new ReglaNegocioException(ex.Message);
            }

            int diasRetraso = Math.Max(0, (hoy - prestamo.FechaLimite.Date).Days);

            return new ResultadoDevolucion
            {
                DiasRetraso = diasRetraso,
                Multa = diasRetraso * MultaPorDia,
                PrestamoCerrado = cerrar
            };
        }

        public Task<List<Prestamo>> ListarPendientesPorSocioAsync(int socioId)
        {
            return _prestamoDatos.ListarPendientesPorSocioAsync(socioId);
        }

        public async Task<List<Prestamo>> ReporteAsync(DateTime desde, DateTime hasta)
        {
            if (hasta.Date < desde.Date)
            {
                throw new ReglaNegocioException("La fecha final no puede ser menor que la fecha inicial.");
            }

            return await _prestamoDatos.ReportePorFechasAsync(desde.Date, hasta.Date);
        }
    }
}