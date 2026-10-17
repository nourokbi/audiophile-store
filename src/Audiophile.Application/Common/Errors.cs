namespace Audiophile.Application.Common;

// Errors a use case raises on purpose. The API's exception handler turns each
// into a ProblemDetails response with the matching status code.
public abstract class AppException(string message) : Exception(message);

public sealed class NotFoundException(string message) : AppException(message);   // 404

public sealed class ConflictException(string message) : AppException(message);   // 409

// Wrong credentials, or a refresh token that is unknown, expired or revoked.
public sealed class UnauthorizedException(string message) : AppException(message); // 401

// The gateway answered with an error. Safe to show the customer a retry.
public sealed class GatewayException(string message) : AppException(message);    // 502

// The gateway did not answer in time. The operation may or may not have happened
// at the gateway, so the caller must check its status before trying again.
public sealed class GatewayTimeoutException(string message) : AppException(message); // 504
