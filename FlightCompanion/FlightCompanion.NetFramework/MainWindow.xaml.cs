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

            windowSource.AddHook(
                WindowMessageHook);

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

            AltitudeText.Text =
                string.Format(
                    "{0:N0} ft",
                    flightData.AltitudeFeet);

            SpeedText.Text =
                string.Format(
                    "{0:N0} kt",
                    flightData.GroundSpeedKnots);

            VSText.Text =
                string.Format(
                    "{0:+0;-0;0} ft/min",
                    flightData.VerticalSpeedFeetPerMinute);

            HeadingText.Text =
                string.Format(
                    "{0:000}°",
                    flightData.HeadingDegrees);

            AircraftText.Text =
                string.IsNullOrWhiteSpace(
                    flightData.AircraftTitle)
                    ? "Avion : non identifié"
                    : "Avion : " +
                      flightData.AircraftTitle;

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
                    "Profil : {0} | Approche : {1} kt | Descente : {2} ft/min",
                    currentAircraftProfile.Name,
                    currentAircraftProfile.ApproachSpeed,
                    currentAircraftProfile
                        .RecommendedDescentRate);

            UpdateDescentCalculation(false);
        }

        private void DescentInput_TextChanged(
            object sender,
            System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
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
                RecommendedVsText.Text =
                    "--- ft/min";

                TodDistanceText.Text =
                    "--- NM";

                TodTimeText.Text =
                    "--- min";

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
                RecommendedVsText.Text =
                    "--- ft/min";

                TodDistanceText.Text =
                    "--- NM";

                TodTimeText.Text =
                    "--- min";

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
                currentFlightData.GroundSpeedKnots;

            if (groundSpeed < 1)
            {
                RecommendedVsText.Text =
                    "--- ft/min";

                TodDistanceText.Text =
                    "--- NM";

                TodTimeText.Text =
                    "--- min";

                DescentAdviceText.Text =
                    "Vitesse sol insuffisante";

                DescentAdviceText.Foreground =
                    Brushes.Orange;

                return;
            }

            double currentAltitude =
                currentFlightData.AltitudeFeet;

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
                        "{0:0.0} NM",
                        distanceBeforeTod);

                TodTimeText.Text =
                    string.Format(
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