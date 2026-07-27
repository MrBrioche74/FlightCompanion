using System;
using System.Globalization;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using FlightCompanion.NetFramework.Calculators;
using FlightCompanion.NetFramework.Models;
using FlightCompanion.NetFramework.Services;

namespace FlightCompanion.NetFramework
{
    public partial class MainWindow : Window
    {
        private SimConnectService simConnectService;
        private HwndSource windowSource;

        private FlightData currentFlightData;
        private bool hasReceivedFlightData;

        private AircraftProfileService aircraftProfileService;
        private AircraftProfile currentAircraftProfile;

        /*
         * Empêche l'événement TextChanged de relancer inutilement
         * le calcul lorsque l'application remplit la distance GPS.
         */
        private bool isUpdatingGpsDistance;

        public MainWindow()
        {
            InitializeComponent();

            aircraftProfileService =
                new AircraftProfileService();

            SourceInitialized +=
                MainWindow_SourceInitialized;

            Closed +=
                MainWindow_Closed;
        }

        private void MainWindow_SourceInitialized(
            object sender,
            EventArgs e)
        {
            IntPtr windowHandle =
                new WindowInteropHelper(this).Handle;

            windowSource =
                HwndSource.FromHwnd(windowHandle);

            if (windowSource != null)
            {
                windowSource.AddHook(
                    WindowMessageHook);
            }

            simConnectService =
                new SimConnectService();

            simConnectService.Connected +=
                SimConnectService_Connected;

            simConnectService.Disconnected +=
                SimConnectService_Disconnected;

            simConnectService.Error +=
                SimConnectService_Error;

            simConnectService.FlightDataReceived +=
                SimConnectService_FlightDataReceived;

            StatusText.Text =
                "● Connexion à MSFS...";

            StatusText.Foreground =
                Brushes.Orange;

            simConnectService.Connect(
                windowHandle);
        }

        private IntPtr WindowMessageHook(
            IntPtr hwnd,
            int message,
            IntPtr wParam,
            IntPtr lParam,
            ref bool handled)
        {
            if (message ==
                SimConnectService.WindowMessageId)
            {
                if (simConnectService != null)
                {
                    simConnectService.ReceiveMessage();
                }

                handled = true;
            }

            return IntPtr.Zero;
        }

        private void SimConnectService_Connected()
        {
            StatusText.Text =
                "● MSFS CONNECTÉ";

            StatusText.Foreground =
                Brushes.Lime;
        }

        private void SimConnectService_Disconnected(
            string message)
        {
            StatusText.Text =
                "● NON CONNECTÉ — " + message;

            StatusText.Foreground =
                Brushes.Orange;

            AltitudeText.Text =
                "----- ft";

            SpeedText.Text =
                "----- kt";

            VSText.Text =
                "----- ft/min";

            HeadingText.Text =
                "---°";

            AircraftText.Text =
                "Avion : ---";

            AircraftProfileText.Text =
                "Profil : ---";

            FlightPlanStatusText.Text =
                "Plan actif : ---";

            WaypointText.Text =
                "Waypoint : ---";

            WaypointProgressText.Text =
                "Progression : --- / ---";

            GpsDistanceText.Text =
                "Distance restante : --- NM";

            NextWaypointDistanceText.Text =
                "Prochain waypoint : --- NM";

            GpsTimeText.Text =
                "Temps restant : ---";

            GpsTodStatusText.Text =
                "En attente...";

            GpsTodStatusText.Foreground =
                Brushes.Orange;

            GpsSourceText.Text =
                "Source : saisie manuelle";

            RecommendedVsText.Text =
                "--- ft/min";

            TodDistanceText.Text =
                "--- NM";

            TodTimeText.Text =
                "--- min";

            DescentAdviceText.Text =
                "En attente de MSFS";

            DescentAdviceText.Foreground =
                Brushes.Orange;

            currentAircraftProfile = null;
            hasReceivedFlightData = false;

            DistanceTextBox.IsReadOnly = false;
        }

        private void SimConnectService_Error(
            string message)
        {
            StatusText.Text =
                message;

            StatusText.Foreground =
                Brushes.Red;
        }

        private void SimConnectService_FlightDataReceived(
            FlightData flightData)
        {
            currentFlightData =
                flightData;

            hasReceivedFlightData =
                true;

            UpdateFlightDisplay(
                flightData);

            UpdateAircraftProfile(
                flightData);

            UpdateGpsDisplay(
                flightData);

            ApplyGpsDistanceIfEnabled(
                flightData);

            UpdateDescentCalculation(false);
        }

