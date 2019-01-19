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
        public class Translation {
            // An array of all Base6Directions, so I can iterate through them all.
            readonly Array BASE6DIRECTIONS = Enum.GetValues(typeof(Base6Directions.Direction));

            Program program;

            IMyTerminalBlock orientationReference;

            List<IMyThrust> allThrusters;

            // Key is direction that the thrusters cause acceleration in.
            Dictionary<Base6Directions.Direction, List<IMyThrust>> thrusters;
            
            // Maximum possible thrust of the ship in each direction.
            Dictionary<Base6Directions.Direction, double> maximumThrust;

            // Target world velocity of the ship.
            public Vector3D targetVelocity = new Vector3D(0, 0, 0);

            // Velocity calculation.
            Vector3D lastPosition;
            Vector3D lastVelocity; // position units per second.

            // Transformations.
            MatrixD rotateWorldToOrientationReference;

            public Translation(Program program, IMyTerminalBlock orientationReference) {
                this.program = program;
                this.orientationReference = orientationReference;
                rotateWorldToOrientationReference = MatrixD.Transpose(orientationReference.WorldMatrix.GetOrientation());

                ReloadBlockReferences();
            }

            public void Update(double dt) {
                UpdateVelocity(dt);
                UpdateVelocityControl(dt);
            }

            void UpdateVelocityControl(double dt) {
                Vector3D velocityError = targetVelocity - GetWorldVelocity();

                Vector3D thrusterPower = velocityError;

                // Smoothing when close to target.
                for (int dim = 0; dim < 3; ++dim) {
                    double power = thrusterPower.GetDim(dim);
                    power = power < 0.0001 ? 0 : power;
                    thrusterPower.SetDim(dim, power);
                }

                program.Echo($"{thrusterPower}");

                Vector3D localThrusterPower = Vector3D.Transform(thrusterPower, rotateWorldToOrientationReference);
                SetThrust(thrusterPower);
            }

            // We accelerate in the specified direction.
            // Each dimension of power is a number in [-1, 1].
            void SetThrust(Vector3D power) {
                double leftPower = (power.X < 0) ? -power.X : 0; // -X
                double rightPower = (power.X >= 0) ? power.X : 0; // +X
                double downPower = (power.Y < 0) ? -power.Y : 0; // -Y
                double upPower = (power.Y >= 0) ? power.Y : 0; // +Y
                double forwardPower = (power.Z < 0) ? -power.Z : 0; // -Z
                double backwardPower = (power.Z >= 0) ? power.Z : 0; // +Z

                SetThrustInDirection(Base6Directions.Direction.Left, leftPower);
                SetThrustInDirection(Base6Directions.Direction.Right, rightPower);
                SetThrustInDirection(Base6Directions.Direction.Down, downPower);
                SetThrustInDirection(Base6Directions.Direction.Up, upPower);
                SetThrustInDirection(Base6Directions.Direction.Forward, forwardPower);
                SetThrustInDirection(Base6Directions.Direction.Backward, backwardPower);
            }

            void SetThrustInDirection(Base6Directions.Direction direction, double power) {
                foreach (IMyThrust thruster in thrusters[direction]) {
                    thruster.ThrustOverridePercentage = (float)power;
                }
            }

            public void ReloadBlockReferences() {
                thrusters = new Dictionary<Base6Directions.Direction, List<IMyThrust>>();
                maximumThrust = new Dictionary<Base6Directions.Direction, double>();
                foreach (Base6Directions.Direction direction in BASE6DIRECTIONS) {
                    thrusters.Add(direction, new List<IMyThrust>());
                    maximumThrust.Add(direction, 0);
                }

                allThrusters = new List<IMyThrust>();
                program.GridTerminalSystem.GetBlocksOfType(allThrusters, b => b.CubeGrid == program.Me.CubeGrid);

                foreach (IMyThrust thruster in allThrusters) {
                    // `thruster.Orientation.Forward` gives us the grid-local direction the front of thruster is facing
                    // (which is the thrust direction).
                    // We then transform that from the grid frame to the orientation reference (ie. cockpit) frame.
                    // See last post of: https://forum.keenswh.com/threads/comparing-block-orientations.7376017
                    Base6Directions.Direction thrustDirection = orientationReference.Orientation.TransformDirectionInverse(thruster.Orientation.Forward);

                    // Then flip that direction to get the ship acceleration direction (relative to the 
                    // orientation reference).
                    Base6Directions.Direction accelerationDirection = Base6Directions.GetFlippedDirection(thrustDirection);

                    thrusters[accelerationDirection].Add(thruster);
                    maximumThrust[accelerationDirection] += thruster.MaxThrust;
                }
            }

            public Vector3D GetWorldPosition() {
                return program.Me.CubeGrid.GetPosition();
            }

            public Vector3D GetWorldVelocity() {
                return lastVelocity;
            }

            void UpdateVelocity(double dt) {
                Vector3D position = GetWorldPosition();
                Vector3D positionDelta = position - lastPosition;
                lastVelocity = positionDelta / dt;
                lastPosition = GetWorldPosition();
            }
        }
    }
}
