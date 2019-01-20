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
                CRUISE_TO_TARGET,
                DECELERATE_AT_TARGET,
                FINETUNE_POSITION_AT_TARGET,
            }

            Program program;

            double CRUISE_SPEED = 100;

            Phase currentPhase = Phase.IDLE;
            public Vector3D targetPosition = Vector3D.Zero;

            Vector3D currentCourseStartPosition;
            Vector3D currentCourseVector;

            public NavigationSystem(Program program) : base(program) {
                this.program = program;

                ToPhase(Phase.IDLE);
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
                        QuaternionD error = GetOrientationError();
                        Vector3D axis;
                        double angle;
                        error.GetAxisAngle(out axis, out angle);
                        program.Log($"Orientation error: {angle}");

                        program.Log($"Velocity: {GetWorldVelocity().Length()}");
                        program.Log($"Velocity error: {GetVelocityError().Length()}");

                        if (GetWorldVelocity().LengthSquared() < 1) {
                            ToPhase(Phase.TURN_TO_TARGET);
                        }
                        break;

                    case Phase.TURN_TO_TARGET:
                        error = GetOrientationError();
                        error.GetAxisAngle(out axis, out angle);

                        program.Log($"Orientation error: {angle}");

                        if (angle < 0.001) {
                            ToPhase(Phase.CRUISE_TO_TARGET);
                        }
                        break;

                    case Phase.CRUISE_TO_TARGET:
                        program.Log($"Velocity: {GetWorldVelocity().Length()}");
                        program.Log($"Velocity error: {GetVelocityError().Length()}");
                        program.Log($"Distance to target: {CalculateDistanceToTarget()}");

                        // -- Stay on the course vector.
                        // Use a coordinate system where the course vector points toward -Z and starts
                        // the origin.
                        MatrixD transformCourseVectorToWorld = MatrixD.CreateFromQuaternion(
                            QuaternionD.CreateFromForwardUp(currentCourseVector, Vector3D.Up));
                        transformCourseVectorToWorld.Translation = currentCourseStartPosition;

                        MatrixD transformWorldToCourseVector = MatrixD.Invert(transformCourseVectorToWorld);

                        /*
                        // Calculate target velocity in course space.
                        Vector3D courseSpaceShipPosition = Vector3D.Transform(GetWorldPosition(), transformWorldToCourseVector);
                        Vector3D courseSpaceTargetPosition = new Vector3D(0, 0, 0);

                        // We're deliberately ignoring the Z component of the error, since
                        // we don't want to be at the start position, we want to be cruising
                        // to the destination.
                        Vector3D courseSpaceError = courseSpaceTargetPosition - courseSpaceShipPosition;
                        courseSpaceError.Z = 0;

                        program.Log($"Course space error: {courseSpaceError}");
                        */

                        Vector3D courseSpaceTargetVelocity = new Vector3D(
                            0,//0.8 * courseSpaceError.X,
                            0,//0.8 * courseSpaceError.Y,
                            CRUISE_SPEED);

                        //TargetVelocity = Vector3D.Transform(courseSpaceTargetVelocity, transformCourseVectorToWorld.GetOrientation());
                        program.Log($"TargetVelocity: {TargetVelocity}");

                        // -- If we get within stopping distance, start decelerating.
                        double stoppingDistance = CalculateStoppingDistance(GetWorldVelocity().Length());
                        program.Log($"Stopping distance: {stoppingDistance}");

                        if (CalculateDistanceToTarget() <= stoppingDistance) {
                            ToPhase(Phase.DECELERATE_AT_TARGET);
                        }
                        break;

                    case Phase.DECELERATE_AT_TARGET:
                        program.Log($"Velocity: {GetWorldVelocity().Length()}");
                        program.Log($"Velocity error: {GetVelocityError().Length()}");

                        if (GetWorldVelocity().LengthSquared() < 1) {
                            ToPhase(Phase.IDLE);
                        }
                        break;

                    case Phase.FINETUNE_POSITION_AT_TARGET:
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
                        SetAutoControlEnabled(false);
                        break;

                    case Phase.PRE_NAVIGATION:
                        SetAutoControlEnabled(true);

                        Vector3D velocityDirection = GetWorldVelocity();
                        double currentSpeed = velocityDirection.Normalize();

                        if (currentSpeed > 30) {
                            // Face backwards so rear thrusters can be used to slow down.
                            Vector3D forward = -velocityDirection;
                            forward.Normalize();
                            TargetOrientation = QuaternionD.CreateFromForwardUp(forward, Vector3D.Up);
                        }

                        TargetVelocity = Vector3D.Zero;
                        break;

                    case Phase.TURN_TO_TARGET:
                        PointAt(targetPosition);
                        break;

                    case Phase.CRUISE_TO_TARGET:
                        currentCourseStartPosition = GetWorldPosition();

                        Vector3D toTarget = CalculateVectorToTarget();
                        currentCourseVector = toTarget;

                        toTarget.Normalize();
                        TargetVelocity = toTarget * 100;
                        break;

                    case Phase.DECELERATE_AT_TARGET:
                        TargetVelocity = Vector3D.Zero;
                        break;

                    case Phase.FINETUNE_POSITION_AT_TARGET:
                        break;

                    default:
                        break;
                }

                currentPhase = phase;
            }

            void PointAt(Vector3D targetPosition) {
                Vector3D forward = CalculateVectorToTarget();
                forward.Normalize();
                TargetOrientation = QuaternionD.CreateFromForwardUp(forward, Vector3D.Up);
            }

            Vector3D CalculateVectorToTarget() {
                return targetPosition - GetWorldPosition();
            }

            double CalculateDistanceToTarget() {
                return CalculateVectorToTarget().Length();
            }

            double CalculateStoppingDistance(double speed) {
                double a = MinPossibleThrustInAnyDirection / Mass;
                program.Log($"a:{a} minthrust:{MinPossibleThrustInAnyDirection} mass:{Mass}");
                return (speed * speed) / (2 * a);
            }
        }
    }
}
