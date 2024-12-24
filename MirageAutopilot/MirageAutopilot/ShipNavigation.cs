﻿using Sandbox.Game.EntityComponents;
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
    internal class ShipNavigation
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
            StationKeeping,
        }

        State state = State.Off;

        Program program;
        Ship ship;

        // Used to calculate stopping distance and as cruising speed for navigation control.
        const double CruiseSpeed = 95;
        const double StoppingDistanceSafetyFactor = 1.1;

        Vector3D TargetPosition { get; set; }

        double maximumPossibleStoppingDistance;

        public ShipNavigation(Program program, Ship ship)
        {
            this.program = program;
            this.ship = ship;

            ship.ReloadBlockReferences();
            ReloadNavigation();
        }

        public void Panic()
        {
            state = State.Off;
            ship.Panic();
        }

        public void Update(double dt)
        {
            switch (state)
            {
                case State.Off:
                    break;

                case State.StationKeeping:
                    UpdateStationKeeping(dt);
                    break;
            }

            ship.Update(dt);
        }

        public void StartStationKeeping(Vector3D position)
        {
            TargetPosition = position;
            state = State.StationKeeping;
        }

        void UpdateStationKeeping(double dt)
        {
            ship.VelocityControlEnabled = true;
            ship.OrientationControlEnabled = true;

            Vector3D positionError = GetPositionError();

            double errorDistance = positionError.Normalize();
            Vector3D positionErrorDirection = positionError;

            program.Log($"Target position: {TargetPosition}");
            program.Log($"Distance to target: {errorDistance}");
            program.Log($"Max stopping distance: {maximumPossibleStoppingDistance}");

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
                    program.Log($"Response: {response}");

                    responseVelocity = positionErrorDirection * response;
                }
                else
                {
                    responseVelocity = positionErrorDirection;
                }

                // TODO: Use explicit cruising speed here.
                ship.TargetVelocity = responseVelocity * CruiseSpeed;

                program.Log($"Target speed: {ship.TargetVelocity.Length()}");
                program.Log($"Actual speed: {ship.GetVelocity().Length()}");
            }
        }

        Vector3D GetPositionError()
        {
            return TargetPosition - ship.GetPosition();
        }

        double CalculateMaximumStoppingDistance(double speed)
        {
            double a = ship.MinPossibleThrustInAnyDirection / ship.Mass;
            return (speed * speed) / (2 * a);
        }

        void ReloadNavigation()
        {
            maximumPossibleStoppingDistance = CalculateMaximumStoppingDistance(CruiseSpeed) * StoppingDistanceSafetyFactor;
        }
    }
}
