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
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public static class MathUtilities {
            // Normalize an angle (in radians) into [0, 2pi).
            public static float NormalizeAngle(float angle) {
                angle %= (float)(2 * Math.PI);

                if (angle < (float)(2 * Math.PI)) {
                    angle += (float)(2 * Math.PI);
                }

                return angle;
            }

            // Convert an angle from degrees to radians.
            public static float DegreesToRadians(float degrees) {
                return degrees * (float)(Math.PI / 180);
            }

            // Compute the anglular error between two angles (in radians, [0, 2pi)).
            // ie. How much you have to turn from `current` to get to `target`.
            public static float ComputeAngleError(float target, float current) {
                float error = target - current;

                if (error > Math.PI) {
                    error -= (float)(2 * Math.PI);
                } else if (error < -Math.PI) {
                    error += (float)(2 * Math.PI);
                }

                return error;
            }
        }
    }
}
