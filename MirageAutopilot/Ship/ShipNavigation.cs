using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
        // Used to calculate stopping distance and as cruising speed for navigation control.
        const double CruiseSpeed = 95;
        const double StoppingDistanceSafetyFactor = 1.1;

        // How close we get to the target speed before we start turning the
        // thrusters down. Currently it's just linear interpolation.
        const double VelocitySmoothingLimit = 4; // position units per second

        public Vector3D TargetPosition { get; set; }

        // TODO: Settable cruise speed.

        double maximumPossibleStoppingDistance;

        void UpdateStationKeeping(double dt)
        {
            Vector3D positionError = GetPositionError();

            double errorDistance = positionError.Normalize();
            Vector3D positionErrorDirection = positionError;

            Log($"Target position: {TargetPosition}");
            Log($"Distance to target: {errorDistance}");
            Log($"Max stopping distance: {maximumPossibleStoppingDistance}");

            if (errorDistance > 0)
            {
                Vector3D responseVelocity;
                if (errorDistance < 1)
                {
                    responseVelocity = Vector3D.Zero;
                }
                else if (errorDistance <= maximumPossibleStoppingDistance)
                {
                    double response = errorDistance / maximumPossibleStoppingDistance;
                    Log($"Response: {response}");

                    responseVelocity = positionErrorDirection * response;
                }
                else
                {
                    responseVelocity = positionErrorDirection;
                }

                // TODO: Use explicit cruising speed here.
                TargetVelocity = responseVelocity * CruiseSpeed;

                Log($"Target speed: {TargetVelocity.Length()}");
                Log($"Actual speed: {GetVelocity().Length()}");
            }
        }

        public Vector3D GetPosition()
        {
            return orientationReference.CenterOfMass;
        }

        public Vector3D GetPositionError()
        {
            return TargetPosition - GetPosition();
        }

        double CalculateMaximumStoppingDistance(double speed)
        {
            double a = MinPossibleThrustInAnyDirection / Mass;
            return (speed * speed) / (2 * a);
        }

        void ReloadNavigation()
        {
            maximumPossibleStoppingDistance = CalculateMaximumStoppingDistance(CruiseSpeed) * StoppingDistanceSafetyFactor;
        }
    }
}
