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
            profiles = CreateProfiles();
        }

        public AircraftProfile FindProfile(string aircraftTitle)
        {
            if (string.IsNullOrWhiteSpace(aircraftTitle))
            {
                return null;
            }

            string normalizedTitle = NormalizeText(aircraftTitle);
            string[] titleTokens = Tokenize(normalizedTitle);

            AircraftProfile bestProfile = null;
            int bestScore = int.MinValue;

            foreach (AircraftProfile profile in profiles)
            {
                if (profile == null ||
                    profile.MatchTerms == null ||
                    profile.MatchTerms.Length == 0)
                {
                    continue;
                }

                int profileBestScore = int.MinValue;

                foreach (string term in profile.MatchTerms)
                {
                    int matchQuality = GetMatchQuality(
                        normalizedTitle,
                        titleTokens,
                        term);

                    if (matchQuality <= 0)
                    {
                        continue;
                    }

                    /*
                     * Le score privilégie :
                     * 1. la qualité réelle de la correspondance ;
                     * 2. la priorité du profil ;
                     * 3. les alias les plus précis et les plus longs.
                     */
                    int score =
                        matchQuality * 100000 +
                        profile.MatchPriority * 100 +
                        NormalizeText(term).Length;

                    if (score > profileBestScore)
                    {
                        profileBestScore = score;
                    }
                }

                /*
                 * Bonus léger lorsque le constructeur apparaît aussi dans
                 * le titre SimConnect. Il ne crée jamais une correspondance
                 * à lui seul.
                 */
                if (profileBestScore > int.MinValue &&
                    ContainsTokenSequence(
                        titleTokens,
                        Tokenize(NormalizeText(profile.Manufacturer))))
                {
                    profileBestScore += 25;
                }

                if (profileBestScore > bestScore)
                {
                    bestScore = profileBestScore;
                    bestProfile = profile;
                }
            }

            return bestProfile;
        }

        private static int GetMatchQuality(
            string normalizedTitle,
            string[] titleTokens,
            string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return 0;
            }

            string normalizedTerm = NormalizeText(term);

            if (normalizedTerm.Length == 0)
            {
                return 0;
            }

            string[] termTokens = Tokenize(normalizedTerm);

            if (termTokens.Length == 0)
            {
                return 0;
            }

            // Le titre complet correspond exactement à l'alias.
            if (string.Equals(
                normalizedTitle,
                normalizedTerm,
                StringComparison.Ordinal))
            {
                return 100;
            }

            // L'alias apparaît comme une véritable suite de mots/tokens.
            if (ContainsTokenSequence(titleTokens, termTokens))
            {
                return termTokens.Length > 1 ? 90 : 80;
            }

            /*
             * Accepte les différences de séparateurs :
             * PC-6 / PC6, C-17 / C17, A320 NEO / A320neo.
             *
             * Seuls des tokens complets et adjacents sont concaténés.
             * Ainsi, "C17" ne correspond jamais à "C172SP".
             */
            if (ContainsConcatenatedTokenSequence(titleTokens, termTokens))
            {
                return termTokens.Length > 1 ? 85 : 75;
            }

            return 0;
        }

        private static string NormalizeText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            char[] characters = value
                .Trim()
                .ToUpperInvariant()
                .ToCharArray();

            for (int index = 0; index < characters.Length; index++)
            {
                if (!char.IsLetterOrDigit(characters[index]))
                {
                    characters[index] = ' ';
                }
            }

            return string.Join(
                " ",
                new string(characters)
                    .Split(
                        new[] { ' ' },
                        StringSplitOptions.RemoveEmptyEntries));
        }

        private static string[] Tokenize(string normalizedValue)
        {
            if (string.IsNullOrWhiteSpace(normalizedValue))
            {
                return new string[0];
            }

            return normalizedValue.Split(
                new[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries);
        }

        private static bool ContainsTokenSequence(
            string[] titleTokens,
            string[] termTokens)
        {
            if (titleTokens == null ||
                termTokens == null ||
                titleTokens.Length == 0 ||
                termTokens.Length == 0 ||
                termTokens.Length > titleTokens.Length)
            {
                return false;
            }

            for (int start = 0;
                start <= titleTokens.Length - termTokens.Length;
                start++)
            {
                bool allTokensMatch = true;

                for (int offset = 0;
                    offset < termTokens.Length;
                    offset++)
                {
                    if (!string.Equals(
                        titleTokens[start + offset],
                        termTokens[offset],
                        StringComparison.Ordinal))
                    {
                        allTokensMatch = false;
                        break;
                    }
                }

                if (allTokensMatch)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsConcatenatedTokenSequence(
            string[] titleTokens,
            string[] termTokens)
        {
            if (titleTokens == null ||
                termTokens == null ||
                titleTokens.Length == 0 ||
                termTokens.Length == 0)
            {
                return false;
            }

            string compactTerm = string.Concat(termTokens);

            /*
             * Examine toutes les suites de tokens adjacents.
             * Une égalité complète est obligatoire : aucune sous-chaîne.
             */
            for (int start = 0; start < titleTokens.Length; start++)
            {
                string compactCandidate = string.Empty;

                for (int end = start; end < titleTokens.Length; end++)
                {
                    compactCandidate += titleTokens[end];

                    if (compactCandidate.Length > compactTerm.Length)
                    {
                        break;
                    }

                    if (string.Equals(
                        compactCandidate,
                        compactTerm,
                        StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public AircraftProfile GetDefaultProfile()
        {
            return new AircraftProfile
            {
                Name = "Avion non reconnu",
                Manufacturer = "Inconnu",
                Category = AircraftCategory.Unknown,
                MatchTerms = new string[0],
                CruiseSpeed = 0,
                ApproachSpeed = 0,
                FinalSpeed = 0,
                RotateSpeed = 0,
                ClimbSpeed = 0,
                RecommendedDescentRate = 500,
                TypicalCruiseAltitude = 0,
                HasAutopilot = false,
                HasVnav = false,
                HasRetractableGear = false,
                IsPressurized = false,
                MatchPriority = 0
            };
        }

        public IList<AircraftProfile> GetAllProfiles()
        {
            return profiles
                .OrderBy(profile => profile.Manufacturer)
                .ThenBy(profile => profile.Name)
                .ToList();
        }

        private static List<AircraftProfile> CreateProfiles()
        {
            return new List<AircraftProfile>
            {
                // ---------------------------------------------------------
                // CESSNA
                // ---------------------------------------------------------
                Profile(
                    "Cessna 152",
                    "Cessna",
                    AircraftCategory.PistonSingle,
                    new[] { "C152", "Cessna 152" },
                    105, 60, 55, 50, 70, 450, 8500,
                    false, false, false, false, 50),

                Profile(
                    "Cessna 172 Skyhawk",
                    "Cessna",
                    AircraftCategory.PistonSingle,
                    new[]
                    {
                        "C172SP",
                        "C172 G1000",
                        "Cessna 172",
                        "Skyhawk"
                    },
                    120, 65, 60, 55, 75, 500, 10000,
                    true, false, false, false, 80),

                Profile(
                    "Cessna 172 Classic",
                    "Cessna",
                    AircraftCategory.PistonSingle,
                    new[]
                    {
                        "C172 Classic",
                        "C172 Steam",
                        "Skyhawk Classic"
                    },
                    115, 65, 60, 55, 75, 500, 9500,
                    false, false, false, false, 95),

                Profile(
                    "Cessna 182 Skylane",
                    "Cessna",
                    AircraftCategory.PistonSingle,
                    new[] { "C182", "Cessna 182", "Skylane" },
                    145, 75, 70, 55, 85, 600, 12000,
                    true, false, false, false, 80),

                Profile(
                    "Cessna 208B Grand Caravan EX",
                    "Cessna",
                    AircraftCategory.TurbopropSingle,
                    new[]
                    {
                        "C208B",
                        "C208",
                        "Grand Caravan",
                        "Caravan EX"
                    },
                    185, 90, 85, 75, 105, 800, 18000,
                    true, false, false, false, 90),

                Profile(
                    "Cessna Citation CJ4",
                    "Cessna",
                    AircraftCategory.BusinessJet,
                    new[] { "Citation CJ4", "CJ4" },
                    440, 125, 115, 105, 180, 1700, 43000,
                    true, true, true, true, 100),

                Profile(
                    "Cessna Citation Longitude",
                    "Cessna",
                    AircraftCategory.BusinessJet,
                    new[] { "Citation Longitude", "Longitude" },
                    480, 135, 125, 115, 190, 1800, 45000,
                    true, true, true, true, 100),

                // ---------------------------------------------------------
                // BEECHCRAFT
                // ---------------------------------------------------------
                Profile(
                    "Beechcraft Bonanza G36",
                    "Beechcraft",
                    AircraftCategory.PistonSingle,
                    new[] { "Bonanza G36", "G36 Bonanza", "Bonanza" },
                    175, 85, 75, 70, 105, 700, 15000,
                    true, false, true, false, 80),

                Profile(
                    "Beechcraft Baron G58",
                    "Beechcraft",
                    AircraftCategory.PistonTwin,
                    new[] { "Baron G58", "G58 Baron", "Baron" },
                    200, 95, 85, 85, 120, 800, 18000,
                    true, false, true, false, 80),

                Profile(
                    "Beechcraft King Air 350i",
                    "Beechcraft",
                    AircraftCategory.TurbopropTwin,
                    new[]
                    {
                        "King Air 350",
                        "KingAir 350",
                        "King Air"
                    },
                    310, 110, 105, 100, 140, 1200, 35000,
                    true, true, true, true, 90),

                // ---------------------------------------------------------
                // CIRRUS
                // ---------------------------------------------------------
                Profile(
                    "Cirrus SR22",
                    "Cirrus",
                    AircraftCategory.PistonSingle,
                    new[] { "Cirrus SR22", "SR22" },
                    180, 80, 75, 70, 100, 700, 17000,
                    true, false, false, false, 90),

                Profile(
                    "Cirrus SF50 Vision Jet",
                    "Cirrus",
                    AircraftCategory.BusinessJet,
                    new[]
                    {
                        "Microsoft Vision Jet",
                        "Cirrus Vision Jet",
                        "SF50",
                        "Vision Jet"
                    },
                    300, 105, 95, 90, 140, 1200, 31000,
                    true, true, true, true, 110),

                // ---------------------------------------------------------
                // DIAMOND
                // ---------------------------------------------------------
                Profile(
                    "Diamond DA40 NG",
                    "Diamond",
                    AircraftCategory.PistonSingle,
                    new[] { "DA40 NG", "Diamond DA40", "DA40" },
                    145, 75, 70, 60, 90, 600, 16000,
                    true, false, false, false, 90),

                Profile(
                    "Diamond DA62",
                    "Diamond",
                    AircraftCategory.PistonTwin,
                    new[] { "Diamond DA62", "DA62" },
                    190, 90, 85, 80, 110, 750, 20000,
                    true, false, true, false, 90),

                // ---------------------------------------------------------
                // DAHER / PILATUS
                // ---------------------------------------------------------
                Profile(
                    "Daher TBM 930",
                    "Daher",
                    AircraftCategory.TurbopropSingle,
                    new[] { "TBM 930", "TBM930", "TBM" },
                    330, 95, 85, 85, 125, 1200, 31000,
                    true, true, true, true, 100),

                Profile(
                    "Daher TBM 850",
                    "Daher",
                    AircraftCategory.TurbopropSingle,
                    new[] { "TBM 850", "TBM850" },
                    320, 95, 85, 85, 125, 1200, 31000,
                    true, false, true, true, 120),

                Profile(
                    "Pilatus PC-6 Porter",
                    "Pilatus",
                    AircraftCategory.TurbopropSingle,
                    new[] { "PC-6", "PC6", "Porter" },
                    125, 70, 65, 60, 85, 650, 18000,
                    true, false, true, false, 80),

                Profile(
                    "Pilatus PC-12",
                    "Pilatus",
                    AircraftCategory.TurbopropSingle,
                    new[] { "PC-12", "PC12" },
                    285, 100, 90, 85, 125, 1000, 30000,
                    true, true, true, true, 90),

                Profile(
                    "Pilatus PC-24",
                    "Pilatus",
                    AircraftCategory.BusinessJet,
                    new[] { "PC-24", "PC24" },
                    440, 120, 110, 105, 170, 1600, 45000,
                    true, true, true, true, 90),

                // ---------------------------------------------------------
                // CUBCRAFTERS / STOL / SPORT
                // ---------------------------------------------------------
                Profile(
                    "CubCrafters X-Cub",
                    "CubCrafters",
                    AircraftCategory.PistonSingle,
                    new[] { "X-Cub", "XCub", "CubCrafters" },
                    125, 60, 55, 45, 70, 450, 12000,
                    true, false, false, false, 80),

                Profile(
                    "Zlin Savage Norden",
                    "Zlin",
                    AircraftCategory.PistonSingle,
                    new[] { "Savage Norden", "Savage" },
                    105, 55, 50, 40, 65, 400, 10000,
                    false, false, false, false, 80),

                Profile(
                    "Robin DR400",
                    "Robin",
                    AircraftCategory.PistonSingle,
                    new[] { "DR400", "Robin DR" },
                    115, 65, 60, 55, 75, 500, 10000,
                    false, false, false, false, 80),

                Profile(
                    "CAP 10",
                    "Mudry",
                    AircraftCategory.Experimental,
                    new[] { "CAP 10", "CAP10" },
                    130, 75, 70, 65, 85, 600, 12000,
                    false, false, false, false, 80),

                Profile(
                    "Extra 330",
                    "Extra",
                    AircraftCategory.Experimental,
                    new[] { "Extra 330", "Extra330" },
                    170, 85, 80, 70, 100, 800, 15000,
                    false, false, false, false, 80),

                Profile(
                    "Aviat Pitts Special",
                    "Aviat",
                    AircraftCategory.Experimental,
                    new[] { "Pitts", "Pitts Special" },
                    150, 85, 80, 70, 95, 750, 15000,
                    false, false, false, false, 80),

                // ---------------------------------------------------------
                // DE HAVILLAND / AMPHIBIANS
                // ---------------------------------------------------------
                Profile(
                    "De Havilland DHC-6 Twin Otter",
                    "De Havilland",
                    AircraftCategory.TurbopropTwin,
                    new[] { "DHC-6", "DHC6", "Twin Otter" },
                    170, 90, 85, 80, 105, 800, 25000,
                    true, false, true, false, 90),

                Profile(
                    "De Havilland CL-415",
                    "De Havilland",
                    AircraftCategory.Amphibian,
                    new[] { "CL-415", "CL415" },
                    180, 100, 95, 90, 115, 900, 20000,
                    true, false, true, false, 90),

                Profile(
                    "Icon A5",
                    "ICON",
                    AircraftCategory.Amphibian,
                    new[] { "ICON A5", "Icon A5" },
                    95, 55, 50, 45, 65, 400, 10000,
                    false, false, false, false, 80),

                // ---------------------------------------------------------
                // ATR / REGIONAL
                // ---------------------------------------------------------
                Profile(
                    "ATR 42-600",
                    "ATR",
                    AircraftCategory.RegionalAirliner,
                    new[] { "ATR 42-600", "ATR42", "ATR 42" },
                    245, 105, 100, 95, 125, 900, 25000,
                    true, true, true, true, 110),

                Profile(
                    "ATR 72-600",
                    "ATR",
                    AircraftCategory.RegionalAirliner,
                    new[] { "ATR 72-600", "ATR72", "ATR 72" },
                    250, 115, 110, 105, 130, 950, 25000,
                    true, true, true, true, 110),

                Profile(
                    "Heart Aerospace ES-30",
                    "Heart Aerospace",
                    AircraftCategory.RegionalAirliner,
                    new[] { "ES-30", "ES30", "Heart Aerospace" },
                    250, 105, 100, 95, 125, 900, 25000,
                    true, true, true, true, 90),

                // ---------------------------------------------------------
                // AIRBUS
                // ---------------------------------------------------------
                Profile(
                    "Airbus A310-300",
                    "Airbus",
                    AircraftCategory.Airliner,
                    new[] { "A310-300", "A310" },
                    470, 145, 135, 135, 180, 1800, 39000,
                    true, true, true, true, 100),

                Profile(
                    "Airbus A320neo",
                    "Airbus",
                    AircraftCategory.Airliner,
                    new[]
                    {
                        "A320neo",
                        "A320 NEO",
                        "Airbus A320",
                        "A320"
                    },
                    450, 140, 130, 130, 180, 1800, 39000,
                    true, true, true, true, 90),

                Profile(
                    "Airbus A321LR",
                    "Airbus",
                    AircraftCategory.Airliner,
                    new[] { "A321LR", "A321 LR", "A321" },
                    455, 145, 135, 135, 185, 1800, 39000,
                    true, true, true, true, 120),

                Profile(
                    "Airbus A330",
                    "Airbus",
                    AircraftCategory.Airliner,
                    new[]
                    {
                        "A330-200",
                        "A330-300",
                        "A330-743L",
                        "A330"
                    },
                    480, 150, 140, 140, 190, 1900, 41000,
                    true, true, true, true, 100),

                Profile(
                    "Airbus Beluga XL",
                    "Airbus",
                    AircraftCategory.Cargo,
                    new[] { "Beluga XL", "BelugaXL", "A330-743L" },
                    450, 145, 135, 135, 185, 1800, 35000,
                    true, true, true, true, 130),

                Profile(
                    "Airbus A400M Atlas",
                    "Airbus",
                    AircraftCategory.Military,
                    new[] { "A400M", "Atlas" },
                    420, 120, 110, 105, 150, 1400, 37000,
                    true, true, true, true, 100),

                // ---------------------------------------------------------
                // BOEING
                // ---------------------------------------------------------
                Profile(
                    "Boeing 737 MAX 8",
                    "Boeing",
                    AircraftCategory.Airliner,
                    new[]
                    {
                        "737 MAX 8",
                        "737-8 MAX",
                        "B737 MAX",
                        "737 MAX"
                    },
                    455, 145, 135, 135, 185, 1800, 41000,
                    true, true, true, true, 120),

                Profile(
                    "Boeing 747-8 Intercontinental",
                    "Boeing",
                    AircraftCategory.Airliner,
                    new[]
                    {
                        "747-8 Intercontinental",
                        "747-8i",
                        "B747-8",
                        "747-8"
                    },
                    490, 160, 150, 150, 190, 2000, 43000,
                    true, true, true, true, 100),

                Profile(
                    "Boeing 747 Supertanker",
                    "Boeing",
                    AircraftCategory.Cargo,
                    new[] { "Supertanker", "747-400 Global" },
                    470, 160, 150, 150, 190, 1900, 40000,
                    true, false, true, true, 140),

                Profile(
                    "Boeing 747 Dreamlifter",
                    "Boeing",
                    AircraftCategory.Cargo,
                    new[] { "Dreamlifter", "747-400 LCF" },
                    460, 160, 150, 150, 190, 1900, 39000,
                    true, false, true, true, 140),

                Profile(
                    "Boeing 787-10 Dreamliner",
                    "Boeing",
                    AircraftCategory.Airliner,
                    new[]
                    {
                        "787-10",
                        "B787-10",
                        "Dreamliner"
                    },
                    490, 150, 140, 140, 190, 1900, 43000,
                    true, true, true, true, 110),

                Profile(
                    "Boeing C-17 Globemaster III",
                    "Boeing",
                    AircraftCategory.Military,
                    new[] { "C-17", "C17", "Globemaster" },
                    450, 130, 120, 115, 160, 1500, 45000,
                    true, true, true, true, 100),

                // ---------------------------------------------------------
                // HELICOPTERS
                // ---------------------------------------------------------
                Profile(
                    "Guimbal Cabri G2",
                    "Guimbal",
                    AircraftCategory.Helicopter,
                    new[] { "Cabri G2", "Cabri" },
                    90, 45, 40, 0, 60, 500, 8000,
                    false, false, false, false, 90),

                Profile(
                    "Bell 407",
                    "Bell",
                    AircraftCategory.Helicopter,
                    new[] { "Bell 407", "B407" },
                    120, 60, 50, 0, 70, 600, 12000,
                    true, false, false, false, 90),

                Profile(
                    "Airbus H125",
                    "Airbus Helicopters",
                    AircraftCategory.Helicopter,
                    new[] { "H125", "AS350" },
                    125, 60, 50, 0, 75, 600, 15000,
                    true, false, false, false, 90),

                Profile(
                    "Airbus H225",
                    "Airbus Helicopters",
                    AircraftCategory.Helicopter,
                    new[] { "H225", "EC225" },
                    145, 70, 60, 0, 85, 700, 20000,
                    true, false, true, false, 90),

                Profile(
                    "Boeing CH-47 Chinook",
                    "Boeing",
                    AircraftCategory.Helicopter,
                    new[] { "CH-47", "CH47", "Chinook" },
                    160, 75, 65, 0, 95, 800, 20000,
                    true, false, true, false, 90),

                // ---------------------------------------------------------
                // GLIDERS / E-VTOL / SPECIAL
                // ---------------------------------------------------------
                Profile(
                    "DG Aviation LS8-18",
                    "DG Aviation",
                    AircraftCategory.Glider,
                    new[] { "LS8-18", "LS8" },
                    80, 55, 50, 0, 60, 300, 18000,
                    false, false, false, false, 90),

                Profile(
                    "Joby Aviation S4",
                    "Joby",
                    AircraftCategory.Experimental,
                    new[] { "Joby", "S4" },
                    175, 60, 50, 0, 90, 600, 15000,
                    true, true, false, false, 70),

                Profile(
                    "Jetson ONE",
                    "Jetson",
                    AircraftCategory.Ultralight,
                    new[] { "Jetson ONE", "Jetson" },
                    55, 30, 25, 0, 40, 300, 5000,
                    false, false, false, false, 90),

                Profile(
                    "Boom XB-1",
                    "Boom Supersonic",
                    AircraftCategory.Experimental,
                    new[] { "XB-1", "Boom" },
                    900, 170, 160, 155, 230, 2500, 50000,
                    true, true, true, true, 90),

                // ---------------------------------------------------------
                // VINTAGE / CLASSICS
                // ---------------------------------------------------------
                Profile(
                    "Douglas DC-3",
                    "Douglas",
                    AircraftCategory.Vintage,
                    new[] { "Douglas DC-3", "DC-3", "DC3" },
                    180, 90, 85, 80, 105, 700, 23000,
                    true, false, true, false, 90),

                Profile(
                    "Ford 4-AT Trimotor",
                    "Ford",
                    AircraftCategory.Vintage,
                    new[] { "Trimotor", "4-AT", "Ford 4" },
                    110, 70, 65, 60, 80, 550, 12000,
                    false, false, true, false, 90),

                Profile(
                    "Antonov An-2",
                    "Antonov",
                    AircraftCategory.Vintage,
                    new[] { "Antonov An-2", "AN-2", "An2" },
                    105, 65, 60, 55, 75, 500, 14500,
                    false, false, false, false, 90),

                Profile(
                    "Antonov An-225",
                    "Antonov",
                    AircraftCategory.Cargo,
                    new[] { "AN-225", "An-225", "Mriya" },
                    460, 165, 155, 155, 190, 1800, 36000,
                    true, false, true, true, 110)
            };
        }

        private static AircraftProfile Profile(
            string name,
            string manufacturer,
            AircraftCategory category,
            string[] matchTerms,
            int cruiseSpeed,
            int approachSpeed,
            int finalSpeed,
            int rotateSpeed,
            int climbSpeed,
            int recommendedDescentRate,
            int typicalCruiseAltitude,
            bool hasAutopilot,
            bool hasVnav,
            bool hasRetractableGear,
            bool isPressurized,
            int matchPriority)
        {
            return new AircraftProfile
            {
                Name = name,
                Manufacturer = manufacturer,
                Category = category,
                MatchTerms = matchTerms,
                CruiseSpeed = cruiseSpeed,
                ApproachSpeed = approachSpeed,
                FinalSpeed = finalSpeed,
                RotateSpeed = rotateSpeed,
                ClimbSpeed = climbSpeed,
                RecommendedDescentRate = recommendedDescentRate,
                TypicalCruiseAltitude = typicalCruiseAltitude,
                HasAutopilot = hasAutopilot,
                HasVnav = hasVnav,
                HasRetractableGear = hasRetractableGear,
                IsPressurized = isPressurized,
                MatchPriority = matchPriority
            };
        }
    }
}