        private void UpdateFlightDisplay(
            FlightData flightData)
        {
            AltitudeText.Text =
                string.Format(
                    CultureInfo.CurrentCulture,
                    "{0:N0} ft",
                    flightData.AltitudeFeet);

            SpeedText.Text =
                string.Format(
                    CultureInfo.CurrentCulture,
                    "{0:N0} kt",
                    flightData.GroundSpeedKnots);

            VSText.Text =
                string.Format(
                    CultureInfo.CurrentCulture,
                    "{0:+0;-0;0} ft/min",
                    flightData.VerticalSpeedFeetPerMinute);

            HeadingText.Text =
                string.Format(
                    CultureInfo.CurrentCulture,
                    "{0:000}°",
                    flightData.HeadingDegrees);

            AircraftText.Text =
                string.IsNullOrWhiteSpace(
                    flightData.AircraftTitle)
                    ? "Avion : non identifié"
                    : "Avion : " +
                      flightData.AircraftTitle;
        }

        private void UpdateAircraftProfile(
            FlightData flightData)
        {
            currentAircraftProfile =
                aircraftProfileService.FindProfile(
                    flightData.AircraftTitle);

            if (currentAircraftProfile == null)
            {
                currentAircraftProfile =
                    aircraftProfileService
                        .GetDefaultProfile();
            }

            AircraftProfileText.Text =
                string.Format(
                    CultureInfo.CurrentCulture,
                    "Profil : {0} | Approche : {1} kt | Descente : {2} ft/min",
                    currentAircraftProfile.Name,
                    currentAircraftProfile.ApproachSpeed,
                    currentAircraftProfile
                        .RecommendedDescentRate);
        }

        private void UpdateGpsDisplay(
            FlightData flightData)
        {
            FlightPlanStatusText.Text =
                flightData.HasActiveFlightPlan
                    ? "Plan actif : OUI"
                    : "Plan actif : NON";

            FlightPlanStatusText.Foreground =
                flightData.HasActiveFlightPlan
                    ? Brushes.Lime
                    : Brushes.Orange;

            WaypointText.Text =
                string.IsNullOrWhiteSpace(
                    flightData.NextWaypointId)
                    ? "Waypoint : ---"
                    : "Waypoint : " +
                      flightData.NextWaypointId;

            if (flightData.WaypointCount > 0)
            {
                WaypointProgressText.Text =
                    string.Format(
                        CultureInfo.CurrentCulture,
                        "Progression : {0} / {1}",
                        flightData.ActiveWaypointIndex + 1,
                        flightData.WaypointCount);
            }
            else
            {
                WaypointProgressText.Text =
                    "Progression : --- / ---";
            }

            if (flightData.HasActiveFlightPlan &&
                flightData.GpsDistanceRemainingNm > 0)
            {
                GpsDistanceText.Text =
                    string.Format(
                        CultureInfo.CurrentCulture,
                        "Distance restante : {0:0.0} NM",
                        flightData.GpsDistanceRemainingNm);
            }
            else
            {
                GpsDistanceText.Text =
                    "Distance restante : --- NM";
            }

            if (flightData.NextWaypointDistanceNm > 0)
            {
                NextWaypointDistanceText.Text =
                    string.Format(
                        CultureInfo.CurrentCulture,
                        "Prochain waypoint : {0:0.0} NM",
                        flightData.NextWaypointDistanceNm);
            }
            else
            {
                NextWaypointDistanceText.Text =
                    "Prochain waypoint : --- NM";
            }

            GpsTimeText.Text =
                FormatGpsTime(
                    flightData.GpsEteSeconds);

            UpdateGpsTodStatus(
                flightData);
        }

        private string FormatGpsTime(
            double totalSeconds)
        {
            if (totalSeconds <= 0)
            {
                return "Temps restant : ---";
            }

            TimeSpan remainingTime =
                TimeSpan.FromSeconds(
                    totalSeconds);

            if (remainingTime.TotalHours >= 1)
            {
                return string.Format(
                    CultureInfo.CurrentCulture,
                    "Temps restant : {0:00} h {1:00} min",
                    (int)remainingTime.TotalHours,
                    remainingTime.Minutes);
            }

            return string.Format(
                CultureInfo.CurrentCulture,
                "Temps restant : {0:00} min {1:00} s",
                remainingTime.Minutes,
                remainingTime.Seconds);
        }

