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
        public class GPSLocation {
            public string name;
            public Vector3D position;

            public GPSLocation(string name, Vector3D position) {
                this.name = name;
                this.position = position;
            }

            public static GPSLocation FromString(string gpsString) {
                string[] fields = gpsString.Split(':');

                if (fields[0] != "GPS") {
                    return null;
                }

                string name = fields[1];
                double x, y, z;
                bool xOk = Double.TryParse(fields[2], out x);
                bool yOk = Double.TryParse(fields[3], out y);
                bool zOk = Double.TryParse(fields[4], out z);

                if (xOk && yOk && zOk) {
                    Vector3D position = new Vector3D(x, y, z);

                    return new GPSLocation(name, position);
                } else {
                    return null;
                }
            }
        }
    }
}