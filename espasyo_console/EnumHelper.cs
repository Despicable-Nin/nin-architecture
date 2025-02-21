namespace espasyo_console;

using System;

public static class EnumHelper
{
    private static readonly Random Random = new();

    public static T? GetRandomEnumValue<T>() where T : Enum
    {
        var values = Enum.GetValues(typeof(T));
        return (T)values.GetValue(Random.Next(values.Length))!;
    }
}


