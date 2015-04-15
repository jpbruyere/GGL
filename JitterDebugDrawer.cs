using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;
using Jitter.LinearMath;
using OpenTK;
using System.Drawing;

namespace GGL
{
    public class JitterDebugDrawer : Jitter.IDebugDrawer
    {
        public void DrawLine(JVector start, JVector end)
        {
            GL.Begin(BeginMode.Lines);
            GL.Vertex3(new Vector3(start.X, start.Y, start.Z));
            GL.Vertex3(new Vector3(end.X, end.Y, end.Z));
            GL.End();

        }

        public void DrawPoint(JVector pos)
        {
            GL.Begin(BeginMode.Points);
            GL.Vertex3(new Vector3(pos.X, pos.Y, pos.Z));
            GL.End();
        }

        public void DrawTriangle(JVector pos1, JVector pos2, JVector pos3)
        {            
            GL.Begin(BeginMode.Triangles);
            GL.Vertex3(new Vector3(pos1.X, pos1.Y, pos1.Z));
            GL.Vertex3(new Vector3(pos2.X, pos2.Y, pos2.Z));
            GL.Vertex3(new Vector3(pos3.X, pos3.Y, pos3.Z));
            GL.End();            
        }
    }
}
