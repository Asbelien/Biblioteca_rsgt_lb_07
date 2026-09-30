using System;
using System.Windows;
using System.Windows.Controls;
using Biblioteca.Entidades;
using Biblioteca.Negocio;

namespace Biblioteca.WPF
{
    public partial class LibrosWindow : UserControl
    {
        private readonly LibroNegocio _negocio = new LibroNegocio();
        private int _libroId;

        public LibrosWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                cboAutor.ItemsSource = await _negocio.ListarAutoresAsync();
                await CargarAsync();
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        private async void btnBuscar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await CargarAsync();
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        private void dgLibros_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgLibros.SelectedItem is Libro libro)
            {
                _libroId = libro.LibroId;
                txtTitulo.Text = libro.Titulo;
                txtIsbn.Text = libro.ISBN;
                cboAutor.SelectedValue = libro.AutorId;
                txtEjemplares.Text = libro.Ejemplares.ToString();
            }
        }

        private void btnNuevo_Click(object sender, RoutedEventArgs e)
        {
            Limpiar();
        }

        private async void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtEjemplares.Text.Trim(), out int ejemplares))
            {
                MessageBox.Show("Los ejemplares deben ser un número entero.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var libro = new Libro
            {
                LibroId = _libroId,
                Titulo = txtTitulo.Text,
                ISBN = txtIsbn.Text,
                AutorId = cboAutor.SelectedValue is int autorId ? autorId : 0,
                Ejemplares = ejemplares
            };

            try
            {
                if (_libroId == 0)
                {
                    await _negocio.InsertarAsync(libro);
                    MessageBox.Show("Libro registrado correctamente.", "Libros", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    await _negocio.ActualizarAsync(libro);
                    MessageBox.Show("Libro actualizado correctamente.", "Libros", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                Limpiar();
                await CargarAsync();
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        private async void btnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (_libroId == 0)
            {
                MessageBox.Show("Seleccione un libro de la lista.", "Libros", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var respuesta = MessageBox.Show("¿Desea dar de baja el libro seleccionado?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (respuesta != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                await _negocio.EliminarAsync(_libroId);
                MessageBox.Show("Libro dado de baja correctamente.", "Libros", MessageBoxButton.OK, MessageBoxImage.Information);
                Limpiar();
                await CargarAsync();
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        private async System.Threading.Tasks.Task CargarAsync()
        {
            dgLibros.ItemsSource = await _negocio.BuscarAsync(txtBuscar.Text);
        }

        private void Limpiar()
        {
            _libroId = 0;
            dgLibros.SelectedItem = null;
            txtTitulo.Clear();
            txtIsbn.Clear();
            txtEjemplares.Clear();
            cboAutor.SelectedIndex = -1;
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