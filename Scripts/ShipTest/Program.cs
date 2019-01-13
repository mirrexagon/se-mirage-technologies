﻿using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript {
    partial class Program : MyGridProgram {
        private Ship ship;

        public Program() {
            ship = new Ship(this);
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Save() {

        }

        public void Main(string argument, UpdateType updateSource) {
            try {
                double dt = Runtime.TimeSinceLastRun.TotalSeconds;
                if (dt == 0) {
                    dt = 0.01;
                }

                if ((updateSource & (UpdateType.Trigger | UpdateType.Terminal)) != 0) {
                    ship.rotation.SetGyroOverrideEnabled(true);

                    string[] args = argument.Split(',');

                    double targetYaw = double.Parse(args[0]);
                    double targetPitch = double.Parse(args[1]);

                    ship.rotation.targetOrientation = QuaternionD.CreateFromYawPitchRoll(targetYaw, targetPitch, 0);
                }

                if ((updateSource & UpdateType.Update10) != 0) {

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