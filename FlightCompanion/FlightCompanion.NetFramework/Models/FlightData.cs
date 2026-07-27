namespace FlightCompanion.NetFramework.Models
{
    public class FlightData
    {
        public double AltitudeFeet { get; set; }

        public double GroundSpeedKnots { get; set; }

        public double VerticalSpeedFeetPerMinute { get; set; }

        public double HeadingDegrees { get; set; }

        public string AircraftTitle { get; set; }

        public bool HasActiveFlightPlan { get; set; }

        public double GpsDistanceRemainingNm { get; set; }

        public double GpsEteSeconds { get; set; }

        public int ActiveWaypointIndex { get; set; }

        public int WaypointCount { get; set; }

        public string NextWaypointId { get; set; }

        public double NextWaypointDistanceNm { get; set; }
    }
}