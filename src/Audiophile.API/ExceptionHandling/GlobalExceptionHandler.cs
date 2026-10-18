using Audiophile.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Audiophile.API.ExceptionHandling;

// Turns every exception into a ProblemDetails answer with the matching status code and never a stack trace
public sealed class GlobalExceptionHandler(IProblemDetailsService problemDetails, ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
  public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
  {
    var (status, title) = exception switch
    {
      NotFoundException => (StatusCodes.Status404NotFound, "Not found"),
      ConflictException or InvalidOperationException => (StatusCodes.Status409Conflict, "Conflict"),
      UnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
      ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request"),
      GatewayTimeoutException => (StatusCodes.Status504GatewayTimeout, "Payment gateway timeout"),
      GatewayException => (StatusCodes.Status502BadGateway, "Payment gateway error"),
      DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Cannot perform change, try again later."),
      _ => (StatusCodes.Status500InternalServerError, "Unexpected error"),
    };

    if (status == StatusCodes.Status500InternalServerError)
      logger.LogError(exception, "Unhandled exception");

    context.Response.StatusCode = status;
    return await problemDetails.TryWriteAsync(new ProblemDetailsContext
    {
      HttpContext = context,
      Exception = exception,
      ProblemDetails = new ProblemDetails
      {
        Status = status,
        Title = title,
        // Our own messages are written for the caller but an unexpected error's message is not
        Detail = status == StatusCodes.Status500InternalServerError ? null : exception.Message,
      },
    });
  }
}
