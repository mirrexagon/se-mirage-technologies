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
        public MatrixD TargetOrientation { get; set; } = MatrixD.Identity;

        // All gyros on the ship.
        List<IMyGyro> gyros;

        void PointInDirection(Vector3D forward)
        {
            TargetOrientation = MatrixD.CreateWorld(Vector3D.Zero, forward, Vector3D.Up);
        }

        void PointAt(Vector3D worldPosition)
        {
            PointInDirection(worldPosition - GetWorldMatrix().Translation);
        }

        void SetGyroOverrideEnabled(bool enabled)
        {
            foreach (IMyGyro gyro in gyros)
            {
                gyro.GyroOverride = enabled;
            }
        }

        void UpdateOrientationControl(double dt)
        {
            MatrixD orientationError = GetOrientationError();
            program.Log($"Error matrix: {orientationError}");

            Vector3D worldErrorAxis;
            double worldErrorAngle_rad;

            GetAxisAngleFromRotationMatrix(ref orientationError, out worldErrorAxis, out worldErrorAngle_rad);

            Vector3D axisAngleWorldError = worldErrorAxis * worldErrorAngle_rad;
            program.Log($"Rotation error: {axisAngleWorldError.X} {axisAngleWorldError.Y} {axisAngleWorldError.Z}");

            // https://forum.keenswh.com/threads/how-can-i-roll-my-ship-to-align-its-floor-with-the-floor-of-a-station.7382390/#post-1286963408
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

        void GetAxisAngleFromRotationMatrix(ref MatrixD rotationMatrix, out Vector3D axis, out double angle_rad)
        {
            // https://github.com/Whiplash141/SpaceEngineersScripts/blob/1fecf23c2ca69c42019cba3631728305f0f55e60/Unpolished/point_ship_at_gps.cs#L283
            axis = new Vector3D(rotationMatrix.M32 - rotationMatrix.M23,
                            rotationMatrix.M13 - rotationMatrix.M31,
                            rotationMatrix.M21 - rotationMatrix.M12);

            double trace = rotationMatrix.M11 + rotationMatrix.M22 + rotationMatrix.M33;
            angle_rad = Math.Acos(MathHelper.Clamp((trace - 1) * 0.5, -1.0, 1.0));

            if (Vector3D.IsZero(axis))
            {
                /*
                 * Degenerate case where we get a zero axis. This means we are either
                 * exactly aligned or exactly anti-aligned.
                 */

                // If we are anti-aligned, arbitarily pick an axis to resolve the singularity (angle will be 2*pi radians).
                axis = Vector3D.Up;
            }

            if (!Vector3D.IsUnit(ref axis))
            {
                axis = Vector3D.Normalize(axis);
            }
        }

        MatrixD GetOrientationError()
        {
            //program.Log($"current: {GetWorldMatrix().GetOrientation()}");
            MatrixD shipToWorld = GetWorldMatrix().GetOrientation();

            // Note: Vectors are considered row-major when doing
            // transformations, so transformations by matrix multiplication
            // apply left-to-right.
            // Top of https://forum.keenswh.com/threads/tutorial-how-to-do-vector-transformations-with-world-matricies.7399827/

            // TODO: Why does this work and what does it represent?
            return MatrixD.Transpose(TargetOrientation * MatrixD.Transpose(shipToWorld));
        }

        void ReloadRotationBlockReferences()
        {
            gyros = new List<IMyGyro>();
            program.GridTerminalSystem.GetBlocksOfType(gyros, b => b.CubeGrid == program.Me.CubeGrid);
        }

    }
}
