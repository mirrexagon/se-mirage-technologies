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
    internal class GPSLocation
    {
        public string Name { get; private set; }
        public Vector3D Position { get; private set; }
        public Color Color { get; private set; }

        public static GPSLocation FromString(string gpsString)
        {
            string[] fields = gpsString.Split(':');

            if (fields[0] != "GPS")
            {
                throw new FormatException($"'{gpsString}' is not a GPS string");
            }

            string name = fields[1];
            double x, y, z;
            Color color;
            bool xOk = double.TryParse(fields[2], out x);
            bool yOk = double.TryParse(fields[3], out y);
            bool zOk = double.TryParse(fields[4], out z);
            bool colorOk = TryParseColor(fields[5], out color);

            if (xOk && yOk && zOk && colorOk)
            {
                Vector3D position = new Vector3D(x, y, z);

                return new GPSLocation
                {
                    Name = name,
                    Position = position,
                    Color = color,
                };
            }
            else
            {
                throw new FormatException($"GPS string '{gpsString}' is formatted incorrectly");
            }
        }
        //GPS:Mirrexagon #2:10.65:-159.46:93.75:#FF75C9F1:
        static bool TryParseColor(string colorString, out Color color)
        {
            color = Color.Black;

            if (colorString.Length != 9 || colorString.Substring(0, 1) != "#")
            {
                return false;
            }

            int r, g, b, a;
            bool rOk = int.TryParse(colorString.Substring(1, 2), System.Globalization.NumberStyles.HexNumber, null, out r);
            bool gOk = int.TryParse(colorString.Substring(3, 2), System.Globalization.NumberStyles.HexNumber, null, out g);
            bool bOk = int.TryParse(colorString.Substring(5, 2), System.Globalization.NumberStyles.HexNumber, null, out b);
            bool aOk = int.TryParse(colorString.Substring(7, 2), System.Globalization.NumberStyles.HexNumber, null, out a);

            if (rOk && gOk && bOk && aOk)
            {
                color = new Color(r, g, b, a);
                return true;
            }
            else
            {
                return false;
            }
        }

        public override string ToString()
        {
            string colorString = $"#{Color.R:X02}{Color.G:X02}{Color.B:X02}{Color.A:X02}";

            return $"GPS:{Name}:{Position.X}:{Position.Y}:{Position.Z}:{colorString}:";
        }
    }
}