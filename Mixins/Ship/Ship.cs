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
        public class Ship {
            Program program;

            List<IMyShipController> shipControllers;
            List<IMyRemoteControl> remoteControls;
            List<IMyCockpit> cockpits;

            IMyTerminalBlock orientationReference;

            public Rotation rotation;

            public Ship(Program program) {
                this.program = program;
                orientationReference = FindOrientationReference();
                rotation = new Rotation(program, orientationReference);
            }

            public void Update(double dt) {
                rotation.Update(dt);
            }

            IMyTerminalBlock FindOrientationReference() {
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
