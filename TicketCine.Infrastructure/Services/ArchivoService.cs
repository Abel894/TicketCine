using Microsoft.AspNetCore.Http;
using TicketCine.Application.Interfaces;

namespace TicketCine.Infrastructure.Services
{
    public class ArchivoService : IArchivoService
    {
        private readonly string _carpetaBase;
        private readonly string[] _extensionesPermitidas = { ".jpg", ".jpeg", ".png" };
        private const long TamanoMaximoBytes = 5 * 1024 * 1024; // 5 MB
        private const string CarpetaPeliculas = "uploads/peliculas";

        public ArchivoService()
        {
            // Obtener la carpeta wwwroot del contexto actual
            _carpetaBase = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        public async Task<string> GuardarPosterAsync(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
            {
                throw new ArgumentException("El archivo no puede estar vacío.", nameof(archivo));
            }

            // Validar extensión
            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (!_extensionesPermitidas.Contains(extension))
            {
                throw new InvalidOperationException($"El tipo de archivo {extension} no está permitido. Solo se aceptan: .jpg, .jpeg, .png");
            }

            // Validar tamaño
            if (archivo.Length > TamanoMaximoBytes)
            {
                throw new InvalidOperationException($"El archivo excede el tamaño máximo permitido de 5 MB. Tamaño actual: {FormatearTamano(archivo.Length)}");
            }

            // Crear directorio si no existe
            var carpetaAbsoluta = Path.Combine(_carpetaBase, CarpetaPeliculas);
            Directory.CreateDirectory(carpetaAbsoluta);

            // Generar nombre único: Guid + extensión original
            var nombreArchivo = $"{Guid.NewGuid()}{extension}";
            var rutaAbsoluta = Path.Combine(carpetaAbsoluta, nombreArchivo);

            // Guardar archivo
            using (var stream = new FileStream(rutaAbsoluta, FileMode.Create))
            {
                await archivo.CopyToAsync(stream);
            }

            // Retornar ruta relativa para guardar en BD
            return $"/{CarpetaPeliculas}/{nombreArchivo}";
        }

        public void EliminarPoster(string rutaRelativa)
        {
            if (string.IsNullOrWhiteSpace(rutaRelativa))
            {
                return;
            }

            try
            {
                // Eliminar el leading "/" si existe
                var ruta = rutaRelativa.TrimStart('/');
                var rutaAbsoluta = Path.Combine(_carpetaBase, ruta);

                if (File.Exists(rutaAbsoluta))
                {
                    File.Delete(rutaAbsoluta);
                }
            }
            catch (Exception ex)
            {
                // Log del error pero no lanzar excepción para no interrumpir el flujo
                System.Diagnostics.Debug.WriteLine($"Error al eliminar archivo {rutaRelativa}: {ex.Message}");
            }
        }

        private string FormatearTamano(long bytes)
        {
            const long KB = 1024;
            const long MB = KB * 1024;

            if (bytes >= MB)
            {
                return $"{bytes / (double)MB:F2} MB";
            }
            if (bytes >= KB)
            {
                return $"{bytes / (double)KB:F2} KB";
            }
            return $"{bytes} bytes";
        }
    }
}

