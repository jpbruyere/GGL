using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Runtime.InteropServices;
using System.Drawing;

namespace GGL
{

    class tree
    {

        public Matrix4 transform = Matrix4.Identity;
        public bool drawVertex = false;

        public string name { get; set; }

        public Vertex[] Vertices { get; set; }

        int verticesBufferId;

        //public void Prepare()
        //{

        //    GL.GenBuffers(1, out verticesBufferId);
        //    GL.BindBuffer(BufferTarget.ArrayBuffer, verticesBufferId);
        //    GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(Vertices.Length * Marshal.SizeOf(typeof(Vertex))), Vertices, BufferUsageHint.DynamicDraw);


        //}
        //public void Render()
        //{
        //    GL.PushClientAttrib(ClientAttribMask.ClientVertexArrayBit);
        //    GL.EnableClientState(EnableCap.VertexArray);

        //    GL.BindBuffer(BufferTarget.ArrayBuffer, verticesBufferId);
        //    GL.InterleavedArrays(InterleavedArrayFormat.T2fN3fV3f, Marshal.SizeOf(typeof(Vertex)), IntPtr.Zero);


        //        GL.PushMatrix();
        //        GL.MultMatrix(ref transform);

        //        GL.PushAttrib(AttribMask.EnableBit);

        //        GL.ActiveTexture(texUnit);
        //        GL.BindTexture(TextureTarget.Texture2D, texture);

        //        GL.BindBuffer(BufferTarget.ElementArrayBuffer, trianglesBufferId);
        //        GL.DrawElements(BeginMode.Points, Triangles.Length * 3, DrawElementsType.UnsignedInt, IntPtr.Zero);


        //        GL.PopAttrib();

        //        GL.PopMatrix();
        //    }

        //    GL.PopClientAttrib();
        //}
    }






}
