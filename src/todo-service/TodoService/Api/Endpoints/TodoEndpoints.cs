using System.Security.Claims;
using MediatR;
using TodoService.Application.DTOs;
using TodoService.Application.Features.CreateTodoItem;
using TodoService.Application.Features.DeleteTodoItem;
using TodoService.Application.Features.GetTodoItems;
using TodoService.Application.Features.UpdateTodoItem; // assume you create this

//using TodoService.Application.Features.DeleteTodoItem; // assume you create this

namespace TodoService.Api.Endpoints;

public static class TodoEndpoints
{
    public static void MapTodoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/todos")
            .RequireAuthorization() // all endpoints need JWT
            .WithTags("Todos")
            .WithOpenApi(); // Swagger metadata

        // GET /api/todos → list all for current user
        group
            .MapGet(
                "/",
                async (IMediator mediator, ClaimsPrincipal user, CancellationToken ct) =>
                {
                    if (
                        !Guid.TryParse(
                            user.FindFirstValue(ClaimTypes.NameIdentifier),
                            out var userId
                        )
                    )
                        return Results.Unauthorized();

                    var query = new GetTodoItemsQuery(userId);
                    var result = await mediator.Send(query, ct);

                    return Results.Ok(result); // IReadOnlyList<TodoItemSummaryDto>
                }
            )
            .Produces<IReadOnlyList<TodoItemSummaryDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .WithName("GetTodos")
            .WithSummary("Get all TODO items for the authenticated user");

        // POST /api/todos → create new
        group
            .MapPost(
                "/",
                async (
                    CreateTodoItemRequest request,
                    IMediator mediator,
                    ClaimsPrincipal user,
                    CancellationToken ct
                ) =>
                {
                    if (
                        !Guid.TryParse(
                            user.FindFirstValue(ClaimTypes.NameIdentifier),
                            out var userId
                        )
                    )
                        return Results.Unauthorized();

                    var command = new CreateTodoItemCommand(
                        request.Title,
                        request.Description,
                        request.DueDate,
                        userId
                    );

                    var created = await mediator.Send(command, ct);

                    return Results.Ok(created);
                }
            )
            .Produces<TodoItemDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .WithName("CreateTodo")
            .WithSummary("Create a new TODO item");

        // PUT /api/todos/{id}
        group
            .MapPut(
                "/{id:guid}",
                async (
                    Guid id,
                    UpdateTodoItemRequest request,
                    IMediator mediator,
                    ClaimsPrincipal user,
                    CancellationToken ct
                ) =>
                {
                    if (
                        !Guid.TryParse(
                            user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                            out var userId
                        )
                    )
                        return Results.Unauthorized();

                    var command = new UpdateTodoItemCommand(id, userId, request);
                    var result = await mediator.Send(command, ct);

                    if (result == null)
                        return Results.NotFound();

                    return Results.Ok(result);
                }
            )
            .Produces<TodoItemDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden) // if you later add policy
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .WithName("UpdateTodo")
            .WithSummary("Update a TODO item");

        // DELETE /api/todos/{id}
        group
            .MapDelete(
                "/{id:guid}",
                async (Guid id, IMediator mediator, ClaimsPrincipal user, CancellationToken ct) =>
                {
                    if (
                        !Guid.TryParse(
                            user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                            out var userId
                        )
                    )
                        return Results.Unauthorized();

                    var command = new DeleteTodoItemCommand(id, userId);
                    var deleted = await mediator.Send(command, ct);

                    return deleted ? Results.NoContent() : Results.NotFound();
                }
            )
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("DeleteTodo")
            .WithSummary("Delete a TODO item");
    }
}
