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

namespace IngameScript
{
    // Proportional-integral-derivative controller.
    // https://nicisdigital.wordpress.com/2011/06/27/proportional-integral-derivative-pid-controller/

    internal class PID
    {
        public double P { get; set; }
        public double I { get; set; }
        public double D { get; set; }
        public double WindupGuard { get; set; }

        private double prevError;
        private double integralError;

        public double Control { get; private set; }

        // ---

        public PID(double p, double i, double d, double windupGuard)
        {
            P = p;
            I = i;
            D = d;
            WindupGuard = windupGuard;

            Control = 0;

            ResetIntegral();
        }

        public void ResetIntegral()
        {
            prevError = 0;
            integralError = 0;
        }

        public double Update(double currError, double dt)
        {
            // integration with windup guarding
            integralError += (currError * dt);

            if (integralError < -(WindupGuard))
            {
                integralError = -(WindupGuard);
            }
            else if (integralError > WindupGuard)
            {
                integralError = WindupGuard;
            }

            // differentiation
            double diff = ((currError - prevError) / dt);

            // scaling
            double P_term = (P * currError);
            double I_term = (I * integralError);
            double D_term = (D * diff);

            // summation of terms
            Control = P_term + I_term + D_term;

            // save current error as previous error for next iteration
            prevError = currError;

            return Control;
        }
    }
}
