using System.Windows;

namespace Biblioteca.WPF
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            rbLibros.IsChecked = true;
            contenido.Content = new LibrosWindow();
        }

        private void btnLibros_Click(object sender, RoutedEventArgs e)
        {
            contenido.Content = new LibrosWindow();
        }

        private void btnSocios_Click(object sender, RoutedEventArgs e)
        {
            contenido.Content = new SociosWindow();
        }

        private void btnPrestamo_Click(object sender, RoutedEventArgs e)
        {
            contenido.Content = new PrestamoWindow();
        }

        private void btnDevolucion_Click(object sender, RoutedEventArgs e)
        {
            contenido.Content = new DevolucionWindow();
        }

        private void btnReporte_Click(object sender, RoutedEventArgs e)
        {
            contenido.Content = new ReporteWindow();
        }
    }
}