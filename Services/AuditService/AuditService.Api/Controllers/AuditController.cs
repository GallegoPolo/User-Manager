using AuditService.Application.DTOs;
using AuditService.Application.UseCases.Queries;
using AuditService.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AuditService.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuditController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<AuditController> _logger;

        public AuditController(IMediator mediator, ILogger<AuditController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet("logs")]
        [ProducesResponseType(typeof(PagedResultDTO<AuditLogDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAuditLogs([FromQuery] string? aggregateId,
                                                      [FromQuery] EEventType? eventType,
                                                      [FromQuery] DateTime? fromDate,
                                                      [FromQuery] DateTime? toDate,
                                                      [FromQuery] int pageNumber = 1,
                                                      [FromQuery] int pageSize = 20,
                                                      CancellationToken cancellationToken = default)
        {
            var query = new GetAuditLogsQuery(aggregateId, eventType, fromDate, toDate, pageNumber, pageSize);

            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Falha ao buscar logs de auditoria: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Data);
        }

        [HttpGet("logs/{id}")]
        [ProducesResponseType(typeof(AuditLogDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAuditLogById(string id, CancellationToken cancellationToken = default)
        {
            var query = new GetAuditLogByIdQuery(id);
            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Log de auditoria não encontrado: {Id}", id);
                return NotFound(new { error = result.Error });
            }

            return Ok(result.Data);
        }
    }
}
