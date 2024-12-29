using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Numerics;
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
    internal partial class Ship
    {
        // Target world orientation of the orientation reference.
        public QuaternionD TargetOrientation { get; set; }

        // All gyros on the ship.
        List<IMyGyro> gyros;

        void SetGyroOverrideEnabled(bool enabled)
        {
            foreach (IMyGyro gyro in gyros)
            {
                gyro.GyroOverride = enabled;
            }
        }

        void UpdateOrientationControl(double dt)
        {
            // Based on https://forum.keenswh.com/threads/how-can-i-roll-my-ship-to-align-its-floor-with-the-floor-of-a-station.7382390/#post-1286963408

            QuaternionD currentWorldOrientation = GetOrientation();
            QuaternionD orientationError = TargetOrientation / currentWorldOrientation;

            Vector3D worldErrorAxis;
            double worldErrorAngle_rad;

            orientationError.GetAxisAngle(out worldErrorAxis, out worldErrorAngle_rad);

            Vector3D axisAngleWorldError = worldErrorAxis * worldErrorAngle_rad;

            foreach (IMyGyro gyro in gyros)
            {
                MatrixD worldToGyro = MatrixD.Transpose(gyro.WorldMatrix.GetOrientation());
                Vector3D localRotationAxis = Vector3D.TransformNormal(worldErrorAxis, worldToGyro);

                // Smoothing when close to target.
                double value = Math.Log(worldErrorAngle_rad + 1, 2);
                localRotationAxis *= value < 0.001 ? 0 : value;

                gyro.Pitch = (float)-localRotationAxis.X;
                gyro.Yaw = (float)-localRotationAxis.Y;
                gyro.Roll = (float)-localRotationAxis.Z;
            }
        }

        public QuaternionD GetOrientation()
        {
            return QuaternionD.CreateFromRotationMatrix(orientationReference.WorldMatrix.GetOrientation());
        }

        void ReloadRotationBlockReferences()
        {
            gyros = new List<IMyGyro>();
            program.GridTerminalSystem.GetBlocksOfType(gyros, b => b.CubeGrid == program.Me.CubeGrid);
        }
    }
}
