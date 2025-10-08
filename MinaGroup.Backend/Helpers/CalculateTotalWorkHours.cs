namespace MinaGroup.Backend.Helpers
{
    public static class CalculateTotalWorkHours
    {
        public static TimeSpan CalculateTotalHours(TimeSpan arrivalTime, TimeSpan departureTime, TimeSpan? breakDuration)
        {
            var total = departureTime - arrivalTime;

            if(breakDuration.HasValue)
                total -= breakDuration.Value;

            if (total < TimeSpan.Zero)
                total = TimeSpan.Zero;

            return total;
        }
    }
}
