namespace FlightCompanion.NetFramework.Calculators
{
    public static class DescentCalculator
    {
        public static double CalculateVerticalSpeed(
            double currentAltitude,
            double targetAltitude,
            double distanceNm,
            double groundSpeedKt)
        {
            if (distanceNm <= 0 ||
                groundSpeedKt <= 0)
            {
                return 0;
            }

            double altitudeToLose =
                currentAltitude - targetAltitude;

            double timeMinutes =
                distanceNm /
                groundSpeedKt *
                60.0;

            return -(altitudeToLose / timeMinutes);
        }
    }
}