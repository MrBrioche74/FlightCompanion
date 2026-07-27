using System;

namespace FlightCompanion.NetFramework.Calculators
{
    public static class TodCalculator
    {
        private const double FeetPerNauticalMile = 300.0;

        public static double CalculateRequiredDescentDistance(
            double currentAltitudeFeet,
            double targetAltitudeFeet)
        {
            double altitudeToLose =
                currentAltitudeFeet - targetAltitudeFeet;

            if (altitudeToLose <= 0)
            {
                return 0;
            }

            return altitudeToLose / FeetPerNauticalMile;
        }

        public static double CalculateDistanceBeforeTod(
            double currentAltitudeFeet,
            double targetAltitudeFeet,
            double distanceRemainingNm)
        {
            double requiredDistance =
                CalculateRequiredDescentDistance(
                    currentAltitudeFeet,
                    targetAltitudeFeet);

            return distanceRemainingNm - requiredDistance;
        }

        public static TimeSpan CalculateTimeBeforeTod(
            double distanceBeforeTodNm,
            double groundSpeedKnots)
        {
            if (distanceBeforeTodNm <= 0 ||
                groundSpeedKnots <= 0)
            {
                return TimeSpan.Zero;
            }

            return TimeSpan.FromHours(
                distanceBeforeTodNm / groundSpeedKnots);
        }
    }
}