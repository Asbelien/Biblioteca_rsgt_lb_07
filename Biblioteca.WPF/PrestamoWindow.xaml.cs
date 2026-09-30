using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Biblioteca.Entidades;
using Biblioteca.Negocio;

namespace Biblioteca.WPF
{
    public partial class PrestamoWindow : UserControl
    {
        private readonly SocioNegocio _socios = new SocioNegocio();
        private readonly LibroNegocio _libros = new LibroNegocio();
        private readonly PrestamoNegocio _prestamos = new PrestamoNegocio();
        private readonly ObservableCollection<Libro> _seleccion = new ObservableCollection<Libro>();

        public PrestamoWindow()
        {
            InitializeComponent();
            dgSeleccion.ItemsSource = _seleccion;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                cboSocio.ItemsSource = await _socios.BuscarAsync(string.Empty);
                await CargarLibrosAsync();
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
                await ActualizarPendientesAsync();
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        private void btnAgregar_Click(object sender, RoutedEventArgs e)
        {
            if (!(cboLibro.SelectedItem is Libro libro))
            {
                MessageBox.Show("Seleccione un libro.", "Préstamo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_seleccion.Any(l => l.LibroId == libro.LibroId))
            {
                MessageBox.Show("Ese libro ya está en la lista.", "Préstamo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _seleccion.Add(libro);
        }

        private void btnQuitar_Click(object sender, RoutedEventArgs e)
        {
            if (dgSeleccion.SelectedItem is Libro libro)
            {
                _seleccion.Remove(libro);
            }
            else
            {
                MessageBox.Show("Seleccione un libro de la lista.", "Préstamo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void btnRegistrar_Click(object sender, RoutedEventArgs e)
        {
            int socioId = cboSocio.SelectedValue is int id ? id : 0;
            var libroIds = _seleccion.Select(l => l.LibroId).ToList();

            try
            {
                int prestamoId = await _prestamos.RegistrarAsync(socioId, libroIds);
                MessageBox.Show("Préstamo N° " + prestamoId + " registrado correctamente.", "Préstamo", MessageBoxButton.OK, MessageBoxImage.Information);

                _seleccion.Clear();
                await CargarLibrosAsync();
                await ActualizarPendientesAsync();
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        private async Task CargarLibrosAsync()
        {
            cboLibro.ItemsSource = await _libros.BuscarAsync(string.Empty);
        }

        private async Task ActualizarPendientesAsync()
        {
            if (!(cboSocio.SelectedValue is int socioId))
            {
                txtPendientes.Text = string.Empty;
                return;
            }

            var pendientes = await _prestamos.ListarPendientesPorSocioAsync(socioId);
            int total = pendientes.Sum(p => p.Detalles.Count);
            txtPendientes.Text = "Libros pendientes de devolución: " + total;
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