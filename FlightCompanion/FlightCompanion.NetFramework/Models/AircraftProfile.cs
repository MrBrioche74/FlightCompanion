namespace FlightCompanion.NetFramework.Models
{
    public class AircraftProfile
    {
        public string Name { get; set; }

        public string Manufacturer { get; set; }

        public AircraftCategory Category { get; set; }

        /*
         * Termes recherchés dans le TITLE fourni par SimConnect.
         * Place les termes les plus spécifiques avant les plus génériques.
         */
        public string[] MatchTerms { get; set; }

        public int CruiseSpeed { get; set; }

        public int ApproachSpeed { get; set; }

        public int FinalSpeed { get; set; }

        public int RotateSpeed { get; set; }

        public int ClimbSpeed { get; set; }

        public int RecommendedDescentRate { get; set; }

        public int TypicalCruiseAltitude { get; set; }

        public bool HasAutopilot { get; set; }

        public bool HasVnav { get; set; }

        public bool HasRetractableGear { get; set; }

        public bool IsPressurized { get; set; }

        /*
         * Plus la valeur est élevée, plus ce profil est prioritaire
         * lorsqu'un même titre peut correspondre à plusieurs profils.
         */
        public int MatchPriority { get; set; }
    }
}