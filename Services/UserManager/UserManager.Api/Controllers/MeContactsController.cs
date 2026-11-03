using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManager.Api.Contracts.Requests;
using UserManager.Api.Contracts.Responses;
using UserManager.Application.UseCases.Contacts.Commands;
using UserManager.Application.UseCases.Contacts.DTOs;
using UserManager.Application.UseCases.Contacts.Queries;

namespace UserManager.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/me/contacts")]
public sealed class MeContactsController : ControllerBase
{
    private readonly IMediator _mediator;

    public MeContactsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateContactRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateContactCommand(request.Name, request.Email, request.Phone), cancellationToken);

        if (!result.IsSuccess)
            return this.Failure(result);

        return CreatedAtAction(nameof(GetById), new { contactId = result.Value }, new CreateContactResponse(result.Value));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetContactsQuery(), cancellationToken);

        if (!result.IsSuccess)
            return this.Failure(result);

        return Ok(result.Value!.Select(ToResponse));
    }

    [HttpGet("{contactId:guid}")]
    public async Task<IActionResult> GetById(Guid contactId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetContactByIdQuery(contactId), cancellationToken);

        if (!result.IsSuccess)
            return this.Failure(result);

        return Ok(ToResponse(result.Value!));
    }

    [HttpPut("{contactId:guid}")]
    public async Task<IActionResult> Update(Guid contactId, [FromBody] UpdateContactRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdateContactCommand(contactId, request.Name, request.Email, request.Phone), cancellationToken);

        if (!result.IsSuccess)
            return this.Failure(result);

        return NoContent();
    }

    [HttpDelete("{contactId:guid}")]
    public async Task<IActionResult> Delete(Guid contactId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteContactCommand(contactId), cancellationToken);

        if (!result.IsSuccess)
            return this.Failure(result);

        return NoContent();
    }

    private static ContactResponse ToResponse(ContactDTO dto) =>
        new(dto.Id, dto.Name, dto.Email, dto.Phone, dto.CreatedAt);
}
