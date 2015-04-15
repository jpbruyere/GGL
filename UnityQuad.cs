using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;

namespace GGL
{
    public static class UnityQuad
    {
        static int dlUnityQuad;
        static UnityQuad()
        {
            dlUnityQuad = GL.GenLists(1);
            GL.NewList(dlUnityQuad, ListMode.Compile);
            {
                GL.Begin(BeginMode.Quads);
                {
                    GL.TexCoord2(0, 0); GL.Vertex2(-1f, -1f);
                    GL.TexCoord2(1, 0); GL.Vertex2(1f, -1f);
                    GL.TexCoord2(1, 1); GL.Vertex2(1f, 1f);
                    GL.TexCoord2(0, 1); GL.Vertex2(-1f, 1f);
                }
                GL.End();
            }
            GL.EndList();
        }

        public static void Draw()
        {
            GL.CallList(dlUnityQuad);
        }

    }
}
