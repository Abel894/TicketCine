using Microsoft.AspNetCore.Mvc;
using TicketCine.Application.DTOs;
using TicketCine.Application.Interfaces;

namespace TicketCine.Web.Controllers
{
    public class CatalogoController : Controller
    {
        private readonly IPeliculaRepository _peliculaRepository;
        private readonly IFuncionRepository _funcionRepository;
        private readonly IAsientoRepository _asientoRepository;
        private readonly ILogger<CatalogoController> _logger;

        public CatalogoController(
            IPeliculaRepository peliculaRepository,
            IFuncionRepository funcionRepository,
            IAsientoRepository asientoRepository,
            ILogger<CatalogoController> logger)
        {
            _peliculaRepository = peliculaRepository ?? throw new ArgumentNullException(nameof(peliculaRepository));
            _funcionRepository = funcionRepository ?? throw new ArgumentNullException(nameof(funcionRepository));
            _asientoRepository = asientoRepository ?? throw new ArgumentNullException(nameof(asientoRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// GET: /Catalogo - Muestra la cartelera de películas con funciones activas
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                var peliculas = await _peliculaRepository.ObtenerConFuncionesActivasAsync();
                var carteleraItems = peliculas.Select(p => new CarteleraItemResponse
                {
                    Id = p.Id,
                    Titulo = p.Titulo,
                    Sinopsis = p.Sinopsis,
                    Genero = p.Genero,
                    DuracionMinutos = p.DuracionMinutos,
                    Clasificacion = p.Clasificacion,
                    RutaPoster = p.RutaPoster
                }).ToList();

                return View(carteleraItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener cartelera");
                ModelState.AddModelError(string.Empty, "Error al cargar la cartelera.");
                return View(new List<CarteleraItemResponse>());
            }
        }

        /// <summary>
        /// GET: /Catalogo/Funciones/{peliculaId} - Muestra las funciones disponibles de una película
        /// </summary>
        public async Task<IActionResult> Funciones(Guid peliculaId)
        {
            try
            {
                var pelicula = await _peliculaRepository.ObtenerPorIdAsync(peliculaId);
                if (pelicula == null)
                {
                    return NotFound("Película no encontrada");
                }

                var funciones = await _funcionRepository.ObtenerPorPeliculaActivasAsync(peliculaId);
                var funcionesOrdenadas = funciones
                    .OrderBy(f => f.FechaHora)
                    .ToList();

                var funcionesDetalle = new List<FuncionDetalleResponse>();
                foreach (var funcion in funcionesOrdenadas)
                {
                    var asientosLibres = await _asientoRepository.ContarLibresPorFuncionAsync(funcion.Id);
                    var totalAsientos = funcion.Sala.Filas * funcion.Sala.Columnas;

                    funcionesDetalle.Add(new FuncionDetalleResponse
                    {
                        Id = funcion.Id,
                        PeliculaId = funcion.PeliculaId,
                        TituloPelicula = pelicula.Titulo,
                        SalaId = funcion.SalaId,
                        NombreSala = funcion.Sala.Nombre,
                        FilasSala = funcion.Sala.Filas,
                        ColumnaSala = funcion.Sala.Columnas,
                        FechaHora = funcion.FechaHora,
                        Precio = funcion.Precio,
                        AsientosDisponibles = asientosLibres,
                        TotalAsientos = totalAsientos
                    });
                }

                ViewBag.PeliculaId = peliculaId;
                ViewBag.TituloPelicula = pelicula.Titulo;
                return View(funcionesDetalle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener funciones para película {PeliculaId}", peliculaId);
                ModelState.AddModelError(string.Empty, "Error al cargar las funciones.");
                return View(new List<FuncionDetalleResponse>());
            }
        }

        /// <summary>
        /// GET: /Catalogo/Asientos/{funcionId} - Muestra el mapa visual de asientos de una función
        /// </summary>
        public async Task<IActionResult> Asientos(Guid funcionId)
        {
            try
            {
                var funcion = await _funcionRepository.ObtenerPorIdAsync(funcionId);
                if (funcion == null)
                {
                    return NotFound("Función no encontrada");
                }

                var asientos = await _asientoRepository.ObtenerPorFuncionAsync(funcionId);

                var mapaSala = new MapaSalaResponse
                {
                    FuncionId = funcion.Id,
                    TituloPelicula = funcion.Pelicula.Titulo,
                    NombreSala = funcion.Sala.Nombre,
                    FechaHora = funcion.FechaHora,
                    Precio = funcion.Precio,
                    Filas = funcion.Sala.Filas,
                    Columnas = funcion.Sala.Columnas,
                    Asientos = asientos.Select(a => new AsientoMapaResponse
                    {
                        Id = a.Id,
                        FuncionId = a.FuncionId,
                        Fila = a.Fila,
                        Columna = a.Columna,
                        Estado = a.Estado.ToString()
                    }).ToList()
                };

                return View(mapaSala);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener mapa de asientos para función {FuncionId}", funcionId);
                ModelState.AddModelError(string.Empty, "Error al cargar el mapa de asientos.");
                return View();
            }
        }
    }
}
