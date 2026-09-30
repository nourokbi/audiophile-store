namespace Audiophile.Application.Common;

/// <summary>
/// Errors a use case raises on purpose. The API's exception handler turns each
/// into a ProblemDetails response with the matching status code.
/// </summary>
public abstract class AppException(string message) : Exception(message);

public sealed class NotFoundException(string message) : AppException(message);   // 404

public sealed class ConflictException(string message) : AppException(message);   // 409

/// <summary>The gateway answered with an error. Safe to show the customer a retry.</summary>
public sealed class GatewayException(string message) : AppException(message);    // 502

/// <summary>
/// The gateway did not answer in time. The operation may or may not have happened
/// at the gateway, so the caller must check its status before trying again.
/// </summary>
public sealed class GatewayTimeoutException(string message) : AppException(message); // 504
