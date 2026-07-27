using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.FlightSimulator.SimConnect;

namespace FlightCompanion
{
    public partial class MainWindow : Window
    {
        private const int WmUserSimConnect = 0x0402;

        private SimConnect? _simConnect;
        private HwndSource? _windowSource;

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

        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            IntPtr windowHandle = new WindowInteropHelper(this).Handle;

            _windowSource = HwndSource.FromHwnd(windowHandle);
            _windowSource?.AddHook(WindowMessageHook);

            ConnectToSimulator(windowHandle);
        }

        private void ConnectToSimulator(IntPtr windowHandle)
        {
            try
            {
                StatusText.Text = "◯ Connexion à MSFS...";
                StatusText.Foreground = Brushes.Orange;

                _simConnect = new SimConnect(
                    "Flight Companion",
                    windowHandle,
                    WmUserSimConnect,
                    null,
                    0);

                _simConnect.OnRecvOpen += SimConnect_OnRecvOpen;
                _simConnect.OnRecvQuit += SimConnect_OnRecvQuit;
                _simConnect.OnRecvException += SimConnect_OnRecvException;
                _simConnect.OnRecvSimobjectData += SimConnect_OnRecvSimobjectData;
            }
            catch (Exception exception)
            {
                ShowDisconnected($"MSFS introuvable : {exception.Message}");
            }
        }

        private IntPtr WindowMessageHook(
            IntPtr hwnd,
            int message,
            IntPtr wParam,
            IntPtr lParam,
            ref bool handled)
        {
            if (message == WmUserSimConnect && _simConnect is not null)
            {
                try
                {
                    _simConnect.ReceiveMessage();
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
            StatusText.Text = "● MSFS connecté";
            StatusText.Foreground = Brushes.Lime;

            ConfigureFlightData();
        }

        private void ConfigureFlightData()
        {
            if (_simConnect is null)
            {
                return;
            }

            _simConnect.AddToDataDefinition(
                Definitions.FlightData,
                "INDICATED ALTITUDE",
                "feet",
                SIMCONNECT_DATATYPE.FLOAT64,
                0,
                SimConnect.SIMCONNECT_UNUSED);

            _simConnect.AddToDataDefinition(
                Definitions.FlightData,
                "GROUND VELOCITY",
                "knots",
                SIMCONNECT_DATATYPE.FLOAT64,
                0,
                SimConnect.SIMCONNECT_UNUSED);

            _simConnect.AddToDataDefinition(
                Definitions.FlightData,
                "VERTICAL SPEED",
                "feet per second",
                SIMCONNECT_DATATYPE.FLOAT64,
                0,
                SimConnect.SIMCONNECT_UNUSED);

            _simConnect.AddToDataDefinition(
                Definitions.FlightData,
                "PLANE HEADING DEGREES TRUE",
                "radians",
                SIMCONNECT_DATATYPE.FLOAT64,
                0,
                SimConnect.SIMCONNECT_UNUSED);

            _simConnect.RegisterDataDefineStruct<FlightData>(
                Definitions.FlightData);

            _simConnect.RequestDataOnSimObject(
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
            if ((Requests)data.dwRequestID != Requests.FlightData ||
                data.dwData.Length == 0)
            {
                return;
            }

            FlightData flightData = (FlightData)data.dwData[0];

            double verticalSpeedFeetPerMinute =
                flightData.VerticalSpeedFeetPerSecond * 60.0;

            double headingDegrees =
                flightData.HeadingRadians * 180.0 / Math.PI;

            headingDegrees = (headingDegrees + 360.0) % 360.0;

            AltitudeText.Text =
                $"{flightData.AltitudeFeet:N0} ft";

            SpeedText.Text =
                $"{flightData.GroundSpeedKnots:N0} kt";

            VSText.Text =
                $"{verticalSpeedFeetPerMinute:+0;-0;0} ft/min";

            HeadingText.Text =
                $"{headingDegrees:000}°";
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
                $"Erreur SimConnect : {data.dwException}";

            StatusText.Foreground = Brushes.Red;
        }

        private void ShowDisconnected(string message)
        {
            StatusText.Text = $"◯ Non connecté — {message}";
            StatusText.Foreground = Brushes.Orange;

            AltitudeText.Text = "----- ft";
            SpeedText.Text = "----- kt";
            VSText.Text = "----- ft/min";
            HeadingText.Text = "---°";

            Disconnect();
        }

        private void Disconnect()
        {
            try
            {
                _simConnect?.Dispose();
            }
            catch
            {
                // Rien à faire pendant la fermeture.
            }

            _simConnect = null;
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            _windowSource?.RemoveHook(WindowMessageHook);
            Disconnect();
        }
    }
}