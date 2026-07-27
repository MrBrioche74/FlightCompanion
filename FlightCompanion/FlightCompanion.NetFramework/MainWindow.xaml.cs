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

        public MainWindow()
        {
            InitializeComponent();

            SourceInitialized += MainWindow_SourceInitialized;
            Closed += MainWindow_Closed;
        }

        private void MainWindow_SourceInitialized(
            object sender,
            EventArgs e)
        {
            IntPtr windowHandle =
                new WindowInteropHelper(this).Handle;

            windowSource =
                HwndSource.FromHwnd(windowHandle);

            windowSource.AddHook(WindowMessageHook);

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

            StatusText.Text = "● Connexion à MSFS...";
            StatusText.Foreground = Brushes.Orange;

            simConnectService.Connect(windowHandle);
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
                simConnectService.ReceiveMessage();
                handled = true;
            }

            return IntPtr.Zero;
        }

        private void SimConnectService_Connected()
        {
            StatusText.Text = "● MSFS CONNECTÉ";
            StatusText.Foreground = Brushes.Lime;
        }

        private void SimConnectService_Disconnected(
            string message)
        {
            StatusText.Text =
                "● NON CONNECTÉ — " + message;

            StatusText.Foreground = Brushes.Orange;

            AltitudeText.Text = "----- ft";
            SpeedText.Text = "----- kt";
            VSText.Text = "----- ft/min";
            HeadingText.Text = "---°";

            RecommendedVsText.Text = "--- ft/min";
            DescentAdviceText.Text =
                "En attente de MSFS";

            DescentAdviceText.Foreground =
                Brushes.Orange;

            hasReceivedFlightData = false;
        }

        private void SimConnectService_Error(
            string message)
        {
            StatusText.Text = message;
            StatusText.Foreground = Brushes.Red;
        }

        private void SimConnectService_FlightDataReceived(
            FlightData flightData)
        {
            currentFlightData = flightData;
            hasReceivedFlightData = true;

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

            UpdateDescentCalculation(false);
        }

        private void CalculateButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            UpdateDescentCalculation(true);
        }

        private void UpdateDescentCalculation(
            bool showInputErrors)
        {
            if (!hasReceivedFlightData)
            {
                RecommendedVsText.Text =
                    "--- ft/min";

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
                if (showInputErrors)
                {
                    DescentAdviceText.Text =
                        "Distance incorrecte";

                    DescentAdviceText.Foreground =
                        Brushes.Red;
                }

                return;
            }

            if (currentFlightData.GroundSpeedKnots < 1)
            {
                RecommendedVsText.Text =
                    "--- ft/min";

                DescentAdviceText.Text =
                    "Vitesse sol insuffisante";

                DescentAdviceText.Foreground =
                    Brushes.Orange;

                return;
            }

            double recommendedVs =
                DescentCalculator.CalculateVerticalSpeed(
                    currentFlightData.AltitudeFeet,
                    targetAltitude,
                    distanceNm,
                    currentFlightData.GroundSpeedKnots);

            RecommendedVsText.Text =
                string.Format(
                    "{0:+0;-0;0} ft/min",
                    recommendedVs);

            double altitudeToLose =
                currentFlightData.AltitudeFeet -
                targetAltitude;

            double requiredSlope =
                Math.Atan2(
                    altitudeToLose,
                    distanceNm * 6076.12) *
                180.0 /
                Math.PI;

            if (altitudeToLose < 0)
            {
                DescentAdviceText.Text =
                    "Une montée est nécessaire";

                DescentAdviceText.Foreground =
                    Brushes.Orange;
            }
            else if (requiredSlope < 1.5)
            {
                DescentAdviceText.Text =
                    "Attendre avant de descendre";

                DescentAdviceText.Foreground =
                    Brushes.Orange;
            }
            else if (requiredSlope <= 3.5)
            {
                DescentAdviceText.Text =
                    "Commencer la descente";

                DescentAdviceText.Foreground =
                    Brushes.Lime;
            }
            else
            {
                DescentAdviceText.Text =
                    "Descente forte requise";

                DescentAdviceText.Foreground =
                    Brushes.Red;
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