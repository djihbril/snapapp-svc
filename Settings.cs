using System.ComponentModel;

namespace SnapApp.Svc;

public static class Settings
{
    public static int AccessTokenExpirationSpanInSecs => GetEnvironmentVariable<int>("AccessTokenExpirationSpanInHours") * 3600;

    public static string? SqlConnectionString => GetEnvironmentVariable("SqlConnectionString");

    private static string? GetEnvironmentVariable(string name) => Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);

    private static T? GetEnvironmentVariable<T>(string name)
    {
        string? value = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));

        return value != null ? (T?)converter.ConvertFrom(value) : default;
    }
}