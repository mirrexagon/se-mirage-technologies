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
            // -- Fields --
            public Vector3D TargetVelocity { get; set; }

            // How close we get to the target speed before we start turning the
            // thrusters down. Currently it's just linear interpolation.
            readonly double velocitySmoothingLimit = 5; // position units per second

            public Vector3D TargetPosition { get; set; }

            // TODO: Settable cruise speed.

            // ---

            // All thruster blocks in the ship, categorized by the direction
            // they cause acceleration in.
            List<IMyThrust> allThrusters;
            Dictionary<Base6Directions.Direction, List<IMyThrust>> thrusters;

            // Velocity calculation.
            Vector3D lastPosition;
            Vector3D lastVelocity; // position units per second.

            // Cached values.
            Dictionary<Base6Directions.Direction, double> maxThrustInDirection;
            public double MinPossibleThrustInAnyDirection { get; private set; }
            public double MaxPossibleThrustInAnyDirection { get; private set; }
            Base6Directions.Direction maxThrustDirection;

            double maximumPossibleStoppingDistance;

            // -- Methods --
            void UpdatePositionControl(double dt) {
                Vector3D positionError = GetPositionError();

                double errorDistance = positionError.Normalize();
                Vector3D positionErrorDirection = positionError;

                program.Log($"Target position: {TargetPosition}");
                program.Log($"Distance to target: {errorDistance}");
                program.Log($"Max stopping distance: {maximumPossibleStoppingDistance}");

                if (errorDistance > 0) {
                    Vector3D responseVelocity;
                    if (errorDistance <= maximumPossibleStoppingDistance) {
                        double response = errorDistance / maximumPossibleStoppingDistance;

                        // Logarithmic response.
                        response = Math.Log(response + 1, 2);
                        responseVelocity = positionErrorDirection * response;
                    } else {
                        responseVelocity = positionErrorDirection;
                    }

                    // TODO: Use explicit cruising speed here.
                    TargetVelocity = responseVelocity * MAXIMUM_SPEED;
                    program.Log($"Target speed: {TargetVelocity.Length()}");
                    program.Log($"Actual speed: {GetVelocity().Length()}");
                }
            }

            void UpdateVelocityControl(double dt) {
                Vector3D velocityError = GetVelocityError();

                double speedError = velocityError.Normalize();
                Vector3D velocityErrorDirection = velocityError;

                if (speedError > 0) {
                    Vector3D localVelocityErrorDirection = Vector3D.TransformNormal(velocityErrorDirection,
                        MatrixD.Transpose(orientationReference.WorldMatrix.GetOrientation()));

                    // If the velocity error is above the velocitySmoothingLimit, 
                    // accelerate at full power. Otherwise, linearly reduce
                    // power with regards to how close we are to the speed target.
                    Vector3D responseThrust;
                    if (speedError < velocitySmoothingLimit) {
                        double fraction = speedError / velocitySmoothingLimit;

                        // Quadratic smoothing.
                        fraction = fraction * fraction;
                        responseThrust = localVelocityErrorDirection * fraction;
                    } else {
                        responseThrust = localVelocityErrorDirection;
                    }

                    // We set this higher than the possible max thrust because SetThrust()
                    // will cap the speed at the actual possible max thrust for the direction.
                    SetThrust(responseThrust * MaxPossibleThrustInAnyDirection * 2);
                }
            }

            public Vector3D GetPosition() {
                return orientationReference.CenterOfMass;
            }

            public Vector3D GetPositionError() {
                return TargetPosition - GetPosition();
            }

            public Vector3D GetVelocity() {
                return lastVelocity;
            }

            public Vector3D GetVelocityError() {
                return TargetVelocity - GetVelocity();
            }

            double CalculateMaximumStoppingDistance(double speed) {
                double a = MinPossibleThrustInAnyDirection / Mass;
                return (speed * speed) / (2 * a);
             }


            // Thrust is capped so that we always accelerate in a straight line,
            // regardless of orientation and different sides having different
            // thrusts.
            // Thrust is in Newtons.
            // Returns actual thrust set.
            Vector3D SetThrust(Vector3D thrust_N) {
                Vector3D thrustDirection;
                Vector3D.Normalize(ref thrust_N, out thrustDirection);

                double desiredThrust_N = thrust_N.Length();
                double maxPossibleThrust_N = CalculateMaxThrustInDirection(thrustDirection);

                if (maxPossibleThrust_N < desiredThrust_N) {
                    thrust_N = thrustDirection * maxPossibleThrust_N;
                }

                SetThrustRaw(thrust_N);
                return thrust_N;
            }

            // We accelerate in the specified direction.
            void SetThrustRaw(Vector3D thrust_N) {
                double forwardThrust = (thrust_N.Z < 0) ? -thrust_N.Z : 0; // -Z
                double backwardThrust = (thrust_N.Z >= 0) ? thrust_N.Z : 0; // +Z
                double leftThrust = (thrust_N.X < 0) ? -thrust_N.X : 0; // -X
                double rightThrust = (thrust_N.X >= 0) ? thrust_N.X : 0; // +X
                double upThrust = (thrust_N.Y >= 0) ? thrust_N.Y : 0; // +Y
                double downThrust = (thrust_N.Y < 0) ? -thrust_N.Y : 0; // -Y

                SetThrustInDirection(Base6Directions.Direction.Forward, forwardThrust);
                SetThrustInDirection(Base6Directions.Direction.Backward, backwardThrust);
                SetThrustInDirection(Base6Directions.Direction.Left, leftThrust);
                SetThrustInDirection(Base6Directions.Direction.Right, rightThrust);
                SetThrustInDirection(Base6Directions.Direction.Up, upThrust);
                SetThrustInDirection(Base6Directions.Direction.Down, downThrust);

                program.Log($"Actual set thrust: {thrust_N.Length()}");
            }

            void SetThrustInDirection(Base6Directions.Direction direction, double thrust_newtons) {
                foreach (IMyThrust thruster in thrusters[direction]) {
                    thruster.ThrustOverride = (float)thrust_newtons;
                }
            }

            double CalculateMaxThrustInDirection(Vector3D direction) {
                Vector3D absMaxThrust = new Vector3D();

                Base6Directions.Direction forwardBackwardDirection = (direction.Z < 0) ? Base6Directions.Direction.Forward : Base6Directions.Direction.Backward;
                absMaxThrust.Z = maxThrustInDirection[forwardBackwardDirection];

                Base6Directions.Direction leftRightDirection = (direction.X < 0) ? Base6Directions.Direction.Left : Base6Directions.Direction.Right;
                absMaxThrust.X = maxThrustInDirection[leftRightDirection];

                Base6Directions.Direction upDownDirection = (direction.Y < 0) ? Base6Directions.Direction.Down : Base6Directions.Direction.Up;
                absMaxThrust.Y = maxThrustInDirection[upDownDirection];

                // ---

                double limitingThrust = absMaxThrust.AbsMin();
                direction *= limitingThrust;

                return direction.Length();
            }

            void UpdateVelocity(double dt) {
                Vector3D position = GetPosition();
                Vector3D positionDelta = position - lastPosition;
                lastVelocity = positionDelta / dt;
                lastPosition = GetPosition();
            }

            void ReloadTranslationBlockReferences() {
                // (Re)initialize fields.
                thrusters = new Dictionary<Base6Directions.Direction, List<IMyThrust>>();
                maxThrustInDirection = new Dictionary<Base6Directions.Direction, double>();
                MaxPossibleThrustInAnyDirection = 0;
                MinPossibleThrustInAnyDirection = double.MaxValue;

                foreach (Base6Directions.Direction direction in Base6Directions.EnumDirections) {
                    thrusters.Add(direction, new List<IMyThrust>());
                    maxThrustInDirection.Add(direction, 0);
                }

                // Get all the thrusters.
                allThrusters = new List<IMyThrust>();
                program.GridTerminalSystem.GetBlocksOfType(allThrusters, b => b.CubeGrid == program.Me.CubeGrid);

                // Categorize thrusters by direction and update maximum thrust in each direction.
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
                    maxThrustInDirection[accelerationDirection] += thruster.MaxThrust;
                }

                // Update maximum thrust in any direction.
                foreach (Base6Directions.Direction direction in Base6Directions.EnumDirections) {
                    double maxThrustForDirection = maxThrustInDirection[direction];
                    if (maxThrustForDirection > MaxPossibleThrustInAnyDirection) {
                        MaxPossibleThrustInAnyDirection = maxThrustForDirection;
                        maxThrustDirection = direction;
                    }

                    MinPossibleThrustInAnyDirection = Math.Min(MinPossibleThrustInAnyDirection, maxThrustForDirection);
                }

                // Update maximum stopping distance.
                maximumPossibleStoppingDistance = CalculateMaximumStoppingDistance(MAXIMUM_SPEED);
            }
        }
    }
}
