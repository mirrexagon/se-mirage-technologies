﻿using Sandbox.Game.EntityComponents;
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
        Ship ship;

        public Program() {
            ship = new Ship(this);
            ship.CruiseSpeed = 200;

            ship.TargetOrientation = QuaternionD.Identity;
            ship.OrientationControlEnabled = true;

            ship.TargetPosition = ship.GetPosition();

            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Main(string argument, UpdateType updateSource) {
            try {
                ClearLog();

                double dt = Runtime.TimeSinceLastRun.TotalSeconds;
                if (dt == 0) {
                    return;
                }

                if ((updateSource & (UpdateType.Trigger | UpdateType.Terminal)) != 0) {
                    ship.ReloadBlockReferences();

                    if (argument == "panic") {
                        ship.OrientationControlEnabled = false;
                        ship.PositionControlEnabled = false;
                        ship.VelocityControlEnabled = false;
                        ship.SetInertialDampenersEnabled(true);
                        ship.SetThrustToZero();
                    } else {
                        GPSLocation location = GPSLocation.FromString(argument);
                        if (location != null) {
                            ship.TargetPosition = location.position;
                        }
                        ship.PositionControlEnabled = true;
                    }
                }

                ship.Update(dt);
            } catch (Exception e) {
                Echo("An error occurred during script execution.");
                Echo($"Exception: {e}\n---");

                throw;
            }
        }
    }
}
