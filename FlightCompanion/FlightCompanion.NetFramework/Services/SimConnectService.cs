using System;
using System.Runtime.InteropServices;
using Microsoft.FlightSimulator.SimConnect;
using FlightCompanion.NetFramework.Models;

namespace FlightCompanion.NetFramework.Services
{
    public class SimConnectService : IDisposable
    {
        public const int WindowMessageId = 0x0402;

        private const double MetersPerNauticalMile = 1852.0;

        private SimConnect simConnect;

        private string currentAircraftTitle = string.Empty;
        private string currentNextWaypointId = string.Empty;

        private RawNavigationData currentNavigationData;

        public event Action Connected;
        public event Action<string> Disconnected;
        public event Action<string> Error;
        public event Action<FlightData> FlightDataReceived;

        private enum Definitions
        {
            FlightData,
            AircraftTitle,
            NavigationData,
            NextWaypointId
        }

        private enum Requests
        {
            FlightData,
            AircraftTitle,
            NavigationData,
            NextWaypointId
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct RawFlightData
        {
            public double AltitudeFeet;
            public double GroundSpeedKnots;
            public double VerticalSpeedFeetPerSecond;
            public double HeadingRadians;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct RawNavigationData
        {
            public int HasActiveFlightPlan;
            public double EteSeconds;
            public double NextWaypointDistanceMeters;
            public int ActiveWaypointIndex;
            public int WaypointCount;
        }

        [StructLayout(
            LayoutKind.Sequential,
            CharSet = CharSet.Ansi,
            Pack = 1)]
        private struct RawAircraftTitle
        {
            [MarshalAs(
                UnmanagedType.ByValTStr,
                SizeConst = 256)]
            public string Title;
        }

        [StructLayout(
            LayoutKind.Sequential,
            CharSet = CharSet.Ansi,
            Pack = 1)]
        private struct RawNextWaypointId
        {
            [MarshalAs(
                UnmanagedType.ByValTStr,
                SizeConst = 256)]
            public string Id;
        }

        public void Connect(IntPtr windowHandle)
        {
            if (simConnect != null)
            {
                return;
            }

            try
            {
                simConnect = new SimConnect(
                    "Flight Companion",
                    windowHandle,
                    WindowMessageId,
                    null,
                    0);

                simConnect.OnRecvOpen += OnRecvOpen;
                simConnect.OnRecvQuit += OnRecvQuit;
                simConnect.OnRecvException += OnRecvException;
                simConnect.OnRecvSimobjectData += OnRecvSimobjectData;
            }
            catch (Exception exception)
            {
                DisconnectInternal();
                RaiseDisconnected(exception.Message);
            }
        }

        public void ReceiveMessage()
        {
            if (simConnect == null)
            {
                return;
            }

            try
            {
                simConnect.ReceiveMessage();
            }
            catch (Exception exception)
            {
                DisconnectInternal();
                RaiseDisconnected(exception.Message);
            }
        }

        private void OnRecvOpen(
            SimConnect sender,
            SIMCONNECT_RECV_OPEN data)
        {
            ConfigureDataDefinitions();

            if (Connected != null)
            {
                Connected();
            }
        }

        private void ConfigureDataDefinitions()
        {
            if (simConnect == null)
            {
                return;
            }

            ConfigureFlightData();
            ConfigureAircraftTitle();
            ConfigureNavigationData();
            ConfigureNextWaypointId();
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

            simConnect.RegisterDataDefineStruct<RawFlightData>(
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

        private void ConfigureAircraftTitle()
        {
            simConnect.AddToDataDefinition(
                Definitions.AircraftTitle,
                "TITLE",
                null,
                SIMCONNECT_DATATYPE.STRING256,
                0,
                SimConnect.SIMCONNECT_UNUSED);

            simConnect.RegisterDataDefineStruct<RawAircraftTitle>(
                Definitions.AircraftTitle);

            simConnect.RequestDataOnSimObject(
                Requests.AircraftTitle,
                Definitions.AircraftTitle,
                SimConnect.SIMCONNECT_OBJECT_ID_USER,
                SIMCONNECT_PERIOD.SECOND,
                SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT,
                0,
                0,
                0);
        }

        private void ConfigureNavigationData()
        {
            simConnect.AddToDataDefinition(
                Definitions.NavigationData,
                "GPS IS ACTIVE FLIGHT PLAN",
                "Bool",
                SIMCONNECT_DATATYPE.INT32,
                0,
                SimConnect.SIMCONNECT_UNUSED);

            simConnect.AddToDataDefinition(
                Definitions.NavigationData,
                "GPS ETE",
                "seconds",
                SIMCONNECT_DATATYPE.FLOAT64,
                0,
                SimConnect.SIMCONNECT_UNUSED);

            simConnect.AddToDataDefinition(
                Definitions.NavigationData,
                "GPS WP DISTANCE",
                "meters",
                SIMCONNECT_DATATYPE.FLOAT64,
                0,
                SimConnect.SIMCONNECT_UNUSED);

            simConnect.AddToDataDefinition(
                Definitions.NavigationData,
                "GPS FLIGHT PLAN WP INDEX",
                "number",
                SIMCONNECT_DATATYPE.INT32,
                0,
                SimConnect.SIMCONNECT_UNUSED);

            simConnect.AddToDataDefinition(
                Definitions.NavigationData,
                "GPS FLIGHT PLAN WP COUNT",
                "number",
                SIMCONNECT_DATATYPE.INT32,
                0,
                SimConnect.SIMCONNECT_UNUSED);

            simConnect.RegisterDataDefineStruct<RawNavigationData>(
                Definitions.NavigationData);

            simConnect.RequestDataOnSimObject(
                Requests.NavigationData,
                Definitions.NavigationData,
                SimConnect.SIMCONNECT_OBJECT_ID_USER,
                SIMCONNECT_PERIOD.SECOND,
                SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT,
                0,
                0,
                0);
        }

        private void ConfigureNextWaypointId()
        {
            simConnect.AddToDataDefinition(
                Definitions.NextWaypointId,
                "GPS WP NEXT ID",
                null,
                SIMCONNECT_DATATYPE.STRING256,
                0,
                SimConnect.SIMCONNECT_UNUSED);

            simConnect.RegisterDataDefineStruct<RawNextWaypointId>(
                Definitions.NextWaypointId);

            simConnect.RequestDataOnSimObject(
                Requests.NextWaypointId,
                Definitions.NextWaypointId,
                SimConnect.SIMCONNECT_OBJECT_ID_USER,
                SIMCONNECT_PERIOD.SECOND,
                SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT,
                0,
                0,
                0);
        }

        private void OnRecvSimobjectData(
            SimConnect sender,
            SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            if (data.dwData == null ||
                data.dwData.Length == 0)
            {
                return;
            }

            Requests request =
                (Requests)data.dwRequestID;

            switch (request)
            {
                case Requests.AircraftTitle:
                    ReceiveAircraftTitle(data);
                    break;

                case Requests.NavigationData:
                    ReceiveNavigationData(data);
                    break;

                case Requests.NextWaypointId:
                    ReceiveNextWaypointId(data);
                    break;

                case Requests.FlightData:
                    ReceiveFlightData(data);
                    break;
            }
        }

        private void ReceiveAircraftTitle(
            SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            RawAircraftTitle aircraft =
                (RawAircraftTitle)data.dwData[0];

            currentAircraftTitle =
                aircraft.Title ?? string.Empty;
        }

        private void ReceiveNavigationData(
            SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            currentNavigationData =
                (RawNavigationData)data.dwData[0];
        }

        private void ReceiveNextWaypointId(
            SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            RawNextWaypointId waypoint =
                (RawNextWaypointId)data.dwData[0];

            currentNextWaypointId =
                waypoint.Id ?? string.Empty;
        }

        private void ReceiveFlightData(
            SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            RawFlightData rawData =
                (RawFlightData)data.dwData[0];

            double headingDegrees =
                rawData.HeadingRadians *
                180.0 /
                Math.PI;

            headingDegrees =
                (headingDegrees + 360.0) %
                360.0;

            bool hasFlightPlan =
                currentNavigationData.HasActiveFlightPlan != 0;

            double estimatedRemainingDistanceNm = 0;

            /*
             * GPS ETE donne le temps estimé restant jusqu’à
             * la destination. Nous estimons la distance restante
             * à partir de la vitesse sol actuelle.
             */
            if (hasFlightPlan &&
                currentNavigationData.EteSeconds > 0 &&
                rawData.GroundSpeedKnots > 0)
            {
                estimatedRemainingDistanceNm =
                    rawData.GroundSpeedKnots *
                    currentNavigationData.EteSeconds /
                    3600.0;
            }

            FlightData flightData =
                new FlightData
                {
                    AltitudeFeet =
                        rawData.AltitudeFeet,

                    GroundSpeedKnots =
                        rawData.GroundSpeedKnots,

                    VerticalSpeedFeetPerMinute =
                        rawData.VerticalSpeedFeetPerSecond *
                        60.0,

                    HeadingDegrees =
                        headingDegrees,

                    AircraftTitle =
                        currentAircraftTitle,

                    HasActiveFlightPlan =
                        hasFlightPlan,

                    GpsDistanceRemainingNm =
                        estimatedRemainingDistanceNm,

                    GpsEteSeconds =
                        currentNavigationData.EteSeconds,

                    ActiveWaypointIndex =
                        currentNavigationData.ActiveWaypointIndex,

                    WaypointCount =
                        currentNavigationData.WaypointCount,

                    NextWaypointId =
                        currentNextWaypointId,

                    NextWaypointDistanceNm =
                        currentNavigationData
                            .NextWaypointDistanceMeters /
                        MetersPerNauticalMile
                };

            if (FlightDataReceived != null)
            {
                FlightDataReceived(flightData);
            }
        }

        private void OnRecvQuit(
            SimConnect sender,
            SIMCONNECT_RECV data)
        {
            DisconnectInternal();
            RaiseDisconnected("MSFS a été fermé.");
        }

        private void OnRecvException(
            SimConnect sender,
            SIMCONNECT_RECV_EXCEPTION data)
        {
            if (Error != null)
            {
                Error(
                    "Erreur SimConnect : " +
                    data.dwException);
            }
        }

        private void RaiseDisconnected(
            string message)
        {
            if (Disconnected != null)
            {
                Disconnected(message);
            }
        }

        private void DisconnectInternal()
        {
            if (simConnect != null)
            {
                try
                {
                    simConnect.Dispose();
                }
                catch
                {
                    // Ignorer les erreurs de fermeture.
                }
            }

            simConnect = null;
            currentAircraftTitle = string.Empty;
            currentNextWaypointId = string.Empty;
            currentNavigationData =
                new RawNavigationData();
        }

        public void Dispose()
        {
            DisconnectInternal();
        }
    }
}