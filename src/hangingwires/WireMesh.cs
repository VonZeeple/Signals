
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace signals.src.hangingwires
{
    class WireMesh
    {
        static float Catenary(float x, float d=1, float a=2)
        {
            return a*((float)Math.Cosh((x-(d/2))/a) - (float)Math.Cosh((d / 2) / a));
        }

        static Vec3f CrossProduct(Vec3f v1, Vec3f v2)
        {
            float x, y, z;
            x = v1.Y * v2.Z - v2.Y * v1.Z;
            y = (v1.X * v2.Z - v2.X * v1.Z) * -1;
            z = v1.X * v2.Y - v2.X * v1.Y;

            var rtnvector = new Vec3f(x, y, z);
            rtnvector.Normalize();
            return rtnvector;
        }

        static public MeshData MakeWireMesh(Vec3f pos1, Vec3f pos2)
        {

            float t = 0.015f;//thickness
            Vec3f dPos = pos2 - pos1;
            float dist = pos2.DistanceTo(pos1);

            int nSec = (int)Math.Floor(dist * 2);//number of section
            nSec = nSec > 5 ? nSec : 5;

            MeshData mesh = new MeshData(4, 6);
            mesh.SetMode(EnumDrawMode.Triangles);


            MeshData mesh_top = new MeshData(4, 6);
            mesh_top.SetMode(EnumDrawMode.Triangles);

            MeshData mesh_bot = new MeshData(4, 6);
            mesh_bot.SetMode(EnumDrawMode.Triangles);

            MeshData mesh_side = new MeshData(4, 6);
            mesh_side.SetMode(EnumDrawMode.Triangles);

            MeshData mesh_side2 = new MeshData(4, 6);
            mesh_side2.SetMode(EnumDrawMode.Triangles);

            //out of plane translation vector:
            Vec3f b = new Vec3f(-dPos.Z, 0, dPos.X).Normalize();
            if (dPos.Z == 0 && dPos.X == 0)
            {
                b = new Vec3f(1, 0, 0);
            }

            mesh_top.Flags.Fill(0);
            mesh_bot.Flags.Fill(0);
            mesh_side.Flags.Fill(0);
            mesh_side2.Flags.Fill(0);

            Vec3f[] positions = [];
            for (int j = 0; j <= nSec; j++)
            {
                float x = dPos.X / nSec * j;
                float y = dPos.Y / nSec * j;
                float z = dPos.Z / nSec * j;
                float l = (float)Math.Sqrt(x * x + y * y + z * z);
                float dy = Catenary(l / dist, 1, 2f);
                positions = positions.Append(new Vec3f(x, y + dy, z));
            }

            Vec3f pos;
            Vec3f pos_next;
            Vec3f pos_before;
            Vec3f direction;
            Vec3f a;

            for (int j = 0; j <= nSec; j++)
            {
                pos = pos1 + positions[j];
                pos_next = j < nSec ? positions[j + 1] : positions[j];
                pos_before = j > 0 ? positions[j - 1] : positions[j];
                direction = (pos_next + pos_before).Normalize();

                a = CrossProduct(direction, b * -1);

                float du = dist / 2 / t / nSec;
                int color = 1;
                mesh_top.AddVertex(
                    (pos - b * t + a * t).X,
                    (pos - b * t + a * t).Y,
                    (pos - b * t + a * t).Z,
                    j * du, 0, color);
                mesh_top.AddVertex(
                    (pos + b * t + a * t).X,
                    (pos + b * t + a * t).Y,
                    (pos + b * t + a * t).Z,
                    j * du, 1, color);

                mesh_bot.AddVertex(
                    (pos - b * t - a * t).X,
                    (pos - b * t - a * t).Y,
                    (pos - b * t - a * t).Z,
                    j * du, 0, color);
                mesh_bot.AddVertex(
                    (pos + b * t - a * t).X,
                    (pos + b * t - a * t).Y,
                    (pos + b * t - a * t).Z,
                    j * du, 1,
                    color);

                mesh_side.AddVertex(
                    (pos - b * t + a * t).X,
                    (pos - b * t + a * t).Y,
                    (pos - b * t + a * t).Z,
                    j * du, 1, color);
                mesh_side.AddVertex(
                    (pos - b * t - a * t).X,
                    (pos - b * t - a * t).Y,
                    (pos - b * t - a * t).Z,
                    j * du, 0, color);


                mesh_side2.AddVertex(
                    (pos + b * t + a * t).X,
                    (pos + b * t + a * t).Y,
                    (pos + b * t + a * t).Z,
                    j * du, 1, color);
                mesh_side2.AddVertex(
                    (pos + b * t - a * t).X,
                    (pos + b * t - a * t).Y,
                    (pos + b * t - a * t).Z,
                    j * du, 0, color);


                mesh_top.Flags[2 * j] = VertexFlags.PackNormal(new Vec3f(0, 1, 0));
                mesh_top.Flags[2 * j + 1] = VertexFlags.PackNormal(new Vec3f(0, 1, 0));

                mesh_bot.Flags[2 * j] = VertexFlags.PackNormal(new Vec3f(0, -1, 0));
                mesh_bot.Flags[2 * j + 1] = VertexFlags.PackNormal(new Vec3f(0, -1, 0));

                mesh_side.Flags[2 * j] = VertexFlags.PackNormal(-b.X, -b.Y, -b.Z);
                mesh_side.Flags[2 * j + 1] = VertexFlags.PackNormal(-b.X, -b.Y, -b.Z);

                mesh_side2.Flags[2 * j] = VertexFlags.PackNormal(b);
                mesh_side2.Flags[2 * j + 1] = VertexFlags.PackNormal(b);

            }

            //add indices
            for (int j = 0; j < nSec; j++)
            {
                //upper stripe
                int offset = 2 * j;
                mesh_top.AddIndex(offset);
                mesh_top.AddIndex(offset + 3);
                mesh_top.AddIndex(offset + 2);
                mesh_top.AddIndex(offset);
                mesh_top.AddIndex(offset + 1);
                mesh_top.AddIndex(offset + 3);

                //lower stripe
                mesh_bot.AddIndex(offset);
                mesh_bot.AddIndex(offset + 3);
                mesh_bot.AddIndex(offset + 1);
                mesh_bot.AddIndex(offset);
                mesh_bot.AddIndex(offset + 2);
                mesh_bot.AddIndex(offset + 3);

                //sides
                mesh_side.AddIndex(offset);
                mesh_side.AddIndex(offset + 3);
                mesh_side.AddIndex(offset + 1);
                mesh_side.AddIndex(offset);
                mesh_side.AddIndex(offset + 2);
                mesh_side.AddIndex(offset + 3);


                mesh_side2.AddIndex(offset);
                mesh_side2.AddIndex(offset + 3);
                mesh_side2.AddIndex(offset + 2);
                mesh_side2.AddIndex(offset);
                mesh_side2.AddIndex(offset + 1);
                mesh_side2.AddIndex(offset + 3);

            }

            mesh.AddMeshData(mesh_top);
            mesh.AddMeshData(mesh_bot);
            mesh.AddMeshData(mesh_side);
            mesh.AddMeshData(mesh_side2);
            mesh.Rgba.Fill((byte)255);


            return mesh;
        }
    } 
}