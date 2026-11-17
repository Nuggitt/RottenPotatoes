namespace RottenPotatoes.Api.Services;

/// <summary>Udfald af en handling, som controlleren oversætter til en HTTP-statuskode.</summary>
public enum ServiceStatus
{
    Ok,         // 200/201
    NotFound,   // 404
    Conflict,   // 409
    Forbidden,  // 403
}

public record ServiceResult<T>(ServiceStatus Status, T? Value = default)
{
    public static ServiceResult<T> Ok(T value) => new(ServiceStatus.Ok, value);

    public static ServiceResult<T> NotFound() => new(ServiceStatus.NotFound);

    public static ServiceResult<T> Conflict() => new(ServiceStatus.Conflict);

    public static ServiceResult<T> Forbidden() => new(ServiceStatus.Forbidden);
}
