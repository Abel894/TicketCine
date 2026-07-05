using System.Transactions;
using TicketCine.Application.DTOs;
using TicketCine.Application.Interfaces;
using TicketCine.Domain.Entities;

namespace TicketCine.Application.Services
{
    public class ReservaService : IReservaService
    {
        private const int MaximoAsientosPorFuncion = 8;
        private readonly IReservaRepository _reservaRepository;
        private readonly IAsientoRepository _asientoRepository;
        private readonly IFuncionRepository _funcionRepository;
        private readonly IUsuarioRepository _usuarioRepository;

        public ReservaService(
            IReservaRepository reservaRepository,
            IAsientoRepository asientoRepository,
            IFuncionRepository funcionRepository,
            IUsuarioRepository usuarioRepository)
        {
            _reservaRepository = reservaRepository ?? throw new ArgumentNullException(nameof(reservaRepository));
            _asientoRepository = asientoRepository ?? throw new ArgumentNullException(nameof(asientoRepository));
            _funcionRepository = funcionRepository ?? throw new ArgumentNullException(nameof(funcionRepository));
            _usuarioRepository = usuarioRepository ?? throw new ArgumentNullException(nameof(usuarioRepository));
        }

        public async Task<ReservaCreadaResponse> CrearReservaAsync(Guid usuarioId, CrearReservaRequest request)
        {
            ValidarUsuarioAutenticado(usuarioId);

            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.FuncionId == Guid.Empty)
                throw new ArgumentException("La función es requerida.", nameof(request.FuncionId));

            var asientoIds = request.AsientoIds?
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList() ?? new List<Guid>();

            if (asientoIds.Count == 0)
                throw new ArgumentException("Debe seleccionar al menos un asiento.", nameof(request.AsientoIds));

            if (asientoIds.Count > MaximoAsientosPorFuncion)
                throw new InvalidOperationException($"No se puede reservar más de {MaximoAsientosPorFuncion} asientos por función.");

            await ExpirarReservasVencidasAsync();

            var usuario = await _usuarioRepository.ObtenerPorIdAsync(usuarioId);
            if (usuario == null || !usuario.Activo)
                throw new UnauthorizedAccessException("Usuario no autenticado o inactivo.");

            var funcion = await _funcionRepository.ObtenerPorIdAsync(request.FuncionId)
                ?? throw new InvalidOperationException("La función no existe.");

            if (funcion.FechaHora <= DateTime.Now)
                throw new InvalidOperationException("No se puede reservar una función que ya inició.");

            var totalReservadosUsuario = await _reservaRepository.ContarAsientosReservadosPorFuncionYUsuarioAsync(request.FuncionId, usuarioId);
            if (totalReservadosUsuario + asientoIds.Count > MaximoAsientosPorFuncion)
                throw new InvalidOperationException($"Se supera el máximo de {MaximoAsientosPorFuncion} asientos por función.");

            var asientos = new List<Asiento>();
            foreach (var asientoId in asientoIds)
            {
                var asiento = await _asientoRepository.ObtenerPorIdAsync(asientoId)
                    ?? throw new InvalidOperationException($"El asiento '{asientoId}' no existe.");

                if (asiento.FuncionId != request.FuncionId)
                    throw new InvalidOperationException("Todos los asientos deben pertenecer a la misma función.");

                if (asiento.Estado != EstadoAsiento.Libre)
                    throw new InvalidOperationException("Uno o más asientos ya no están disponibles.");

                asientos.Add(asiento);
            }

            var fechaCreacion = DateTime.Now;
            var reserva = new Reserva
            {
                Id = Guid.NewGuid(),
                UsuarioId = usuarioId,
                FuncionId = request.FuncionId,
                FechaCreacion = fechaCreacion,
                FechaExpiracion = fechaCreacion.AddMinutes(30),
                Estado = EstadoReserva.Pendiente
            };

            var asientosReserva = asientos.Select(a => new AsientoReserva
            {
                Id = Guid.NewGuid(),
                ReservaId = reserva.Id,
                AsientoId = a.Id
            }).ToList();

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                await _reservaRepository.CrearAsync(reserva);
                await _reservaRepository.CrearAsientosReservaAsync(asientosReserva);

                foreach (var asiento in asientos)
                {
                    asiento.Estado = EstadoAsiento.Reservado;
                    await _asientoRepository.ActualizarAsync(asiento);
                }

                scope.Complete();
            }

