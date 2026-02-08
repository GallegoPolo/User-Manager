using AuditService.Application.Commons;
using AuditService.Application.DTOs;
using AuditService.Application.UseCases.Queries;
using AuditService.Domain.Interfaces.Repositories;
using MediatR;

namespace AuditService.Application.UseCases.Handlers
{
    public class GetAuditLogsHandler : IRequestHandler<GetAuditLogsQuery, Result<PagedResultDTO<AuditLogDTO>>>
    {
        private readonly IAuditLogRepository _repository;

        public GetAuditLogsHandler(IAuditLogRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<PagedResultDTO<AuditLogDTO>>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
        {
            if (request.PageNumber < 1)
                return Result<PagedResultDTO<AuditLogDTO>>.Failure("PageNumber deve ser maior que 0");

            if (request.PageSize < 1 || request.PageSize > 100)
                return Result<PagedResultDTO<AuditLogDTO>>.Failure("PageSize deve estar entre 1 e 100");

            if (request.FromDate.HasValue && request.ToDate.HasValue && request.FromDate > request.ToDate)
                return Result<PagedResultDTO<AuditLogDTO>>.Failure("FromDate não pode ser maior que ToDate");

            var (logs, totalCount) = await _repository.GetAllAsync(request.AggregateId,
                                                                   request.EventType,
                                                                   request.FromDate,
                                                                   request.ToDate,
                                                                   request.PageNumber,
                                                                   request.PageSize,
                                                                   cancellationToken);

            var dtos = logs.Select(log => new AuditLogDTO(log.Id,
                                                          log.EventId,
                                                          log.AggregateId,
                                                          log.AggregateType,
                                                          log.EventType.ToString(),
                                                          log.PerformedBy,
                                                          log.Timestamp,
                                                          log.Payload)).ToList();

            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            var pagedResult = new PagedResultDTO<AuditLogDTO>(dtos,
                                                              request.PageNumber,
                                                              request.PageSize,
                                                              totalCount,
                                                              totalPages);

            return Result<PagedResultDTO<AuditLogDTO>>.Success(pagedResult);
        }
    }
}
