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
        partial class Ship {
            // Target world orientation of the orientation reference.
            public QuaternionD TargetOrientation { get; set; }

            // All gyros on the ship.
            List<IMyGyro> gyros;

            public QuaternionD GetWorldOrientation() {
                return QuaternionD.CreateFromRotationMatrix(orientationReference.WorldMatrix.GetOrientation());
            }

            public void SetGyroOverrideEnabled(bool enabled) {
                foreach (IMyGyro gyro in gyros) {
                    gyro.GyroOverride = enabled;
                }
            }

            void UpdateOrientationControl(double dt) {
                QuaternionD orientationError = GetOrientationError();

                Vector3D worldRotationAxis;
                double worldRotationAngle;
                orientationError.GetAxisAngle(out worldRotationAxis, out worldRotationAngle);

                program.Log($"Rotation error: {worldRotationAngle}");

                foreach (IMyGyro gyro in gyros) {
                    // https://forum.keenswh.com/threads/how-can-i-roll-my-ship-to-align-its-floor-with-the-floor-of-a-station.7382390/#post-1286963408
                    MatrixD worldToGyro = MatrixD.Invert(gyro.WorldMatrix.GetOrientation());
                    Vector3D localRotationAxis = Vector3D.Transform(worldRotationAxis, worldToGyro);

                    // Smoothing when close to target.
                    double value = Math.Log(worldRotationAngle + 1, 2);
                    localRotationAxis *= value < 0.001 ? 0 : value;

                    gyro.Pitch = (float)-localRotationAxis.X;
                    gyro.Yaw = (float)-localRotationAxis.Y;
                    gyro.Roll = (float)-localRotationAxis.Z;
                }
            }

            public QuaternionD GetOrientationError() {
                return TargetOrientation / GetWorldOrientation();
            }

            public void ReloadRotationBlockReferences() {
                gyros = new List<IMyGyro>();
                program.GridTerminalSystem.GetBlocksOfType(gyros, b => b.CubeGrid == program.Me.CubeGrid);
            }
        }
    }
}
