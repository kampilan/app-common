using CommunityToolkit.Diagnostics;

namespace AppCommon.Core.Mediator;

/// <summary>
/// Type-erased result wrapper for batch command responses.
/// Allows heterogeneous command results to be handled uniformly.
/// </summary>
public record BatchCommandResult
{
    /// <summary>
    /// Whether the command succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// The response object from the command (type-erased).
    /// </summary>
    public object? Response { get; init; }

    /// <summary>
    /// The type of command that was executed.
    /// </summary>
    public required string CommandType { get; init; }

    /// <summary>
    /// The UID of the entity affected by the command.
    /// </summary>
    public string? EntityUid { get; init; }

    /// <summary>
    /// Error message if the command failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful result from a command response.
    /// </summary>
    public static BatchCommandResult Succeeded<T>(T response, string? entityUid = null)
    {
        return new BatchCommandResult
        {
            Success = true,
            Response = response,
            CommandType = typeof(T).Name.Replace("Response", ""),
            EntityUid = entityUid
        };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static BatchCommandResult Failed(string commandType, string? entityUid, string error)
    {
        Guard.IsNotNullOrWhiteSpace(commandType);
        Guard.IsNotNullOrWhiteSpace(error);

        return new BatchCommandResult
        {
            Success = false,
            CommandType = commandType,
            EntityUid = entityUid,
            ErrorMessage = error
        };
    }

    /// <summary>
    /// Attempts to get the response as a specific type.
    /// </summary>
    public T? GetResponse<T>() where T : class
    {
        return Response as T;
    }
}
