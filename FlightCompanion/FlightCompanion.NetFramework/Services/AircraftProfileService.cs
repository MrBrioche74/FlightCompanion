using System;
using System.Collections.Generic;
using System.Linq;
using FlightCompanion.NetFramework.Models;

namespace FlightCompanion.NetFramework.Services
{
    public class AircraftProfileService
    {
        private readonly List<AircraftProfile> profiles;

        public AircraftProfileService()
        {
            profiles = new List<AircraftProfile>
            {
                new AircraftProfile
                {
                    Name = "Cessna 172",
                    SimTitleContains = "C172",
                    CruiseSpeed = 120,
                    ApproachSpeed = 65,
                    RecommendedDescentRate = 500
                },

                new AircraftProfile
                {
                    Name = "Cessna 208 Grand Caravan",
                    SimTitleContains = "C208",
                    CruiseSpeed = 185,
                    ApproachSpeed = 85,
                    RecommendedDescentRate = 800
                },

                new AircraftProfile
                {
                    Name = "King Air 350",
                    SimTitleContains = "King Air",
                    CruiseSpeed = 310,
                    ApproachSpeed = 110,
                    RecommendedDescentRate = 1200
                },

                new AircraftProfile
                {
                    Name = "TBM 930",
                    SimTitleContains = "TBM",
                    CruiseSpeed = 330,
                    ApproachSpeed = 90,
                    RecommendedDescentRate = 1200
                },

                new AircraftProfile
                {
                    Name = "Airbus A320",
                    SimTitleContains = "A320",
                    CruiseSpeed = 450,
                    ApproachSpeed = 140,
                    RecommendedDescentRate = 1800
                },

                new AircraftProfile
                {
                    Name = "Boeing 737",
                    SimTitleContains = "737",
                    CruiseSpeed = 460,
                    ApproachSpeed = 145,
                    RecommendedDescentRate = 1700
                },

                new AircraftProfile
                {
                    Name = "ATR 72",
                    SimTitleContains = "ATR",
                    CruiseSpeed = 250,
                    ApproachSpeed = 110,
                    RecommendedDescentRate = 900
                }
            };
        }

        public AircraftProfile FindProfile(string aircraftTitle)
        {
            if (string.IsNullOrWhiteSpace(aircraftTitle))
            {
                return null;
            }

            return profiles.FirstOrDefault(
                profile => aircraftTitle.IndexOf(
                    profile.SimTitleContains,
                    StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public AircraftProfile GetDefaultProfile()
        {
            return new AircraftProfile
            {
                Name = "Avion non reconnu",
                SimTitleContains = string.Empty,
                CruiseSpeed = 0,
                ApproachSpeed = 0,
                RecommendedDescentRate = 500
            };
        }
    }
}