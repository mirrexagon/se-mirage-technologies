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
    internal class ManeuverExecutor
    {
        // Idle
        // user sets destination
        // turn to face target
        // calculate top speed such that the velocity trapezoid has enough time at top speed to turn around. Variables needed:
        // - Distance to target
        // - Time to flip
        // - max forward thrust
        // - max retro thrust
        // TODO optimization: take into account retro thrusters, if it would be faster to accelerate and then use retros to slow down vs. time taken to turn around + slow down with mains
        //
        // Choose plan, either
        // accelerate to cruise speed, flip, wait until at stopping distance, decelerate with mains, or
        // accelerate to cruise speed, wait until at stopping distance, decelerate with retros
        enum State
        {
            Off,
            StationKeeping, // Keep target orientation and position.
            TurnToFace, // Turn while keeping current position.
        }

        State state = State.Off;

        Program program;
        Ship ship;

        // Used to calculate stopping distance and as cruising speed for navigation control.
        const double CruiseSpeed = 95;
        const double StoppingDistanceSafetyFactor = 1.1;

        Vector3D TargetPosition { get; set; }

        double maximumPossibleStoppingDistance;

        public ManeuverExecutor(Program program, Ship ship)
        {
            this.program = program;
            this.ship = ship;

            ship.ReloadBlockReferences();
            Reload();
        }

        public void StopControl()
        {
            state = State.Off;
            ship.StopControl();
        }

        public void StartStationKeeping(Vector3D position, MatrixD orientation)
        {
            orientation.Translation = position;
            StartStationKeeping(orientation);
        }

        public void StartStationKeeping(MatrixD worldMatrix)
        {
            TargetPosition = worldMatrix.Translation;
            ship.TargetOrientation = worldMatrix.GetOrientation();

            ship.VelocityControlEnabled = true;
            ship.OrientationControlEnabled = true;

            state = State.StationKeeping;
        }

        public void Update(double dt)
        {
            switch (state)
            {
                case State.Off:
                    break;

                case State.StationKeeping:
                    program.Log("State: StationKeeping");
                    UpdateStationKeeping(dt);
                    break;
            }

            ship.Update(dt);
        }

        void UpdateStationKeeping(double dt)
        {
            Vector3D positionError = GetPositionError();

            double errorDistance = positionError.Normalize();
            Vector3D positionErrorDirection = positionError;

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
                    responseVelocity = positionErrorDirection * response;
                }
                else
                {
                    responseVelocity = positionErrorDirection;
                }

                ship.TargetVelocity = responseVelocity * CruiseSpeed;
            }
        }

        Vector3D GetPositionError()
        {
            return TargetPosition - ship.GetWorldMatrix().Translation;
        }

        double CalculateMaximumStoppingDistance(double speed)
        {
            double a = ship.MinPossibleThrustInAnyDirection / ship.Mass;
            return (speed * speed) / (2 * a);
        }

        void Reload()
        {
            maximumPossibleStoppingDistance = CalculateMaximumStoppingDistance(CruiseSpeed) * StoppingDistanceSafetyFactor;
        }
    }
}
