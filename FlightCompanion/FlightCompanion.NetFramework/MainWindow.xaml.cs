using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.FlightSimulator.SimConnect;
using FlightCompanion.NetFramework.Calculators;

namespace FlightCompanion.NetFramework
{
    public partial class MainWindow : Window
    {
        private const int WM_USER_SIMCONNECT = 0x0402;

        private SimConnect simConnect;
        private HwndSource windowSource;

        private FlightData currentFlightData;
        private bool hasReceivedFlightData;

        private enum Definitions
        {
            FlightData
        }

        private enum Requests
        {
            FlightData
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct FlightData
        {
            public double AltitudeFeet;
            public double GroundSpeedKnots;
            public double VerticalSpeedFeetPerSecond;
            public double HeadingRadians;
        }

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

            windowSource = HwndSource.FromHwnd(windowHandle);
            windowSource.AddHook(WindowMessageHook);

            ConnectToMsfs(windowHandle);
        }

        private void ConnectToMsfs(IntPtr windowHandle)
        {
            try
            {
                StatusText.Text = "● Connexion à MSFS...";
                StatusText.Foreground = Brushes.Orange;

                simConnect = new SimConnect(
                    "Flight Companion",
                    windowHandle,
                    WM_USER_SIMCONNECT,
                    null,
                    0);

                simConnect.OnRecvOpen += SimConnect_OnRecvOpen;
                simConnect.OnRecvQuit += SimConnect_OnRecvQuit;
                simConnect.OnRecvException +=
                    SimConnect_OnRecvException;
                simConnect.OnRecvSimobjectData +=
                    SimConnect_OnRecvSimobjectData;
            }
            catch (Exception exception)
            {
                ShowDisconnected(exception.Message);
            }
        }

        private IntPtr WindowMessageHook(
            IntPtr hwnd,
            int message,
            IntPtr wParam,
            IntPtr lParam,
            ref bool handled)
        {
            if (message == WM_USER_SIMCONNECT &&
                simConnect != null)
            {
                try
                {
                    simConnect.ReceiveMessage();
                }
                catch (Exception exception)
                {
                    ShowDisconnected(exception.Message);
                }

                handled = true;
            }

            return IntPtr.Zero;
        }

        private void SimConnect_OnRecvOpen(
            SimConnect sender,
            SIMCONNECT_RECV_OPEN data)
        {
            StatusText.Text = "● MSFS CONNECTÉ";
            StatusText.Foreground = Brushes.Lime;

            ConfigureFlightData();
        }

        private void ConfigureFlightData()
        {
            simConnect.AddToDataDefinition(
                Definitions.FlightData,
                "INDICATED ALTITUDE",
                "feet",
                SIMCONNECT_DATATYPE.FLOAT64,
                0,
                SimConnect.SIMCONNECT_UNUSED);

            simConnect.AddToDataDefinition(
                Definitions.FlightData,
                "GROUND VELOCITY",
                "knots",
                SIMCONNECT_DATATYPE.FLOAT64,
                0,
                SimConnect.SIMCONNECT_UNUSED);

            simConnect.AddToDataDefinition(
                Definitions.FlightData,
                "VERTICAL SPEED",
                "feet per second",
                SIMCONNECT_DATATYPE.FLOAT64,
                0,
                SimConnect.SIMCONNECT_UNUSED);

            simConnect.AddToDataDefinition(
                Definitions.FlightData,
                "PLANE HEADING DEGREES TRUE",
                "radians",
                SIMCONNECT_DATATYPE.FLOAT64,
                0,
                SimConnect.SIMCONNECT_UNUSED);

            simConnect.RegisterDataDefineStruct<FlightData>(
                Definitions.FlightData);

            simConnect.RequestDataOnSimObject(
                Requests.FlightData,
                Definitions.FlightData,
                SimConnect.SIMCONNECT_OBJECT_ID_USER,
                SIMCONNECT_PERIOD.SECOND,
                SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT,
                0,
                0,
                0);
        }

        private void SimConnect_OnRecvSimobjectData(
            SimConnect sender,
            SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            if ((Requests)data.dwRequestID !=
                    Requests.FlightData ||
                data.dwData.Length == 0)
            {
                return;
            }

            currentFlightData =
                (FlightData)data.dwData[0];

            hasReceivedFlightData = true;

            double verticalSpeedFeetPerMinute =
                currentFlightData.VerticalSpeedFeetPerSecond * 60.0;

            double headingDegrees =
                currentFlightData.HeadingRadians * 180.0 / Math.PI;

            headingDegrees =
                (headingDegrees + 360.0) % 360.0;

            AltitudeText.Text =
                string.Format(
                    "{0:N0} ft",
                    currentFlightData.AltitudeFeet);

            SpeedText.Text =
                string.Format(
                    "{0:N0} kt",
                    currentFlightData.GroundSpeedKnots);

            VSText.Text =
                string.Format(
                    "{0:+0;-0;0} ft/min",
                    verticalSpeedFeetPerMinute);

            HeadingText.Text =
                string.Format(
                    "{0:000}°",
                    headingDegrees);

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
                RecommendedVsText.Text = "--- ft/min";
                DescentAdviceText.Text =
                    "En attente des données de MSFS";

                DescentAdviceText.Foreground = Brushes.Orange;
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

                    DescentAdviceText.Foreground = Brushes.Red;
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

                    DescentAdviceText.Foreground = Brushes.Red;
                }

                return;
            }

            double groundSpeed =
                currentFlightData.GroundSpeedKnots;

            if (groundSpeed < 1)
            {
                RecommendedVsText.Text = "--- ft/min";
                DescentAdviceText.Text =
                    "Vitesse sol insuffisante";

                DescentAdviceText.Foreground = Brushes.Orange;
                return;
            }

            double recommendedVs =
                DescentCalculator.CalculateVerticalSpeed(
                    currentFlightData.AltitudeFeet,
                    targetAltitude,
                    distanceNm,
                    groundSpeed);

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

        private void SimConnect_OnRecvQuit(
            SimConnect sender,
            SIMCONNECT_RECV data)
        {
            ShowDisconnected("MSFS a été fermé.");
        }

        private void SimConnect_OnRecvException(
            SimConnect sender,
            SIMCONNECT_RECV_EXCEPTION data)
        {
            StatusText.Text =
                "Erreur SimConnect : " +
                data.dwException;

            StatusText.Foreground = Brushes.Red;
        }

        private void ShowDisconnected(string message)
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

            Disconnect();
        }

        private void Disconnect()
        {
            if (simConnect == null)
            {
                return;
            }

            try
            {
                simConnect.Dispose();
            }
            catch
            {
                // Ignorer les erreurs pendant la fermeture.
            }

            simConnect = null;
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

            Disconnect();
        }
    }
}