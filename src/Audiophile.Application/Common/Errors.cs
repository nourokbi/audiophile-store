namespace Audiophile.Application.Common;

// Errors raised on purpose, the API's exception handler turns each one into its status code
public abstract class AppException(string message) : Exception(message);

public sealed class NotFoundException(string message) : AppException(message);   // 404

public sealed class ConflictException(string message) : AppException(message);   // 409

// Wrong credentials, an invalid refresh token, or a webhook with a bad signature
public sealed class UnauthorizedException(string message) : AppException(message); // 401

// The payment gateway answered with an error
public sealed class GatewayException(string message) : AppException(message);    // 502

// The payment gateway did not answer in time, so the result is unknown
public sealed class GatewayTimeoutException(string message) : AppException(message); // 504
