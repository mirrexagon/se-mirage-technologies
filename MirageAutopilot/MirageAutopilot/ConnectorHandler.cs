using Microsoft.Build.Utilities;
using Sandbox.Game.Entities.Interfaces;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    internal partial class ConnectorHandler
    {
        internal class ConnectorInfo
        {
            public string Name { get; private set; }
            public Vector3D WorldPosition { get; private set; }
            public QuaternionD WorldOrientation { get; private set; }
            public DateTime LastAdvertisementReceived { get; set; }

            ConnectorInfo() { }

            public ConnectorInfo(IMyShipConnector connector)
            {
                Name = $"{connector.CubeGrid.CustomName}/{connector.DisplayNameText}";
                WorldPosition = connector.WorldMatrix.Translation;
                WorldOrientation = QuaternionD.CreateFromRotationMatrix(connector.WorldMatrix.GetOrientation());
                LastAdvertisementReceived = DateTime.Now;
            }

            public static ConnectorInfo FromString(string connectorInfoString)
            {
                string[] fields = connectorInfoString.Split(':');

                if (fields[0] != "ConnectorInfo")
                {
                    throw new FormatException($"'{connectorInfoString}' is not a ConnectorInfo string");
                }

                string name = fields[1];
                double x, y, z, qw, qx, qy, qz;
                bool xOk = double.TryParse(fields[2], out x);
                bool yOk = double.TryParse(fields[3], out y);
                bool zOk = double.TryParse(fields[4], out z);
                bool qxOk = double.TryParse(fields[5], out qx);
                bool qyOk = double.TryParse(fields[6], out qy);
                bool qzOk = double.TryParse(fields[7], out qz);
                bool qwOk = double.TryParse(fields[8], out qw);

                if (xOk && yOk && zOk && qxOk && qyOk && qzOk && qwOk)
                {
                    Vector3D position = new Vector3D(x, y, z);
                    QuaternionD orientation = new QuaternionD(qx, qy, qz, qw);

                    return new ConnectorInfo
                    {
                        Name = name,
                        WorldPosition = position,
                        WorldOrientation = orientation,
                        LastAdvertisementReceived = DateTime.Now,
                    };
                }
                else
                {
                    throw new FormatException($"ConnectorInfo string '{connectorInfoString}' is formatted incorrectly");
                }
            }

            public override string ToString()
            {
                var position = WorldPosition;
                var orientation = WorldOrientation;
                return $"ConnectorInfo:{Name}:{position.X}:{position.Y}:{position.Z}:{orientation.X}:{orientation.Y}:{orientation.Z}:{orientation.W}";
            }
        }

        public Dictionary<string, ConnectorInfo> ReceivedConnectorAdvertisements { get; private set; }
        public IMyShipConnector PrimaryDockingConnector { get; private set; }

        readonly Program program;
        List<IMyShipConnector> connectorsOnShip;
        IMyBroadcastListener connectorAdvertisementListener;

        // TODO: More specific for specific applications, eg. drone ship.
        const string CONNECTOR_ADVERTISE_TAG = "MirageTechnologies:ConnectorAdvertise";

        public ConnectorHandler(Program program)
        {
            this.program = program;

            ReloadBlockReferences();

            ReceivedConnectorAdvertisements = new Dictionary<string, ConnectorInfo>();
            connectorAdvertisementListener = program.IGC.RegisterBroadcastListener(CONNECTOR_ADVERTISE_TAG);
        }

        public void ReloadBlockReferences()
        {
            connectorsOnShip = new List<IMyShipConnector>();
            program.GridTerminalSystem.GetBlocksOfType(connectorsOnShip);

            PrimaryDockingConnector = connectorsOnShip.Find(c => c.IsParkingEnabled);
        }

        public void AdvertiseConnectors()
        {
            foreach (var connector in connectorsOnShip)
            {
                var connectorInfo = new ConnectorInfo(connector);
                program.IGC.SendBroadcastMessage(CONNECTOR_ADVERTISE_TAG, connectorInfo.ToString(), TransmissionDistance.AntennaRelay);
            }
        }

        public void ProcessReceivedAdvertisements()
        {
            while (connectorAdvertisementListener.HasPendingMessage)
            {
                var message = connectorAdvertisementListener.AcceptMessage();

                try
                {
                    string payload = message.Data as string;
                    if (payload != null)
                    {
                        ConnectorInfo connectorInfo = ConnectorInfo.FromString(payload);
                        connectorInfo.LastAdvertisementReceived = DateTime.Now;
                        ReceivedConnectorAdvertisements[connectorInfo.Name] = connectorInfo;
                    }
                }
                catch (FormatException)
                {
                    continue;
                }
            }
        }
    }
}