        private void UpdateGpsTodStatus(
            FlightData flightData)
        {
            double targetAltitude;

            if (!flightData.HasActiveFlightPlan ||
                flightData.GpsDistanceRemainingNm <= 0)
            {
                GpsTodStatusText.Text =
                    "Aucune distance GPS";

                GpsTodStatusText.Foreground =
                    Brushes.Orange;

                return;
            }

            if (!TryReadNumber(
                    TargetAltitudeTextBox.Text,
                    out targetAltitude))
            {
                GpsTodStatusText.Text =
                    "Altitude cible incorrecte";

                GpsTodStatusText.Foreground =
                    Brushes.Red;

                return;
            }

            double altitudeToLose =
                flightData.AltitudeFeet -
                targetAltitude;

            if (altitudeToLose <= 0)
            {
                GpsTodStatusText.Text =
                    "Aucune descente nécessaire";

                GpsTodStatusText.Foreground =
                    Brushes.Orange;

                return;
            }

            double distanceBeforeTod =
                TodCalculator
                    .CalculateDistanceBeforeTod(
                        flightData.AltitudeFeet,
                        targetAltitude,
                        flightData
                            .GpsDistanceRemainingNm);

            if (distanceBeforeTod > 0)
            {
                GpsTodStatusText.Text =
                    string.Format(
                        CultureInfo.CurrentCulture,
                        "TOD dans {0:0.0} NM",
                        distanceBeforeTod);

                GpsTodStatusText.Foreground =
                    Brushes.Orange;
            }
            else
            {
                GpsTodStatusText.Text =
                    "DESCENDRE MAINTENANT";

                GpsTodStatusText.Foreground =
                    Brushes.Lime;
            }
        }

        private void ApplyGpsDistanceIfEnabled(
            FlightData flightData)
        {
            bool useGpsDistance =
                UseGpsDistanceCheckBox.IsChecked ==
                true;

            DistanceTextBox.IsReadOnly =
                useGpsDistance;

            if (!useGpsDistance)
            {
                GpsSourceText.Text =
                    "Source : saisie manuelle";

                return;
            }

            if (!flightData.HasActiveFlightPlan ||
                flightData.GpsDistanceRemainingNm <= 0)
            {
                GpsSourceText.Text =
                    "Source : GPS indisponible";

                GpsSourceText.Foreground =
                    Brushes.Orange;

                return;
            }

            GpsSourceText.Text =
                "Source : distance GPS automatique";

            GpsSourceText.Foreground =
                Brushes.Lime;

            string gpsDistance =
                flightData.GpsDistanceRemainingNm
                    .ToString(
                        "0.0",
                        CultureInfo.CurrentCulture);

            if (DistanceTextBox.Text ==
                gpsDistance)
            {
                return;
            }

            isUpdatingGpsDistance =
                true;

            DistanceTextBox.Text =
                gpsDistance;

            isUpdatingGpsDistance =
                false;
        }

        private void UseGpsDistanceCheckBox_Changed(
            object sender,
            RoutedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            bool useGpsDistance =
                UseGpsDistanceCheckBox.IsChecked ==
                true;

            DistanceTextBox.IsReadOnly =
                useGpsDistance;

            if (useGpsDistance &&
                hasReceivedFlightData)
            {
                ApplyGpsDistanceIfEnabled(
                    currentFlightData);
            }
            else
            {
                GpsSourceText.Text =
                    "Source : saisie manuelle";

                GpsSourceText.Foreground =
                    Brushes.Gray;
            }

            UpdateDescentCalculation(false);
        }

        private void DescentInput_TextChanged(
            object sender,
            System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!IsLoaded ||
                isUpdatingGpsDistance)
            {
                return;
            }

            if (hasReceivedFlightData)
            {
                UpdateGpsTodStatus(
                    currentFlightData);
            }

            UpdateDescentCalculation(false);
        }

