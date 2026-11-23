namespace RottenPotatoes.Api.Services;

/// <summary>Udfald af en handling, som controlleren oversætter til en HTTP-statuskode.</summary>
public enum ServiceStatus
{
    /// <summary>200/201.</summary>
    Ok,

    /// <summary>404.</summary>
    NotFound,

    /// <summary>409.</summary>
    Conflict,

    /// <summary>403.</summary>
    Forbidden,
}

public record ServiceResult<T>(ServiceStatus Status, T? Value = default)
{
    public static ServiceResult<T> Ok(T value) => new(ServiceStatus.Ok, value);

    public static ServiceResult<T> NotFound() => new(ServiceStatus.NotFound);

    public static ServiceResult<T> Conflict() => new(ServiceStatus.Conflict);

    public static ServiceResult<T> Forbidden() => new(ServiceStatus.Forbidden);
}
