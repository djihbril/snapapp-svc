using System.ComponentModel;

namespace SnapApp.Svc;

public static class Settings
{
    public static int AccessTokenExpirationSpanInSecs => GetEnvironmentVariable<int>("AccessTokenExpirationSpanInHours") * 3600;

    public static int RefreshTokenExpirationSpanInSecs => GetEnvironmentVariable<int>("RefreshTokenExpirationSpanInDays") * 86400;

    public static string? SqlConnectionString => GetEnvironmentVariable("SqlConnectionString");

    public static string? ComServicesConnectionString => GetEnvironmentVariable("ComServicesConnectionString");

    public static string? EmailSenderAddress => GetEnvironmentVariable("EmailSenderAddress");

    public static string? LogoLink => GetEnvironmentVariable("LogoLink");

    private static string? GetEnvironmentVariable(string name) => Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);

    private static T? GetEnvironmentVariable<T>(string name)
    {
        string? value = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));

        return value != null ? (T?)converter.ConvertFrom(value) : default;
    }
}