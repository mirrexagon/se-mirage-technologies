using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
    partial class Program : MyGridProgram
    {
        readonly Ship ship;
        readonly ManeuverExecutor maneuverExecutor;
        readonly ConnectorHandler connectorHandler;

        public Program()
        {
            ship = new Ship(this);
            maneuverExecutor = new ManeuverExecutor(this, ship);
            connectorHandler = new ConnectorHandler(this);

            Runtime.UpdateFrequency = UpdateFrequency.Update10 | UpdateFrequency.Update100;
        }

        public void Log(string message)
        {
            Echo(message);
        }

        void UpdateFromUser(string argument)
        {
            if (argument == "panic")
            {
                maneuverExecutor.StopControl();
            }
            else
            {
                GPSLocation location = GPSLocation.FromString(argument);
                if (location != null)
                {
                    maneuverExecutor.StartStationKeeping(location.Position, ship.GetWorldOrientation());
                }
            }
        }

        public void Main(string argument, UpdateType updateSource)
        {
            try
            {
                if ((updateSource & (UpdateType.Trigger | UpdateType.Terminal)) != 0)
                {
                    UpdateFromUser(argument);
                }
                else if ((updateSource & UpdateType.Update10) != 0)
                {
                    double dt = Runtime.TimeSinceLastRun.TotalSeconds;
                    if (dt == 0)
                    {
                        return;
                    }

                    maneuverExecutor.Update(dt);
                }
                else if ((updateSource & UpdateType.Update100) != 0)
                {
                    connectorHandler.AdvertiseConnectors();
                    connectorHandler.ProcessReceivedAdvertisements();

                    Log("Advertised connectors:");
                    DateTime now = DateTime.Now;
                    foreach (var info in connectorHandler.ReceivedConnectorAdvertisements.Values)
                    {
                        TimeSpan receivedTimeAgo = now - info.LastAdvertisementReceived;
                        Log($"{info.Name}: {receivedTimeAgo.TotalSeconds} seconds ago");
                    }
                }
            }
            catch (Exception e)
            {
                Echo("An error occurred during script execution.");
                Echo($"Exception: {e}\n---");

                throw;
            }
        }
    }
}
