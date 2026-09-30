using System.Collections.Generic;
using System.Threading.Tasks;
using Biblioteca.Datos;
using Biblioteca.Entidades;

namespace Biblioteca.Negocio
{
    public class LibroNegocio
    {
        private readonly LibroDatos _libroDatos = new LibroDatos();
        private readonly AutorDatos _autorDatos = new AutorDatos();

        public Task<List<Libro>> BuscarAsync(string filtro)
        {
            return _libroDatos.BuscarActivosAsync(filtro);
        }

        public Task<List<Autor>> ListarAutoresAsync()
        {
            return _autorDatos.ListarActivosAsync();
        }

        public async Task<int> InsertarAsync(Libro libro)
        {
            await ValidarAsync(libro, 0);
            return await _libroDatos.InsertarAsync(libro);
        }

        public async Task ActualizarAsync(Libro libro)
        {
            var existente = await _libroDatos.ObtenerPorIdAsync(libro.LibroId);
            if (existente == null || !existente.Activo)
            {
                throw new ReglaNegocioException("El libro no existe o está dado de baja.");
            }

            await ValidarAsync(libro, libro.LibroId);
            await _libroDatos.ActualizarAsync(libro);
        }

        public async Task EliminarAsync(int libroId)
        {
            var libro = await _libroDatos.ObtenerPorIdAsync(libroId);
            if (libro == null)
            {
                throw new ReglaNegocioException("El libro no existe.");
            }

            if (!libro.Activo)
            {
                throw new ReglaNegocioException("El libro ya está dado de baja.");
            }

            int pendientes = await _libroDatos.ContarPendientesAsync(libroId);
            if (pendientes > 0)
            {
                throw new ReglaNegocioException("No se puede dar de baja el libro porque tiene " + pendientes + " préstamo(s) pendiente(s).");
            }

            await _libroDatos.DesactivarAsync(libroId);
        }

        private async Task ValidarAsync(Libro libro, int libroIdExcluido)
        {
            if (libro == null)
            {
                throw new ReglaNegocioException("Los datos del libro son obligatorios.");
            }

            libro.Titulo = (libro.Titulo ?? string.Empty).Trim();
            libro.ISBN = (libro.ISBN ?? string.Empty).Trim();

            if (libro.Titulo.Length == 0)
            {
                throw new ReglaNegocioException("El título es obligatorio.");
            }

            if (libro.ISBN.Length == 0)
            {
                throw new ReglaNegocioException("El ISBN es obligatorio.");
            }

            if (libro.ISBN.Length > 20)
            {
                throw new ReglaNegocioException("El ISBN no puede tener más de 20 caracteres.");
            }

            if (libro.Ejemplares < 0)
            {
                throw new ReglaNegocioException("Los ejemplares no pueden ser negativos.");
            }

            var autor = await _autorDatos.ObtenerPorIdAsync(libro.AutorId);
            if (autor == null || !autor.Activo)
            {
                throw new ReglaNegocioException("Debe seleccionar un autor válido.");
            }

            if (await _libroDatos.ExisteIsbnAsync(libro.ISBN, libroIdExcluido))
            {
                throw new ReglaNegocioException("Ya existe un libro con el ISBN " + libro.ISBN + ".");
            }
        }
    }
}