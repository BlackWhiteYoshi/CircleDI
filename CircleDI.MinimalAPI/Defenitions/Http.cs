namespace CircleDI.MinimalAPI.Defenitions;

/// <summary>
/// HTTP method of the endpoint.
/// </summary>
public enum Http {
    /// <summary>
    /// Matches any HTTP requests for the specified pattern.
    /// </summary>
    Any = 0,

    /// <summary>
    /// Matches HTTP GET requests for the specified pattern.
    /// </summary>
    Get,

    /// <summary>
    /// Matches HTTP POST requests for the specified pattern.
    /// </summary>
    Post,

    /// <summary>
    /// Matches HTTP PUT requests for the specified pattern.
    /// </summary>
    Put,

    /// <summary>
    /// Matches HTTP PATCH requests for the specified pattern.
    /// </summary>
    Patch,

    /// <summary>
    /// Matches HTTP DELETE requests for the specified pattern.
    /// </summary>
    Delete,
}

public static class HttpExtension {
    public static string AsString(this Http http)
        => http switch {
            Http.Any => "",
            Http.Get => "Get",
            Http.Post => "Post",
            Http.Put => "Put",
            Http.Patch => "Patch",
            Http.Delete => "Delete",
            _ => ""
        };
}
