using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Biblioteca.Negocio;

namespace Biblioteca.WPF
{
    public class DevolucionItem
    {
        public int PrestamoId { get; set; }
        public int LibroId { get; set; }
        public string Titulo { get; set; }
        public DateTime FechaPrestamo { get; set; }
        public DateTime FechaLimite { get; set; }
    }

    public partial class DevolucionWindow : UserControl
    {
        private readonly SocioNegocio _socios = new SocioNegocio();
        private readonly PrestamoNegocio _prestamos = new PrestamoNegocio();

        public DevolucionWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                cboSocio.ItemsSource = await _socios.BuscarAsync(string.Empty);
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        private async void cboSocio_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                txtResultado.Text = "Seleccione un libro pendiente y presione Devolver.";
                txtMulta.Text = string.Empty;
                await CargarPendientesAsync();
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        private async void btnDevolver_Click(object sender, RoutedEventArgs e)
        {
            if (!(dgPendientes.SelectedItem is DevolucionItem item))
            {
                MessageBox.Show("Seleccione un libro pendiente de la lista.", "Devolución", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var resultado = await _prestamos.DevolverAsync(item.PrestamoId, item.LibroId);

                await CargarPendientesAsync();

                string mensaje = "Libro \"" + item.Titulo + "\" devuelto correctamente. Días de retraso: " + resultado.DiasRetraso + ".";
                if (resultado.PrestamoCerrado)
                {
                    mensaje += " El préstamo N° " + item.PrestamoId + " pasó a Devuelto.";
                }

                txtResultado.Text = mensaje;
                txtMulta.Text = "Multa: S/ " + resultado.Multa.ToString("N2");
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        private async Task CargarPendientesAsync()
        {
            if (!(cboSocio.SelectedValue is int socioId))
            {
                dgPendientes.ItemsSource = null;
                return;
            }

            var prestamos = await _prestamos.ListarPendientesPorSocioAsync(socioId);

            dgPendientes.ItemsSource = prestamos
                .SelectMany(p => p.Detalles.Select(d => new DevolucionItem
                {
                    PrestamoId = p.PrestamoId,
                    LibroId = d.LibroId,
                    Titulo = d.LibroTitulo,
                    FechaPrestamo = p.FechaPrestamo,
                    FechaLimite = p.FechaLimite
                }))
                .ToList();
        }

        private void MostrarError(Exception ex)
        {
            if (ex is ReglaNegocioException)
            {
                MessageBox.Show(ex.Message, "Regla de negocio", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show("Ocurrió un error inesperado: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}