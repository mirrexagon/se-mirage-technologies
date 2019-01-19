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

// TODO: Calculating stopping distance and using that to just go up to cruise speed, then slow down in time.

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

            public Vector3D targetVelocity = new Vector3D(0, 0, 0);
            public Vector3D targetPosition = new Vector3D(0, 0, 0);
            public double cruiseSpeed = 100.0; // m/s

            // Velocity calculation.
            Vector3D lastPosition;
            Vector3D lastVelocity; // position units per second.

            // PID control.
            Dictionary<Base6Directions.Axis, PID> velocityPIDs;
            Dictionary<Base6Directions.Axis, PID> positionPIDs;

            // Transformations.
            MatrixD rotateWorldToOrientationReference;

            public Translation(Program program, IMyTerminalBlock orientationReference) {
                this.program = program;
                this.orientationReference = orientationReference;
                rotateWorldToOrientationReference = MatrixD.Transpose(orientationReference.WorldMatrix.GetOrientation());

                velocityPIDs = new Dictionary<Base6Directions.Axis, PID>();
                velocityPIDs.Add(Base6Directions.Axis.LeftRight, 
                        new PID(0.8, 0, 0, 10));
                velocityPIDs.Add(Base6Directions.Axis.UpDown, 
                        new PID(0.8, 0, 0, 10));
                velocityPIDs.Add(Base6Directions.Axis.ForwardBackward, 
                        new PID(0.8, 0, 0, 10));

                positionPIDs = new Dictionary<Base6Directions.Axis, PID>();
                positionPIDs.Add(Base6Directions.Axis.LeftRight,
                        new PID(2, 0, 0.2, 10));
                positionPIDs.Add(Base6Directions.Axis.UpDown,
                        new PID(2, 0, 0.2, 10));
                positionPIDs.Add(Base6Directions.Axis.ForwardBackward,
                        new PID(2, 0, 0.2, 10));


                ReloadBlockReferences();
            }

            public void Update(double dt) {
                UpdateVelocity(dt);

                UpdatePositionControl(dt);
                UpdateVelocityControl(dt);

                program.Echo($"Position: {GetWorldPosition()}");
                program.Echo($"Velocity: {GetWorldVelocity()}");
            }

            void UpdatePositionControl(double dt) {
                Vector3D positionError = targetPosition - GetWorldPosition();
                Vector3D responseVelocity = new Vector3D();

                program.Echo($"Position error: {positionError}");

                responseVelocity.X = positionPIDs[Base6Directions.Axis.LeftRight].Update(positionError.X, dt);
                responseVelocity.Y = positionPIDs[Base6Directions.Axis.UpDown].Update(positionError.Y, dt);
                responseVelocity.Z = positionPIDs[Base6Directions.Axis.ForwardBackward].Update(positionError.Z, dt);

                program.Echo($"Response velocity: {responseVelocity}");

                double responseSpeed = responseVelocity.Length();
                if (responseSpeed > cruiseSpeed) {
                    responseVelocity *= cruiseSpeed / responseSpeed;
                }

                program.Echo($"Capped response velocity: {responseVelocity}");

                targetVelocity = responseVelocity;
            }

            void UpdateVelocityControl(double dt) {
                Vector3D velocityError = targetVelocity - GetWorldVelocity();
                program.Echo($"Velocity error: {velocityError}");

                Vector3D localVelocityError = Vector3D.Transform(velocityError, rotateWorldToOrientationReference);

                Vector3D responseThrust = new Vector3D();

                responseThrust.X = velocityPIDs[Base6Directions.Axis.LeftRight].Update(velocityError.X, dt);
                responseThrust.Y = velocityPIDs[Base6Directions.Axis.UpDown].Update(velocityError.Y, dt);
                responseThrust.Z = velocityPIDs[Base6Directions.Axis.ForwardBackward].Update(velocityError.Z, dt);

                program.Echo($"Response velocity: {responseThrust}");

                SetThrust(responseThrust);
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
