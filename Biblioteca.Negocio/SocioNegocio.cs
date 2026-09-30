using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Biblioteca.Datos;
using Biblioteca.Entidades;

namespace Biblioteca.Negocio
{
    public class SocioNegocio
    {
        private readonly SocioDatos _socioDatos = new SocioDatos();

        public Task<List<Socio>> BuscarAsync(string filtro)
        {
            return _socioDatos.BuscarActivosAsync(filtro);
        }

        public async Task<int> InsertarAsync(Socio socio)
        {
            await ValidarAsync(socio, 0);
            return await _socioDatos.InsertarAsync(socio);
        }

        public async Task ActualizarAsync(Socio socio)
        {
            var existente = await _socioDatos.ObtenerPorIdAsync(socio.SocioId);
            if (existente == null || !existente.Activo)
            {
                throw new ReglaNegocioException("El socio no existe o está dado de baja.");
            }

            await ValidarAsync(socio, socio.SocioId);
            await _socioDatos.ActualizarAsync(socio);
        }

        public async Task EliminarAsync(int socioId)
        {
            var socio = await _socioDatos.ObtenerPorIdAsync(socioId);
            if (socio == null)
            {
                throw new ReglaNegocioException("El socio no existe.");
            }

            if (!socio.Activo)
            {
                throw new ReglaNegocioException("El socio ya está dado de baja.");
            }

            int pendientes = await _socioDatos.ContarPendientesAsync(socioId);
            if (pendientes > 0)
            {
                throw new ReglaNegocioException("No se puede dar de baja al socio porque tiene " + pendientes + " libro(s) pendiente(s) de devolución.");
            }

            await _socioDatos.DesactivarAsync(socioId);
        }

        private async Task ValidarAsync(Socio socio, int socioIdExcluido)
        {
            if (socio == null)
            {
                throw new ReglaNegocioException("Los datos del socio son obligatorios.");
            }

            socio.DNI = (socio.DNI ?? string.Empty).Trim();
            socio.Nombre = (socio.Nombre ?? string.Empty).Trim();
            socio.Email = (socio.Email ?? string.Empty).Trim();

            if (socio.DNI.Length != 8 || !EsNumerico(socio.DNI))
            {
                throw new ReglaNegocioException("El DNI debe tener exactamente 8 dígitos.");
            }

            if (socio.Nombre.Length == 0)
            {
                throw new ReglaNegocioException("El nombre es obligatorio.");
            }

            if (socio.Email.Length == 0 || !socio.Email.Contains("@") || socio.Email.StartsWith("@") || socio.Email.EndsWith("@"))
            {
                throw new ReglaNegocioException("Debe ingresar un email válido.");
            }

            if (await _socioDatos.ExisteDniAsync(socio.DNI, socioIdExcluido))
            {
                throw new ReglaNegocioException("Ya existe un socio con el DNI " + socio.DNI + ".");
            }
        }

        private static bool EsNumerico(string texto)
        {
            foreach (char c in texto)
            {
                if (!char.IsDigit(c))
                {
                    return false;
                }
            }

            return true;
        }
    }
}