using System;
using System.Runtime.InteropServices;
using Microsoft.FlightSimulator.SimConnect;
using FlightCompanion.NetFramework.Models;

namespace FlightCompanion.NetFramework.Services
{
    public class SimConnectService : IDisposable
    {
        public const int WindowMessageId = 0x0402;

        private SimConnect simConnect;

        public event Action Connected;
        public event Action<string> Disconnected;
        public event Action<string> Error;
        public event Action<FlightData> FlightDataReceived;

        private enum Definitions
        {
            FlightData
        }

        private enum Requests
        {
            FlightData
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct RawFlightData
        {
            public double AltitudeFeet;
            public double GroundSpeedKnots;
            public double VerticalSpeedFeetPerSecond;
            public double HeadingRadians;
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

                if (Disconnected != null)
                {
                    Disconnected(exception.Message);
                }
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

                if (Disconnected != null)
                {
                    Disconnected(exception.Message);
                }
            }
        }

        private void OnRecvOpen(
            SimConnect sender,
            SIMCONNECT_RECV_OPEN data)
        {
            ConfigureFlightData();

            if (Connected != null)
            {
                Connected();
            }
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

        private void OnRecvSimobjectData(
            SimConnect sender,
            SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            if ((Requests)data.dwRequestID != Requests.FlightData ||
                data.dwData.Length == 0)
            {
                return;
            }

            RawFlightData rawData =
                (RawFlightData)data.dwData[0];

            double headingDegrees =
                rawData.HeadingRadians * 180.0 / Math.PI;

            headingDegrees =
                (headingDegrees + 360.0) % 360.0;

            FlightData flightData = new FlightData
            {
                AltitudeFeet = rawData.AltitudeFeet,
                GroundSpeedKnots = rawData.GroundSpeedKnots,
                VerticalSpeedFeetPerMinute =
                    rawData.VerticalSpeedFeetPerSecond * 60.0,
                HeadingDegrees = headingDegrees
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

            if (Disconnected != null)
            {
                Disconnected("MSFS a été fermé.");
            }
        }

        private void OnRecvException(
            SimConnect sender,
            SIMCONNECT_RECV_EXCEPTION data)
        {
            if (Error != null)
            {
                Error("Erreur SimConnect : " + data.dwException);
            }
        }

        private void DisconnectInternal()
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

        public void Dispose()
        {
            DisconnectInternal();
        }
    }
}