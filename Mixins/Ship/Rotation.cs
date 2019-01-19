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

// With "Show grid pivot" on, the red line points right/X+, the green line points up/Y+, the blue line points forward/Z-

// A block's Orientation.GetMatrix() method returns a matrix that transforms
// from the block's orientation to the parent grid's orientation. We need the inverse.
// The inverse of a pure rotation matrix is just its transpose.
// https://forum.keenswh.com/threads/comparing-block-orientations.7376017/#post-1286903026

// https://forum.keenswh.com/threads/programmable-block-imythrust-gridthrustdirection-issue.7399243/
// https://github.com/Wicorel/WicoSpaceEngineers/blob/master/WicoThrusters/WicoThrusters/thrusters.cs#L88

// Vector.Transform() premultiplies (as opposed to post multiplying) the vector:
// https://forum.keenswh.com/threads/script-to-maintain-a-specific-alignment.7324214/#post-1286570090

// Gyros:
// Pitch property positive -> terminal pitch negative -> clockwise rotation when looking from positive X (right)
// Yaw property positive -> terminal yaw positive -> anticlockwise rotation when looking from positive Y (up)
// Roll property positive -> terminal roll positive -> clockwise rotation when looking from positive Z (back)

namespace IngameScript {
    partial class Program {
        public class Rotation {
            Program program;

            IMyTerminalBlock orientationReference;
            List<IMyGyro> gyroBlocks;

            // Target world orientation of the orientation reference.
            public QuaternionD targetOrientation = QuaternionD.Identity;

            public Rotation(Program program, IMyTerminalBlock orientationReference) {
                this.program = program;
                this.orientationReference = orientationReference;

                ReloadBlockReferences();
            }

            public void Update(double dt) {
                UpdateOrientationControl(dt);
            }

            void UpdateOrientationControl(double dt) {
                QuaternionD orientationError = targetOrientation / GetWorldOrientation();

                Vector3D worldRotationAxis;
                double worldRotationAngle;
                orientationError.GetAxisAngle(out worldRotationAxis, out worldRotationAngle);

                foreach (IMyGyro gyro in gyroBlocks) {
                    // https://forum.keenswh.com/threads/how-can-i-roll-my-ship-to-align-its-floor-with-the-floor-of-a-station.7382390/#post-1286963408
                    MatrixD worldToGyro = MatrixD.Invert(gyro.WorldMatrix.GetOrientation());
                    Vector3D localRotationAxis = Vector3D.Transform(worldRotationAxis, worldToGyro);
                    
                    double value = Math.Log(worldRotationAngle + 1, 2);
                    localRotationAxis *= value < 0.001 ? 0 : value;
                    gyro.Pitch = (float)-localRotationAxis.X;
                    gyro.Yaw = (float)-localRotationAxis.Y;
                    gyro.Roll = (float)-localRotationAxis.Z;
                }
            }

            public void ReloadBlockReferences() {
                gyroBlocks = new List<IMyGyro>();
                program.GridTerminalSystem.GetBlocksOfType(gyroBlocks, b => b.CubeGrid == program.Me.CubeGrid);
            }

            public void SetGyroOverrideEnabled(bool enabled) {
                foreach (IMyGyro gyro in gyroBlocks) {
                    gyro.GyroOverride = enabled;
                }
            }

            public QuaternionD GetWorldOrientation() {
                return QuaternionD.CreateFromRotationMatrix(orientationReference.WorldMatrix.GetOrientation());
            }
        }
    }
}
