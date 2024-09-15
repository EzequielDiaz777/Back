using Back.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Back.Controllers
{
	[Route("[controller]")]
	[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
	public class TurnosController : ControllerBase
	{
		private readonly DataContext contexto;

		public TurnosController(DataContext contexto)
		{
			this.contexto = contexto;
		}

		[HttpGet]
		public async Task<ActionResult<IEnumerable<Turno>>> Get()
		{
			try
			{
				var turno = await contexto.Turno
				.Include(t => t.Paciente)
				.Include(t => t.TipoDeVacuna)
				.Include(t => t.Tutor)
				.Include(t => t.Agente)
				.Include(t => t.Aplicacion)
					.ThenInclude(a => a.LoteProveedor)
						.ThenInclude(l => l.Laboratorio)
				.ToListAsync();
				return Ok(turno);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[HttpGet("{id}")]
		public async Task<ActionResult<Turno>> Get(int id)
		{
			try
			{
				var turno = await contexto.Turno
					.Include(t => t.Paciente)
					.Include(t => t.TipoDeVacuna)
					.Include(t => t.Tutor)
					.Include(t => t.Agente)
					.Include(t => t.Aplicacion)
						.ThenInclude(a => a.LoteProveedor)
							.ThenInclude(l => l.Laboratorio)
					.SingleOrDefaultAsync(t => t.Id == id);

				if (turno == null)
				{
					return NotFound();
				}

				return Ok(turno);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}


		[HttpPost]
		public async Task<IActionResult> Post([FromForm] Turno turno)
		{
			try
			{
				if (ModelState.IsValid)
				{
					// 1) Verificar si existen vacunas disponibles del tipo especificado
					var loteP = await contexto.LoteProveedor
						.Where(l => l.TipoDeVacunaId == turno.TipoDeVacunaId && l.CantidadDeVacunas > 0)
						.FirstOrDefaultAsync();

					if (loteP == null)
					{
						return BadRequest("No hay vacunas disponibles para el tipo de vacuna seleccionado.");
					}

					// 2) Determinar la dosis correcta para el paciente
					var maxDosis = await contexto.Turno
						.Where(t => t.PacienteId == turno.PacienteId && t.AplicacionId != null)
						.Join(contexto.Aplicacion,
							t => t.AplicacionId,
							a => a.Id,
							(t, a) => a.Dosis)
						.ToListAsync(); // Ejecutar la consulta en la base de datos y traer los datos a la memoria

					var dosis = maxDosis.Any() ? maxDosis.Max() : 0; // Determinar la dosis máxima o usar 0 si no hay registros


					// 3) Crear la aplicación correspondiente
					var matricula = int.Parse(User.FindFirstValue("Matricula"));
					var aplicacion = new Aplicacion
					{
						LoteProveedorId = loteP.Id,
						AgenteId = 0,
						Dosis = dosis+1,
						Estado = Estado.Pendiente
					};

					contexto.Aplicacion.Add(aplicacion);
					await contexto.SaveChangesAsync();

					// 4) Asignar el ID de la aplicación al turno y guardar el turno
					turno.AplicacionId = aplicacion.Id;

					contexto.Turno.Add(turno);
					await contexto.SaveChangesAsync();

					return CreatedAtAction(nameof(Get), new { id = turno.Id }, turno);
				}

				return BadRequest(ModelState);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
		}

		[HttpPut]
		public async Task<IActionResult> Put([FromForm] Turno turno)
		{
			try
			{
				if (ModelState.IsValid)
				{
					var loteP = await contexto.LoteProveedor
						.Where(l => l.TipoDeVacunaId == turno.TipoDeVacunaId && l.CantidadDeVacunas > 0)
						.FirstOrDefaultAsync();

					if (loteP == null)
					{
						return BadRequest("No hay vacunas disponibles para el tipo de vacuna seleccionado.");
					}

					// 2) Determinar la dosis correcta para el paciente
					var maxDosis = await contexto.Turno
						.Where(t => t.PacienteId == turno.PacienteId && t.AplicacionId != null)
						.Join(contexto.Aplicacion,
							t => t.AplicacionId,
							a => a.Id,
							(t, a) => a.Dosis)
						.ToListAsync(); // Ejecutar la consulta en la base de datos y traer los datos a la memoria

					var dosis = maxDosis.Any() ? maxDosis.Max() : 0; // Determinar la dosis máxima o usar 0 si no hay registros

					// 3) Actualizar el estado de la aplicación directamente con SQL crudo
					await contexto.Database.ExecuteSqlRawAsync("UPDATE Aplicacion SET Estado = 'Cancelada' WHERE Id = {0}", turno.AplicacionId);

					// Crear la nueva aplicación
					var aplicacion = new Aplicacion
					{
						LoteProveedorId = loteP.Id,
						AgenteId = null,
						Dosis = dosis + 1,
						Estado = Estado.Pendiente
					};

					contexto.Aplicacion.Add(aplicacion);
					await contexto.SaveChangesAsync();

					// 4) Asignar el ID de la nueva aplicación al turno y guardar el turno
					turno.AplicacionId = aplicacion.Id;

					contexto.Turno.Update(turno);
					await contexto.SaveChangesAsync();

					return Ok(turno);
				}
				else
				{
					// Inspect the errors in ModelState
					var errors = ModelState.Values.SelectMany(v => v.Errors);
					var errorMessages = string.Join("; ", errors.Select(e => e.ErrorMessage));
					return BadRequest($"Modelo no válido: {errorMessages}");
				}
			}
			catch (Exception ex)
			{
				return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
		}


		/*[HttpPut("{id}")]
		public async Task<IActionResult> Put(int id, [FromForm] Turno turno)
		{
			try
			{
				if (id != turno.Id)
				{
					return BadRequest();
				}
			}
			catch (Exception ex)
			{
				return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
		}*/
	}
}