        private void UpdateDescentCalculation(
            bool showInputErrors)
        {
            if (!hasReceivedFlightData)
            {
                RecommendedVsText.Text =
                    "--- ft/min";

                TodDistanceText.Text =
                    "--- NM";

                TodTimeText.Text =
                    "--- min";

                DescentAdviceText.Text =
                    "En attente des données de MSFS";

                DescentAdviceText.Foreground =
                    Brushes.Orange;

                return;
            }

            double targetAltitude;
            double distanceNm;

            if (!TryReadNumber(
                    TargetAltitudeTextBox.Text,
                    out targetAltitude))
            {
                ClearDescentResults();

                if (showInputErrors)
                {
                    DescentAdviceText.Text =
                        "Altitude cible incorrecte";

                    DescentAdviceText.Foreground =
                        Brushes.Red;
                }

                return;
            }

            if (!TryReadNumber(
                    DistanceTextBox.Text,
                    out distanceNm) ||
                distanceNm <= 0)
            {
                ClearDescentResults();

                if (showInputErrors)
                {
                    DescentAdviceText.Text =
                        "Distance incorrecte";

                    DescentAdviceText.Foreground =
                        Brushes.Red;
                }

                return;
            }

            double groundSpeed =
                currentFlightData
                    .GroundSpeedKnots;

            if (groundSpeed < 1)
            {
                ClearDescentResults();

                DescentAdviceText.Text =
                    "Vitesse sol insuffisante";

                DescentAdviceText.Foreground =
                    Brushes.Orange;

                return;
            }

            double currentAltitude =
                currentFlightData
                    .AltitudeFeet;

            double altitudeToLose =
                currentAltitude -
                targetAltitude;

            double recommendedVs =
                DescentCalculator
                    .CalculateVerticalSpeed(
                        currentAltitude,
                        targetAltitude,
                        distanceNm,
                        groundSpeed);

            RecommendedVsText.Text =
                string.Format(
                    CultureInfo.CurrentCulture,
                    "{0:+0;-0;0} ft/min",
                    recommendedVs);

            double requiredDescentDistance =
                TodCalculator
                    .CalculateRequiredDescentDistance(
                        currentAltitude,
                        targetAltitude);

            double distanceBeforeTod =
                TodCalculator
                    .CalculateDistanceBeforeTod(
                        currentAltitude,
                        targetAltitude,
                        distanceNm);

            TimeSpan timeBeforeTod =
                TodCalculator
                    .CalculateTimeBeforeTod(
                        distanceBeforeTod,
                        groundSpeed);

            if (altitudeToLose <= 0)
            {
                TodDistanceText.Text =
                    "--- NM";

                TodTimeText.Text =
                    "--- min";

                DescentAdviceText.Text =
                    "Une montée est nécessaire";

                DescentAdviceText.Foreground =
                    Brushes.Orange;

                return;
            }

            if (distanceBeforeTod > 0)
            {
                TodDistanceText.Text =
                    string.Format(
                        CultureInfo.CurrentCulture,
                        "{0:0.0} NM",
                        distanceBeforeTod);

                TodTimeText.Text =
                    string.Format(
                        CultureInfo.CurrentCulture,
                        "{0} min {1:00} s",
                        (int)timeBeforeTod
                            .TotalMinutes,
                        timeBeforeTod.Seconds);

                DescentAdviceText.Text =
                    "Attendre avant de descendre";

                DescentAdviceText.Foreground =
                    Brushes.Orange;
            }
            else
            {
                TodDistanceText.Text =
                    "MAINTENANT";

                TodTimeText.Text =
                    string.Format(
                        CultureInfo.CurrentCulture,
                        "Distance nécessaire : {0:0.0} NM",
                        requiredDescentDistance);

                double requiredSlope =
                    Math.Atan2(
                        altitudeToLose,
                        distanceNm * 6076.12) *
                    180.0 /
                    Math.PI;

                if (requiredSlope <= 3.5)
                {
                    DescentAdviceText.Text =
                        "COMMENCER LA DESCENTE";

                    DescentAdviceText.Foreground =
                        Brushes.Lime;
                }
                else
                {
                    DescentAdviceText.Text =
                        "DESCENTE FORTE REQUISE";

                    DescentAdviceText.Foreground =
                        Brushes.Red;
                }
            }
        }

        private void ClearDescentResults()
        {
            RecommendedVsText.Text =
                "--- ft/min";

            TodDistanceText.Text =
                "--- NM";

            TodTimeText.Text =
                "--- min";
        }

        private bool TryReadNumber(
            string text,
            out double value)
        {
            if (double.TryParse(
                    text,
                    NumberStyles.Float,
                    CultureInfo.CurrentCulture,
                    out value))
            {
                return true;
            }

            string normalizedText =
                text.Replace(',', '.');

            return double.TryParse(
                normalizedText,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value);
        }

        private void MainWindow_Closed(
            object sender,
            EventArgs e)
        {
            if (windowSource != null)
            {
                windowSource.RemoveHook(
                    WindowMessageHook);
            }

            if (simConnectService != null)
            {
                simConnectService.Dispose();
            }
        }
    }
}