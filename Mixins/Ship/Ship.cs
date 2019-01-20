using Sandbox.Game.EntityComponents;
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
    partial class Program {
        IMyTextPanel debugPanel;
        string debugText = "";

        public void Log(string s) {
            if (debugPanel != null) {
                debugText += s + "\n";
                debugPanel.WritePublicText(debugText);
            }
        }

        public void ClearLog() {
            if (debugPanel != null) {
                debugText = "";
                debugPanel.WritePublicText(debugText);
            }
        }

        partial class Ship {
            // The all-important Program.
            Program program;

            // World constants
            public static readonly double MAXIMUM_SPEED = 105;

            // Ship controllers
            List<IMyShipController> shipControllers;
            List<IMyRemoteControl> remoteControls;
            List<IMyCockpit> cockpits;

            IMyShipController orientationReference;

            // Useful ship information.
            double shipMass;

            public Ship(Program program) {
                this.program = program;

                IMyTerminalBlock possibleDebugPanel = program.GridTerminalSystem.GetBlockWithName("Debug Panel");

                if (possibleDebugPanel != null) {
                    program.debugPanel = (IMyTextPanel)possibleDebugPanel;
                }

                ReloadBlockReferences();
            }

            public void Update(double dt) {
                program.ClearLog();

                UpdateOrientationControl(dt);
                UpdateVelocity(dt);
                UpdateVelocityControl(dt);
            }

            public void SetInertialDampenersEnabled(bool enabled) {
                foreach (IMyShipController shipController in shipControllers) {
                    shipController.DampenersOverride = enabled;
                }
            }

            public double GetShipMass() {
                return shipMass;
            }

            // ---

            public void ReloadBlockReferences() {
                ReloadShipControllerReferences();
                orientationReference = FindOrientationReference();

                ReloadRotationBlockReferences();
                ReloadTranslationBlockReferences();

                shipMass = orientationReference.CalculateShipMass().TotalMass;
            }

            IMyShipController FindOrientationReference() {
                ReloadShipControllerReferences();

                IMyRemoteControl mainRemoteControl = remoteControls.FindAll(rc => rc.IsMainCockpit).FirstOrDefault();
                if (mainRemoteControl != null) {
                    return mainRemoteControl;
                }

                IMyCockpit mainCockpit = cockpits.FindAll(c => c.IsMainCockpit).FirstOrDefault();
                if (mainCockpit != null) {
                    return mainCockpit;
                }

                if (remoteControls.Count > 0) {
                    return remoteControls[0];
                }

                if (cockpits.Count > 0) {
                    return cockpits[0];
                }

                return null;
            }

            void ReloadShipControllerReferences() {
                shipControllers = new List<IMyShipController>();
                program.GridTerminalSystem.GetBlocksOfType(shipControllers);

                remoteControls = new List<IMyRemoteControl>();
                program.GridTerminalSystem.GetBlocksOfType(remoteControls);

                cockpits = new List<IMyCockpit>();
                program.GridTerminalSystem.GetBlocksOfType(cockpits);
            }
        }
    }
}
