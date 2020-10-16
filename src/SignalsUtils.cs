using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace signals.src
{
    public class SignalsUtils
    {
        private static Dictionary<string, Vec3f> rotations = new Dictionary<string, Vec3f>() {
            { "*-north-down", new Vec3f(0,0,0) },
            { "*-south-down", new Vec3f(0, 180, 0) },
            {"*-east-down", new Vec3f(0, 270, 0) },
            {"*-west-down", new Vec3f(0, 90, 0) },


            { "*-north-up", new Vec3f(180,180,0) },
            { "*-south-up", new Vec3f(180, 0, 0) },
            {"*-east-up", new Vec3f(180, 270, 0) },
            {"*-west-up", new Vec3f(180, 90, 0) },

            { "*-north-west", new Vec3f(180,180,90) },
            { "*-south-west", new Vec3f(0, 180, 90) },
            {"*-up-west", new Vec3f(270, 180, 90) },
            {"*-down-west", new Vec3f(90, 180, 90) },

            { "*-north-east", new Vec3f(0,0,90) },
            { "*-south-east", new Vec3f(180, 0, 90) },
            {"*-up-east", new Vec3f(90, 0, 90) },
            {"*-down-east", new Vec3f(270, 0, 90) },

            { "*-east-north", new Vec3f(90,270,0) },
            { "*-west-north", new Vec3f(90, 90, 0) },
            {"*-up-north", new Vec3f(90, 0, 0) },
            {"*-down-north", new Vec3f(90, 180, 0) },

            { "*-east-south", new Vec3f(270,270,0) },
            { "*-west-south", new Vec3f(270, 90, 0) },
            {"*-up-south", new Vec3f(270, 180, 0) },
            {"*-down-south", new Vec3f(270, 0, 0) }


        };

        public static Vec3f FacingToRotation(BlockFacing orientation, BlockFacing side)
        {
            string key = "*-" + orientation.Code + "-" + side.Code;
            if (!rotations.ContainsKey(key)) return new Vec3f(0,0,0);
            return rotations[key].Clone();
        }

        public static void RotateVector(ref Vec3f vector, float degX, float degY, float degZ, Vec3f origin)
        {
            float radX = degX * GameMath.DEG2RAD;
            float radY = degY * GameMath.DEG2RAD;
            float radZ = degZ * GameMath.DEG2RAD;

            float[] matrix = Mat4f.Create();
            Mat4f.RotateX(matrix, matrix, radX);
            Mat4f.RotateY(matrix, matrix, radY);
            Mat4f.RotateZ(matrix, matrix, radZ);

            float[] pos = new float[] { 0, 0, 0, 1 };

            float[] vec = new float[] { vector.X - (float)origin.X, vector.Y - (float)origin.Y, vector.Z - (float)origin.Z, 1 };
            vec = Mat4f.MulWithVec4(matrix, vec);

            vector.X = vec[0] + origin.X;
            vector.Y = vec[1] + origin.Y;
            vector.Z = vec[2] + origin.Z;
        }


        public static CircuitComponent GetCircuitComponentFromItem(ICoreAPI api, Item item)
        {
            SignalsMod mod = api.ModLoader.GetModSystem<SignalsMod>();
            JsonObject jsonObj = item.Attributes?["circuitComponent"];
            if (jsonObj == null) return null;
            string className = jsonObj["class"]?.AsString();
            Type type = mod.getCircuitComponentType(className);
            return (CircuitComponent)Activator.CreateInstance(type);

        }
        public static Vec3i GetCircuitComponentSizeFromItem(ICoreAPI api, Item item)
        {
            if (item == null) return null;
            SignalsMod mod = api.ModLoader.GetModSystem<SignalsMod>();
            JsonObject jsonObj = item.Attributes?["circuitComponent"];
            if (jsonObj == null) return null;
            Vec3i size = jsonObj["size"]?.AsObject<Vec3i>() ;

            return size;
        }
    }
}
