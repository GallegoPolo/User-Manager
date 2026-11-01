using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManager.Api.Contracts.Requests;
using UserManager.Api.Contracts.Responses;
using UserManager.Application.Common;
using UserManager.Application.UseCases.Contacts.Commands;
using UserManager.Application.UseCases.Contacts.DTOs;
using UserManager.Application.UseCases.Contacts.Queries;

namespace UserManager.Api.Controllers;

[Authorize(Policy = "AdminOnly")]
[ApiController]
[Route("api/users/{userId:guid}/contacts")]
public sealed class ContactsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ContactsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid userId, [FromBody] CreateContactRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateContactCommand(userId, request.Name, request.Email, request.Phone);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return Failure(result);

        return CreatedAtAction(nameof(GetById), new { userId, contactId = result.Value }, new CreateContactResponse(result.Value));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetContactsQuery(userId), cancellationToken);

        if (!result.IsSuccess)
            return Failure(result);

        return Ok(result.Value!.Select(ToResponse));
    }

    [HttpGet("{contactId:guid}")]
    public async Task<IActionResult> GetById(Guid userId, Guid contactId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetContactByIdQuery(userId, contactId), cancellationToken);

        if (!result.IsSuccess)
            return Failure(result);

        return Ok(ToResponse(result.Value!));
    }

    [HttpPut("{contactId:guid}")]
    public async Task<IActionResult> Update(Guid userId, Guid contactId, [FromBody] UpdateContactRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateContactCommand(userId, contactId, request.Name, request.Email, request.Phone);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return Failure(result);

        return NoContent();
    }

    [HttpDelete("{contactId:guid}")]
    public async Task<IActionResult> Delete(Guid userId, Guid contactId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteContactCommand(userId, contactId), cancellationToken);

        if (!result.IsSuccess)
            return Failure(result);

        return NoContent();
    }

    private static ContactResponse ToResponse(ContactDTO dto) =>
        new(dto.Id, dto.Name, dto.Email, dto.Phone, dto.CreatedAt);

    private IActionResult Failure<T>(Result<T> result) => result.ErrorType switch
    {
        ErrorType.NotFound => NotFound(result.Errors),
        ErrorType.Conflict => Conflict(result.Errors),
        _ => BadRequest(result.Errors)
    };
}
