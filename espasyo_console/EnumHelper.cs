namespace espasyo_console;

using System;

public static class EnumHelper
{
    // Seeded per run via SetSeed so enum picks (crime type, weather) are reproducible.
    private static Random Random = new();

    public static void SetSeed(int seed) => Random = new Random(seed);

    public static T? GetRandomEnumValue<T>() where T : Enum
    {
        var values = Enum.GetValues(typeof(T));
        return (T)values.GetValue(Random.Next(values.Length))!;
    }
}


