using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Biblioteca.Negocio;

namespace Biblioteca.WPF
{
    public class ReporteItem
    {
        public int PrestamoId { get; set; }
        public string Socio { get; set; }
        public string Libros { get; set; }
        public DateTime FechaPrestamo { get; set; }
        public DateTime FechaLimite { get; set; }
        public string Estado { get; set; }
    }

    public partial class ReporteWindow : UserControl
    {
        private readonly PrestamoNegocio _prestamos = new PrestamoNegocio();

        public ReporteWindow()
        {
            InitializeComponent();
            dpDesde.SelectedDate = DateTime.Today.AddMonths(-2);
            dpHasta.SelectedDate = DateTime.Today;
        }

        private async void btnConsultar_Click(object sender, RoutedEventArgs e)
        {
            if (!dpDesde.SelectedDate.HasValue || !dpHasta.SelectedDate.HasValue)
            {
                MessageBox.Show("Seleccione la fecha inicial y la fecha final.", "Reporte", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var prestamos = await _prestamos.ReporteAsync(dpDesde.SelectedDate.Value, dpHasta.SelectedDate.Value);

                var items = prestamos
                    .Select(p => new ReporteItem
                    {
                        PrestamoId = p.PrestamoId,
                        Socio = p.SocioNombre,
                        Libros = string.Join(", ", p.Detalles.Select(d => d.LibroTitulo)),
                        FechaPrestamo = p.FechaPrestamo,
                        FechaLimite = p.FechaLimite,
                        Estado = p.Estado
                    })
                    .ToList();

                dgReporte.ItemsSource = items;
                txtTotal.Text = "Total de préstamos: " + items.Count;
            }
            catch (Exception ex)
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
}