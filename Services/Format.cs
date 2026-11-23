using System.Globalization;

namespace Camstep.Services;

public static class Format
{
    public static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-CO");

    public static string Money(decimal value) => "$" + value.ToString("N0", Es);

    public static string Date(DateTime value) => value.ToString("dd/MM/yyyy", Es);

    public static string LongDate(DateTime value) =>
        Capitalize(value.ToString("dddd, d 'de' MMMM 'de' yyyy", Es));

    public static string MonthYear(DateTime value) =>
        Capitalize(value.ToString("MMMM 'de' yyyy", Es));

    public static string Time(DateTime value) => value.ToString("HH:mm", Es);

    public static string Capitalize(string text) =>
        string.IsNullOrEmpty(text) ? text : char.ToUpper(text[0], Es) + text[1..];
}

public static class Priorities
{
    public static readonly string[] All = { "Alta", "Media", "Baja" };

    public static int Rank(string? priority) => priority switch
    {
        "Alta" => 0,
        "Media" => 1,
        "Baja" => 2,
        _ => 3
    };

    public static bool IsValid(string? priority) => All.Contains(priority);
}
