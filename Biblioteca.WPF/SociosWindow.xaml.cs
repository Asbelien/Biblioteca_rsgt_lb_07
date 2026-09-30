using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Biblioteca.Entidades;
using Biblioteca.Negocio;

namespace Biblioteca.WPF
{
    public partial class SociosWindow : UserControl
    {
        private readonly SocioNegocio _negocio = new SocioNegocio();
        private int _socioId;

        public SociosWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
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

        private void dgSocios_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgSocios.SelectedItem is Socio socio)
            {
                _socioId = socio.SocioId;
                txtDni.Text = socio.DNI;
                txtNombre.Text = socio.Nombre;
                txtEmail.Text = socio.Email;
            }
        }

        private void btnNuevo_Click(object sender, RoutedEventArgs e)
        {
            Limpiar();
        }

        private async void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            var socio = new Socio
            {
                SocioId = _socioId,
                DNI = txtDni.Text,
                Nombre = txtNombre.Text,
                Email = txtEmail.Text
            };

            try
            {
                if (_socioId == 0)
                {
                    await _negocio.InsertarAsync(socio);
                    MessageBox.Show("Socio registrado correctamente.", "Socios", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    await _negocio.ActualizarAsync(socio);
                    MessageBox.Show("Socio actualizado correctamente.", "Socios", MessageBoxButton.OK, MessageBoxImage.Information);
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
            if (_socioId == 0)
            {
                MessageBox.Show("Seleccione un socio de la lista.", "Socios", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var respuesta = MessageBox.Show("¿Desea dar de baja al socio seleccionado?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (respuesta != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                await _negocio.EliminarAsync(_socioId);
                MessageBox.Show("Socio dado de baja correctamente.", "Socios", MessageBoxButton.OK, MessageBoxImage.Information);
                Limpiar();
                await CargarAsync();
            }
            catch (Exception ex)
            {
                MostrarError(ex);
            }
        }

        private async Task CargarAsync()
        {
            dgSocios.ItemsSource = await _negocio.BuscarAsync(txtBuscar.Text);
        }

        private void Limpiar()
        {
            _socioId = 0;
            dgSocios.SelectedItem = null;
            txtDni.Clear();
            txtNombre.Clear();
            txtEmail.Clear();
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