using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using System.Drawing.Imaging;
using GGL;
using Examples.Shapes;

namespace GGL
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        public Vector2 TexCoord;
        public Vector3 Normal;
        public Vector3 position;
    }
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Triangle
    {
        public int Index0;
        public int Index1;
        public int Index2;
    }
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Quad
    {
        public int Index0;
        public int Index1;
        public int Index2;
        public int Index3;
    }
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct QuadStrip
    {
        public int Index0;
        public int Index1;
    }
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct TriangleFan
    {
        public int Index0;
    }

    [Serializable]
    public class Mesh
    {
		public Mesh(){}
		public Mesh(BOquads boq)
		{
			Vertices = boq.Vertices;
			FaceGroup fg = new FaceGroup ();
			fg.Quads = boq.Quads;
			this.Faces.Add (fg);
		}
        public static bool ShowBoundingBox = false;
        public static bool ShowNormals = false;
        public static bool ShowVertices = false;
        public static bool ShowID = false;
#if DEBUG
#endif
        public Matrix4 transform = Matrix4.Identity;
        public BoundingBox bounds = null;

        public string name { get; set; }
        public Vertex[] Vertices { get; set; }
        public List<FaceGroup> Faces = new List<FaceGroup>();

        int verticesBufferId;

        public void ComputeBounds()
        {
            bounds = new BoundingBox();

            bounds.X0Y0Z0 = new Vector3(10000, 10000, 10000);
            bounds.X1Y1Z1 = Vector3.Zero;

            for (int i = 0; i < Vertices.Length; i++)
            {
                bounds.X0Y0Z0 = Vector3.ComponentMin(bounds.X0Y0Z0, Vertices[i].position);
                bounds.X1Y1Z1 = Vector3.ComponentMax(bounds.X1Y1Z1, Vertices[i].position);
            }
        }
		public Vector3 centre
		{
			get
			{
				float x = 0,
				y = 0,
				z = 0;

				for (int i = 0; i < Vertices.Length; i++)
				{
					x += Vertices[i].position.X;
					y += Vertices[i].position.Y;
					z += Vertices[i].position.Z;
				}

				Vector3 vRes = new Vector3(x / Vertices.Length, y / Vertices.Length, z / Vertices.Length);

				return vRes;
			}
		}
        
		public void Prepare()
        {
			if (verticesBufferId > 0)
				GL.DeleteBuffer (verticesBufferId);

            GL.GenBuffers(1, out verticesBufferId);
            GL.BindBuffer(BufferTarget.ArrayBuffer, verticesBufferId);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(Vertices.Length * Marshal.SizeOf(typeof(Vertex))), Vertices, BufferUsageHint.StaticDraw);

            foreach (FaceGroup fg in Faces)
                fg.prepare();
        }
        public virtual void Render()
        {
            GL.PushAttrib(AttribMask.EnableBit);
            GL.PushClientAttrib(ClientAttribMask.ClientVertexArrayBit);
            GL.PushMatrix();

            GL.EnableClientState(EnableCap.VertexArray);

            GL.BindBuffer(BufferTarget.ArrayBuffer, verticesBufferId);
            GL.InterleavedArrays(InterleavedArrayFormat.T2fN3fV3f, Marshal.SizeOf(typeof(Vertex)), IntPtr.Zero);

            GL.MultMatrix(ref transform);

            foreach (FaceGroup fg in Faces)
            {
                fg.render();
            }
#if DEBUG
            GL.Disable(EnableCap.Texture2D);
            GL.Disable(EnableCap.Lighting);

            if (ShowBoundingBox)
                bounds.render();
            if (ShowNormals)
            {
                GL.Color3(Color.Blue);
                GL.LineWidth(1.0f);
                GL.Begin(BeginMode.Lines);
                foreach (Vertex v in Vertices)
                {
                    GL.Vertex3(v.position);
                    GL.Vertex3(v.position + v.Normal * 1f);
                }
                GL.End();
            }
            if (ShowVertices)
            {
                GL.Color3(Color.Yellow);
                GL.PointSize(2.0f);
                GL.Begin(BeginMode.Points);
                foreach (Vertex v in Vertices)
                {
                    GL.Vertex3(v.position);
                }
                GL.End();
            }

#endif
            GL.PopMatrix();
            GL.PopClientAttrib();
            GL.PopAttrib();
        }
			
        public static Mesh Plane(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Mesh m = new Mesh();
            FaceGroup fg = new FaceGroup();

            m.Vertices = new Vertex[4];
            fg.Quads = new Quad[1];

            //m.Vertices[0] = new Vertex { position = new Vector3(0, 100, 0), TexCoord = new Vector2(0, 0), Normal = new Vector3(0, 0, 0) };
            //m.Vertices[1] = new Vertex { position = new Vector3(0, 0, 0), TexCoord = new Vector2(1, 0), Normal = new Vector3(0, 0, 0) };
            //m.Vertices[2] = new Vertex { position = new Vector3(100, 0, 0), TexCoord = new Vector2(1, 1), Normal = new Vector3(0, 0, 0) };
            //m.Vertices[3] = new Vertex { position = new Vector3(100, 100, 0), TexCoord = new Vector2(0, 1), Normal = new Vector3(0, 0, 0) };
            m.Vertices[0] = new Vertex { position = a, TexCoord = new Vector2(0, 0), Normal = new Vector3(0, 0, 1) };
            m.Vertices[1] = new Vertex { position = b, TexCoord = new Vector2(1, 0), Normal = new Vector3(0, 0, 1) };
            m.Vertices[2] = new Vertex { position = c, TexCoord = new Vector2(1, 1), Normal = new Vector3(0, 0, 1) };
            m.Vertices[3] = new Vertex { position = d, TexCoord = new Vector2(0, 1), Normal = new Vector3(0, 0, 1) };

            Quad q = new Quad();
            q.Index0 = 0;
            q.Index1 = 1;
            q.Index2 = 2;
            q.Index3 = 3;
            fg.Quads[0] = q;

            m.Faces.Add(fg);

            return m;
        }
    
		public static Mesh operator +(Mesh m, BOquads q)
		{
//			int startIndex = m.Vertices.Length;
//
//			Array.Resize<Vertex> (ref m.Vertices, m.Vertices.Length + q.Vertices.Length);
//			Array.Copy (q.Vertices, m.Vertices, q.Vertices.Length);
//
//			FaceGroup fg = new FaceGroup ();
//			fg.Quads = new Quad[q.Quads.Length];
//			Array.Copy (q.Quads, fg.Quads, q.Quads.Length);
//			foreach (Quad i in fg.Quads) {
//				i.Index0 += startIndex;
//				i.Index1 += startIndex;
//				i.Index2 += startIndex;
//				i.Index3 += startIndex;
//			}
			return m;
		}
	}

    [Serializable]
    public class FaceGroup
    {
        public static int currentName = 0;
        public static bool wireFrame = false;

		int trianglesBufferId;
		int quadsBufferId;
		public Triangle[] Triangles { get; set; }
		public Quad[] Quads { get; set; }

        public int texture = 0;
        public Shader shader;        
        Material _material;
        
		public Material material
        {
            get { return _material; }
            set
            {
                _material = value;                
            }

        }
			
        public void prepare()
        {
            if (material != null)
                texture = material.DiffuseMap;

            if (Triangles != null)
            {
				if (trianglesBufferId > 0)
					GL.DeleteBuffer (trianglesBufferId);
                GL.GenBuffers(1, out trianglesBufferId);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, trianglesBufferId);
                GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(Triangles.Length * Marshal.SizeOf(typeof(Triangle))), Triangles, BufferUsageHint.StaticDraw);
            }
            if (Quads != null)
            {
				if (quadsBufferId > 0)
					GL.DeleteBuffer (quadsBufferId);
                GL.GenBuffers(1, out quadsBufferId);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, quadsBufferId);
                GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(Quads.Length * Marshal.SizeOf(typeof(Quad))), Quads, BufferUsageHint.StaticDraw);
            }
        }
        public void render()
        {
            //GL.LoadName(currentName);
            //currentName++;

            GL.PushAttrib(AttribMask.EnableBit);

			if (material != null)
				material.Enable();

            if (wireFrame)
            {
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                GL.LineWidth(1f);
                GL.Disable(EnableCap.Lighting);
                GL.Disable(EnableCap.Texture2D);
            }
            else
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

            if (trianglesBufferId > 0)
            {
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, trianglesBufferId);
                GL.DrawElements(BeginMode.Triangles, Triangles.Length *3 , DrawElementsType.UnsignedInt, IntPtr.Zero);
            }
            if (Quads.Length > 0)
            {
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, quadsBufferId);
                GL.DrawElements(BeginMode.Quads, Quads.Length * 4, DrawElementsType.UnsignedInt, IntPtr.Zero);
            }
				
            GL.PopAttrib();

			if (material != null)
				material.Disable();
        }

        public int findFirstTriangleByVertexIDInIndex0(int vertexID)
        {
            for (int i = 0; i < Triangles.Length; i++)
            {
                if (Triangles[i].Index0 == vertexID)
                    return i;
            }
            return -1;
        }
    }

    [Serializable]
    public class BoundingBox
    {
        public Vector3 X0Y0Z0;
        public Vector3 X1Y1Z1;

        BOquads box;
        public void renderBox()
        {
            if (box == null)
            {
                box = BOquads.createBox2(width, length, height);
                box.Prepare();
            }

            box.Render();
        }

        int dispList = 0;
        public void render()
        {
            if (dispList > 0)
            {
                GL.CallList(dispList);
                return;
            }

            dispList = GL.GenLists(1);
            GL.NewList(dispList, ListMode.CompileAndExecute);

            GL.PushAttrib(AttribMask.EnableBit);
            GL.Disable(EnableCap.Texture2D);
            GL.Disable(EnableCap.Lighting);

            GL.LineWidth(1.5f);

            GL.Color3(Color.Red);
            GL.Begin(BeginMode.Lines);
            {
                GL.Vertex3(x0, y0, z0);
                GL.Vertex3(x0, y0, z1);

                GL.Vertex3(x0, y0, z0);
                GL.Vertex3(x0, y1, z0);

                GL.Vertex3(x0, y0, z0);
                GL.Vertex3(x1, y0, z0);

                GL.Vertex3(x0, y1, z0);
                GL.Vertex3(x0, y1, z1);

                GL.Vertex3(x0, y1, z0);
                GL.Vertex3(x1, y1, z0);

                GL.Vertex3(x1, y1, z1);
                GL.Vertex3(x1, y0, z1);

                GL.Vertex3(x1, y1, z1);
                GL.Vertex3(x0, y1, z1);

                GL.Vertex3(x1, y1, z1);
                GL.Vertex3(x1, y1, z0);

                GL.Vertex3(x0, y0, z1);
                GL.Vertex3(x0, y1, z1);

                GL.Vertex3(x0, y0, z1);
                GL.Vertex3(x1, y0, z1);

                GL.Vertex3(x1, y0, z0);
                GL.Vertex3(x1, y1, z0);

                GL.Vertex3(x1, y0, z0);
                GL.Vertex3(x1, y0, z1);
            }
            GL.End();

            GL.PopAttrib();

            GL.EndList();
        }


        public static BoundingBox operator +(BoundingBox b1, BoundingBox b2)
        {

            BoundingBox res = new BoundingBox();
            res.X0Y0Z0 = Vector3.ComponentMin(b1.X0Y0Z0, b2.X0Y0Z0);
            res.X1Y1Z1 = Vector3.ComponentMax(b1.X1Y1Z1, b2.X1Y1Z1);
            return res;
        }

        public float x0
        { get { return X0Y0Z0.X; } }
        public float y0
        { get { return X0Y0Z0.Y; } }
        public float z0
        { get { return X0Y0Z0.Z; } }
        public float x1
        { get { return X1Y1Z1.X; } }
        public float y1
        { get { return X1Y1Z1.Y; } }
        public float z1
        { get { return X1Y1Z1.Z; } }

        public float width
        { get { return Math.Abs(x0 - x1); } }
        public float height
        { get { return Math.Abs(z0 - z1); } }
        public float length
        { get { return Math.Abs(y0 - y1); } }

        public Vector3 center
        {
            get
            {
                return Vector3.Lerp(X0Y0Z0, X1Y1Z1, 0.5f);
            }
        }

        public bool rayTest(Vector3 source, Vector3 dir)
        {
            return false;
        }
    }
}
