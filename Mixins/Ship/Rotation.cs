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

// The front of a gyro is the bit that points downwards.

namespace IngameScript {
    partial class Program {
        public class Rotation {
            Program program;
            IMyTerminalBlock orientationReference;

            IMyTextPanel debugPanel;

            List<IMyGyro> gyroBlocks;

            public Rotation(Program program, IMyTerminalBlock orientationReference) {
                this.program = program;
                this.orientationReference = orientationReference;

                List<IMyTextPanel> panels = new List<IMyTextPanel>();
                program.GridTerminalSystem.GetBlocksOfType(panels);
                debugPanel = panels[0];

                ReloadBlockReferences();
            }

            public void Update(double dt) {
                ReloadBlockReferences();
                string msg = "";

                MatrixD referenceWorldOrientation = orientationReference.WorldMatrix.GetOrientation();
                MatrixD gridWorldOrientation = program.Me.CubeGrid.WorldMatrix.GetOrientation();

                msg += $"{gridWorldOrientation.Forward}\n";

                if (gyroBlocks.Count > 0) {
                    MatrixD gyroWorldOrientation = gyroBlocks[0].WorldMatrix.GetOrientation();
                    msg += $"{gyroWorldOrientation.Forward}";
                }

                debugPanel.WritePublicText(msg);
            }

            public void ReloadBlockReferences() {
                gyroBlocks = new List<IMyGyro>();
                program.GridTerminalSystem.GetBlocksOfType(gyroBlocks, b => b.CubeGrid == program.Me.CubeGrid);
            }
            
            Quaternion GetWorldOrientationOfReference() {
                return Quaternion.CreateFromRotationMatrix(orientationReference.WorldMatrix.GetOrientation());
            }

            Quaternion GetWorldOrientationOfGrid() {
                return Quaternion.CreateFromRotationMatrix(program.Me.CubeGrid.WorldMatrix.GetOrientation());
            }
        }
    }
}
