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
        ShipNavigation navigation;

        public Program()
        {
            Ship ship = new Ship(this);
            navigation = new ShipNavigation(this, ship);
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Log(string message)
        {
            Echo(message);
        }

        void UpdateFromUser(string argument)
        {
            if (argument == "panic")
            {
                navigation.Panic();
            }
            else
            {
                GPSLocation location = GPSLocation.FromString(argument);
                if (location != null)
                {
                    navigation.StartStationKeeping(location.position);
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

                    navigation.Update(dt);
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
