using AuditService.Application.Commons;
using AuditService.Application.DTOs;
using AuditService.Application.UseCases.Queries;
using AuditService.Domain.Interfaces.Repositories;
using MediatR;

namespace AuditService.Application.UseCases.Handlers
{
    public class GetAuditLogByIdHandler : IRequestHandler<GetAuditLogByIdQuery, Result<AuditLogDTO>>
    {
        private readonly IAuditLogRepository _repository;

        public GetAuditLogByIdHandler(IAuditLogRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<AuditLogDTO>> Handle(GetAuditLogByIdQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Id))
                return Result<AuditLogDTO>.Failure("Id é obrigatório");

            var log = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (log is null)
                return Result<AuditLogDTO>.Failure("Log de auditoria não encontrado");

            var dto = new AuditLogDTO(log.Id,
                                      log.EventId,
                                      log.AggregateId,
                                      log.AggregateType,
                                      log.EventType.ToString(),
                                      log.PerformedBy,
                                      log.Timestamp,
                                      log.Payload
            );

            return Result<AuditLogDTO>.Success(dto);
        }
    }
}
