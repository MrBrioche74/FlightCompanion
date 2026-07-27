namespace FlightCompanion.NetFramework.Models
{
    public class AircraftProfile
    {
        public string Name { get; set; }

        public string SimTitleContains { get; set; }

        public int CruiseSpeed { get; set; }

        public int ApproachSpeed { get; set; }

        public int RecommendedDescentRate { get; set; }
    }
}