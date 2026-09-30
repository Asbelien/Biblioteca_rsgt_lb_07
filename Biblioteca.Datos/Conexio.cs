using System.Configuration;
using Microsoft.Data.SqlClient;

namespace Biblioteca.Datos
{
    internal static class Conexion
    {
        public static SqlConnection Crear()
        {
            string cadena = ConfigurationManager.ConnectionStrings["BibliotecaDB"].ConnectionString;
            return new SqlConnection(cadena);
        }
    }
}