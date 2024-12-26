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
        Program program;

        // Features on/off
        bool _orientationControlEnabled = false;
        public bool OrientationControlEnabled
        {
            get { return _orientationControlEnabled; }
            set
            {
                SetGyroOverrideEnabled(value);
                _orientationControlEnabled = value;
            }
        }

        bool _velocityControlEnabled = false;
        public bool VelocityControlEnabled
        {
            get { return _velocityControlEnabled; }
            set
            {
                SetInertialDampenersEnabled(!value);
                _velocityControlEnabled = value;
            }
        }

        // Ship controllers
        List<IMyShipController> shipControllers;
        List<IMyRemoteControl> remoteControls;
        List<IMyCockpit> cockpits;

        IMyShipController orientationReference;

        // Useful ship information.
        public double Mass { get; private set; }

        public Ship(Program program)
        {
            this.program = program;

            ReloadBlockReferences();

            TargetOrientation = GetWorldMatrix().GetOrientation();
            TargetVelocity = Vector3D.Zero;
        }

        public void StopControl()
        {
            OrientationControlEnabled = false;
            VelocityControlEnabled = false;
            SetInertialDampenersEnabled(true);
            SetThrustToZero();
        }

        public virtual void Update(double dt)
        {
            if (_orientationControlEnabled)
            {
                UpdateOrientationControl(dt);
            }

            if (_velocityControlEnabled)
            {
                UpdateVelocityControl(dt);
            }
        }

        public void SetInertialDampenersEnabled(bool enabled)
        {
            foreach (IMyShipController shipController in shipControllers)
            {
                shipController.DampenersOverride = enabled;
            }
        }

        // Get local-to-world matrix centered at center of mass and with orientation of the orientation reference.
        public MatrixD GetWorldMatrix()
        {
            MatrixD gridCenterOfMassWorldMatrix = orientationReference.WorldMatrix.GetOrientation();
            gridCenterOfMassWorldMatrix.Translation = orientationReference.CenterOfMass;
            return gridCenterOfMassWorldMatrix;
        }

        // ---

        public void ReloadBlockReferences()
        {
            ReloadShipControllerReferences();
            orientationReference = FindOrientationReference();
            Mass = orientationReference.CalculateShipMass().PhysicalMass;

            ReloadRotationBlockReferences();
            ReloadThrustBlockReferences();
        }

        IMyShipController FindOrientationReference()
        {
            ReloadShipControllerReferences();

            IMyRemoteControl mainRemoteControl = remoteControls.FindAll(rc => rc.IsMainCockpit).FirstOrDefault();
            if (mainRemoteControl != null)
            {
                return mainRemoteControl;
            }

            IMyCockpit mainCockpit = cockpits.FindAll(c => c.IsMainCockpit).FirstOrDefault();
            if (mainCockpit != null)
            {
                return mainCockpit;
            }

            if (remoteControls.Count > 0)
            {
                return remoteControls[0];
            }

            if (cockpits.Count > 0)
            {
                return cockpits[0];
            }

            return null;
        }

        void ReloadShipControllerReferences()
        {
            shipControllers = new List<IMyShipController>();
            program.GridTerminalSystem.GetBlocksOfType(shipControllers);

            remoteControls = new List<IMyRemoteControl>();
            program.GridTerminalSystem.GetBlocksOfType(remoteControls);

            cockpits = new List<IMyCockpit>();
            program.GridTerminalSystem.GetBlocksOfType(cockpits);
        }
    }
}
