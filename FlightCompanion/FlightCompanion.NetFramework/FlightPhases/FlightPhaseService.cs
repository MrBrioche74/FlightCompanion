using FlightCompanion.NetFramework.Models;

namespace FlightCompanion.NetFramework.FlightPhases
{
    public class FlightPhaseService
    {
        public FlightPhase GetCurrentPhase(
            FlightData data)
        {
            if (data.GroundSpeedKnots < 5)
                return FlightPhase.Parking;

            if (data.GroundSpeedKnots < 30)
                return FlightPhase.Taxi;

            if (data.AltitudeFeet < 150 &&
                data.VerticalSpeedFeetPerMinute > 300)
                return FlightPhase.Takeoff;

            if (data.VerticalSpeedFeetPerMinute > 300)
                return FlightPhase.Climb;

            if (data.VerticalSpeedFeetPerMinute < -300 &&
                data.AltitudeFeet > 2000)
                return FlightPhase.Descent;

            if (data.AltitudeFeet < 2000 &&
                data.VerticalSpeedFeetPerMinute < 0)
                return FlightPhase.Approach;

            if (data.AltitudeFeet < 50 &&
                data.GroundSpeedKnots > 40)
                return FlightPhase.Landing;

            return FlightPhase.Cruise;
        }
    }
}