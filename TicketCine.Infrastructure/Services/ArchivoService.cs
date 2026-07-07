using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using TicketCine.Application.Interfaces;

namespace TicketCine.Infrastructure.Services
{
    public class ArchivoService : IArchivoService
    {
        private readonly Cloudinary _cloudinary;
        private readonly string[] _extensionesPermitidas = { ".jpg", ".jpeg", ".png" };
        private const long TamanoMaximoBytes = 5 * 1024 * 1024; // 5 MB
        private const string CarpetaPeliculas = "ticketcine/peliculas";

        public ArchivoService(IConfiguration configuration)
        {
            var cloudName = configuration["Cloudinary:CloudName"];
            var apiKey = configuration["Cloudinary:ApiKey"];
            var apiSecret = configuration["Cloudinary:ApiSecret"];

            if (string.IsNullOrWhiteSpace(cloudName) ||
                string.IsNullOrWhiteSpace(apiKey) ||
                string.IsNullOrWhiteSpace(apiSecret))
            {
                throw new InvalidOperationException("La configuración de Cloudinary es obligatoria. Complete Cloudinary:CloudName, Cloudinary:ApiKey y Cloudinary:ApiSecret.");
            }

            _cloudinary = new Cloudinary(new Account(cloudName, apiKey, apiSecret));
        }

        public async Task<string> GuardarPosterAsync(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
            {
                throw new ArgumentException("El archivo no puede estar vacío.", nameof(archivo));
            }

            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (!_extensionesPermitidas.Contains(extension))
            {
                throw new InvalidOperationException($"El tipo de archivo {extension} no está permitido. Solo se aceptan: .jpg, .jpeg, .png");
            }

            if (archivo.Length > TamanoMaximoBytes)
            {
                throw new InvalidOperationException($"El archivo excede el tamaño máximo permitido de 5 MB. Tamaño actual: {FormatearTamano(archivo.Length)}");
            }

            await using var stream = archivo.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(archivo.FileName, stream),
                Folder = CarpetaPeliculas,
                PublicId = Guid.NewGuid().ToString(),
                Overwrite = false,
                UseFilename = false,
                UniqueFilename = false
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error is not null)
            {
                throw new InvalidOperationException($"No se pudo subir la imagen a Cloudinary: {uploadResult.Error.Message}");
            }

            var urlSegura = uploadResult.SecureUrl?.ToString();
            if (string.IsNullOrWhiteSpace(urlSegura))
            {
                throw new InvalidOperationException("Cloudinary no devolvió una URL segura para la imagen.");
            }

            return urlSegura;
        }

        public void EliminarPoster(string rutaRelativa)
        {
            if (string.IsNullOrWhiteSpace(rutaRelativa))
            {
                return;
            }

            try
            {
                var publicId = ObtenerPublicIdDesdeRuta(rutaRelativa);
                if (string.IsNullOrWhiteSpace(publicId))
                {
                    return;
                }

                var resultado = _cloudinary.Destroy(new DeletionParams(publicId)
                {
                    ResourceType = ResourceType.Image
                });

                if (resultado.Error is not null)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al eliminar imagen en Cloudinary {rutaRelativa}: {resultado.Error.Message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al eliminar archivo {rutaRelativa}: {ex.Message}");
            }
        }

        private static string? ObtenerPublicIdDesdeRuta(string rutaPoster)
        {
            if (!Uri.TryCreate(rutaPoster, UriKind.Absolute, out var uri))
            {
                return null;
            }

            var segmentos = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var segmentosCarpeta = CarpetaPeliculas.Split('/', StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i <= segmentos.Length - segmentosCarpeta.Length; i++)
            {
                var coincide = true;

                for (var j = 0; j < segmentosCarpeta.Length; j++)
                {
                    if (!string.Equals(segmentos[i + j], segmentosCarpeta[j], StringComparison.OrdinalIgnoreCase))
                    {
                        coincide = false;
                        break;
                    }
                }

                if (!coincide)
                {
                    continue;
                }

                var publicIdConExtension = string.Join("/", segmentos[i..]);
                var indiceExtension = publicIdConExtension.LastIndexOf('.');

                return indiceExtension >= 0
                    ? publicIdConExtension[..indiceExtension]
                    : publicIdConExtension;
            }

            return null;
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