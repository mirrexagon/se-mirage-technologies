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
        class NavigationSystem : Ship {
            enum Phase {
                IDLE,

                // Stop before navigating.
                PRE_NAVIGATION,

                TURN_TO_TARGET,
                ACCELERATE_TO_TARGET,
                CRUISE_TO_TARGET,
                DECELERATE_AT_TARGET,
            }

            Program program;
            public Vector3D targetPosition = Vector3D.Zero;
            Phase currentPhase = Phase.IDLE;

            public NavigationSystem(Program program) : base(program) {
                this.program = program;
            }

            public void SetTargetPosition(Vector3D targetPosition) {
                this.targetPosition = targetPosition;
                ToPhase(Phase.PRE_NAVIGATION);
            }

            public override void Update(double dt) {
                switch (currentPhase) {
                    case Phase.IDLE:
                        break;

                    case Phase.PRE_NAVIGATION:
                        if (GetWorldVelocity().LengthSquared() < 1) {
                            ToPhase(Phase.TURN_TO_TARGET);
                        }
                        break;

                    case Phase.TURN_TO_TARGET:
                        QuaternionD error = GetOrientationError();
                        Vector3D axis;
                        double angle;
                        error.GetAxisAngle(out axis, out angle);

                        program.Log($"Error: {angle}");

                        if (angle < 0.001) {
                            ToPhase(Phase.IDLE);
                        }
                        break;

                    case Phase.ACCELERATE_TO_TARGET:
                        break;

                    case Phase.CRUISE_TO_TARGET:
                        break;

                    case Phase.DECELERATE_AT_TARGET:
                        break;

                    default:
                        break;
                }

                program.Log($"Current phase: {currentPhase}");

                base.Update(dt);
            }

            void ToPhase(Phase phase) {
                switch (phase) {
                    case Phase.IDLE:
                        SetGyroOverrideEnabled(false);
                        SetInertialDampenersEnabled(true);
                        break;

                    case Phase.PRE_NAVIGATION:
                        SetGyroOverrideEnabled(true);
                        SetInertialDampenersEnabled(false);

                        Vector3D velocityDirection = GetWorldVelocity();
                        double currentSpeed = velocityDirection.Normalize();

                        if (currentSpeed > 30) {
                            // Face backwards so rear thrusters can be used to slow down.
                            PointAt(-velocityDirection);
                        }

                        TargetVelocity = Vector3D.Zero;
                        break;

                    case Phase.TURN_TO_TARGET:
                        PointAt(targetPosition);
                        break;

                    case Phase.ACCELERATE_TO_TARGET:
                        break;

                    case Phase.CRUISE_TO_TARGET:
                        break;

                    case Phase.DECELERATE_AT_TARGET:
                        break;

                    default:
                        break;
                }

                currentPhase = phase;
            }

            void PointAt(Vector3D targetPosition) {
                Vector3D forward = targetPosition - GetWorldPosition();
                forward.Normalize();
                Vector3D up = Vector3D.Up;
                TargetOrientation = QuaternionD.CreateFromForwardUp(forward, up);
            }
        }
    }
}
