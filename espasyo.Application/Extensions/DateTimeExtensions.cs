namespace espasyo.Application.Extensions;

public static class DateTimeExtensions
{
    public static string GetTimeOfDay(this DateTime dt)
    {
        // If the hour is before noon, return "Morning"
        if (dt.Hour < 12)
        {
            return "Morning";
        }
        // If the hour is after noon but before 6 PM, return "Afternoon"
        else if (dt.Hour < 18)
        {
            return "Afternoon";
        }
        // Otherwise, return "Evening"
        else
        {
            return "Evening";
        }
    }

    public static string GetTimeOfDay(this DateTimeOffset dt) => dt.GetTimeOfDay();

}