            return new ReservaCreadaResponse
            {
                ReservaId = reserva.Id,
                FuncionId = reserva.FuncionId,
                Estado = reserva.Estado,
                FechaCreacion = reserva.FechaCreacion,
                FechaExpiracion = reserva.FechaExpiracion,
                TiempoRestanteSegundos = (int)Math.Max(0, (reserva.FechaExpiracion - DateTime.Now).TotalSeconds),
                CodigoReserva = reserva.Id.ToString("N").ToUpperInvariant(),
                AsientoIds = asientos.Select(a => a.Id).ToList()
            };
        }

        public async Task<Reserva?> ObtenerReservaPorIdAsync(Guid usuarioId, Guid reservaId)
        {
            ValidarUsuarioAutenticado(usuarioId);
            await ExpirarReservasVencidasAsync();

            var reserva = await _reservaRepository.ObtenerPorIdAsync(reservaId);
            if (reserva == null || reserva.UsuarioId != usuarioId)
                return null;

            return reserva;
        }

        public async Task<IEnumerable<Reserva>> ObtenerReservasActivasAsync(Guid usuarioId)
        {
            ValidarUsuarioAutenticado(usuarioId);
            await ExpirarReservasVencidasAsync();
            return await _reservaRepository.ObtenerActivasPorUsuarioAsync(usuarioId);
        }

        public async Task<IEnumerable<Reserva>> ObtenerReservasExpiradasAsync(Guid usuarioId)
        {
            ValidarUsuarioAutenticado(usuarioId);
            await ExpirarReservasVencidasAsync();
            return await _reservaRepository.ObtenerExpiradasPorUsuarioAsync(usuarioId);
        }

        public async Task<int> ExpirarReservasVencidasAsync()
        {
            var reservasVencidas = (await _reservaRepository.ObtenerPendientesVencidasAsync(DateTime.Now)).ToList();
            if (reservasVencidas.Count == 0)
                return 0;

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                foreach (var reserva in reservasVencidas)
                {
                    var asientosReserva = await _reservaRepository.ObtenerAsientosPorReservaAsync(reserva.Id);

                    foreach (var asientoReserva in asientosReserva)
                    {
                        var asiento = await _asientoRepository.ObtenerPorIdAsync(asientoReserva.AsientoId);
                        if (asiento != null && asiento.Estado == EstadoAsiento.Reservado)
                        {
                            asiento.Estado = EstadoAsiento.Libre;
                            await _asientoRepository.ActualizarAsync(asiento);
                        }
                    }

                    reserva.Estado = EstadoReserva.Expirada;
                    await _reservaRepository.ActualizarAsync(reserva);
                }

                scope.Complete();
            }

            return reservasVencidas.Count;
        }

        public async Task CancelarAsync(Guid usuarioId, Guid reservaId)
        {
            ValidarUsuarioAutenticado(usuarioId);
            await ExpirarReservasVencidasAsync();

            var reserva = await _reservaRepository.ObtenerPorIdAsync(reservaId)
                ?? throw new InvalidOperationException("La reserva no existe.");

            if (reserva.UsuarioId != usuarioId)
                throw new UnauthorizedAccessException("La reserva no pertenece al usuario autenticado.");

            if (reserva.Estado != EstadoReserva.Pendiente)
                throw new InvalidOperationException("Solo se pueden cancelar reservas en estado Pendiente.");

            var funcion = await _funcionRepository.ObtenerPorIdAsync(reserva.FuncionId)
                ?? throw new InvalidOperationException("La función asociada no existe.");

            if (funcion.FechaHora <= DateTime.Now.AddHours(1))
                throw new InvalidOperationException("No se puede cancelar la reserva: falta menos de 1 hora para el inicio de la función.");

            var asientosReserva = (await _reservaRepository.ObtenerAsientosPorReservaAsync(reserva.Id)).ToList();

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                foreach (var asientoReserva in asientosReserva)
                {
                    var asiento = await _asientoRepository.ObtenerPorIdAsync(asientoReserva.AsientoId)
                        ?? throw new InvalidOperationException("Uno de los asientos de la reserva no existe.");

                    if (asiento.Estado == EstadoAsiento.Vendido)
                        throw new InvalidOperationException("No se puede cancelar una reserva con asientos vendidos.");

                    asiento.Estado = EstadoAsiento.Libre;
                    await _asientoRepository.ActualizarAsync(asiento);
                }

                reserva.Estado = EstadoReserva.Cancelada;
                await _reservaRepository.ActualizarAsync(reserva);

                scope.Complete();
            }
        }

        private static void ValidarUsuarioAutenticado(Guid usuarioId)
        {
            if (usuarioId == Guid.Empty)
                throw new UnauthorizedAccessException("Usuario no autenticado.");
        }
    }
}
