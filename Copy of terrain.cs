using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using System.Drawing;
using OpenTK.Graphics.OpenGL;
using System.Runtime.InteropServices;

namespace OpenTKTests
{
    public class Terrain2
    {
        public static bool wiredFrame = false;

        public int size
        {
            get { return heightMap==null? 0 : heightMap.Size; }
        }
        public void reshape()
        {
            model.meshes[0].Prepare();
        }

        public float getHeight(Vector2 v)
        {
            int x = (int)v.X;
            int y = (int)v.Y;

            Vector3 v1 = getHeightVector(x, y);
            Vector3 v2 = getHeightVector(x + 1, y + 1);

            Vector3 res = Vector3.Lerp(v1, v2, (float)Math.Sqrt(Math.Pow(v.X % 1, 2.0) + Math.Pow(v.Y % 1, 2.0)));

            return res.Z;
        }

        public void setHeight(int x, int y, float height)
        {
            Vector3 v1 = getHeightVector(x, y);
            Vector3 v2 = getHeightVector(x, y + 1);
            Vector3 v3 = getHeightVector(x + 1, y + 1);
            Vector3 v4 = getHeightVector(x + 1, y);

            setHeightForVertex(getVertexIndex(new Vector2(v1)), height);
            setHeightForVertex(getVertexIndex(new Vector2(v2)), height);
            setHeightForVertex(getVertexIndex(new Vector2(v3)), height);
            setHeightForVertex(getVertexIndex(new Vector2(v4)), height);
        }
        public void setHeight(Vector2 v, float height)
        {
            setHeight((int)v.X,(int)v.Y,height);
        }

        public void makePlanar(Vector2 v1, Vector2 v2)
        {
            makePlanar((int)v1.X, (int)v1.Y, (int)v2.X, (int)v2.Y);
        }

        public void makePlanar(int x1, int y1, int x2, int y2)
        {
            float minHeight = float.MaxValue;

            for (int x = x1; x < x2; x++)
            {
                for (int y = y1; y < y2; y++)
                {
                    float h = getHeight(x, y);
                    if (h < minHeight)
                        minHeight = h;
                }
            }
            for (int x = x1; x < x2; x++)
            {
                for (int y = y1; y < y2; y++)
                {
                    setHeight(x, y, minHeight);
                }
            }

            reshape();        
        }

        void setHeightForVertex(int vertexIndex,float height)
        {
            mesh.Vertices[vertexIndex].position.Z = height;
        }



        float getHeight(int x, int y)
        {
            try
            {
                return heightMap.Heights[x, y] * heightScale;
            }
            catch 
            {

                return 0f;
            }
        }
        Vector3 getHeightVector(int x, int y)
        {
            return new Vector3(x, y, getHeight(x, y));
        }

        int getVertexIndex(Vector2 v)
        {
            int x = (int)v.X;
            int y = (int)v.Y;

            return x + y * heightMap.Size;
        }
        Vertex getVertex(Vector2 v)
        {
            int x = (int)v.X;
            int y = (int)v.Y;

            return mesh.Vertices[x + y * heightMap.Size];
        }
        

        HeightMap heightMap;
        public ObjModel model;
        public float heightScale = 10.0f;

        Mesh mesh = null;
        FaceGroup faces = null;

        public void addObject(ModelInstance m)
        {
            try
            {
                m.z = getHeight((int)m.x, (int)m.y);
            }
            catch 
            { }
            objects.Add(m);
        }

        public void addRoad(RoadSegment r)
        {
            //m.z = heightMap.Heights[(int)m.x, (int)m.y];
            roads.Add(r);
            //r.bind(this);
        }
        public List<ModelInstance> objects = new List<ModelInstance>();
        public List<RoadSegment> roads = new List<RoadSegment>();

        int nbVertices
        {
            get { return (int)Math.Pow(heightMap.Size, 2.0); }
        }
        int nbTrianglesFan
        {
            get { return (int)Math.Pow(heightMap.Size - 1, 2.0)*2; }
        }

        public Terrain2(int size)
        {
            heightMap = new HeightMap(size);
            heightMap.AddPerlinNoise(6.0f);
            heightMap.Perturb(32.0f, 32.0f);
            for (int i = 0; i < 10; i++)
                heightMap.Erode(16.0f);
            heightMap.Smoothen();

            Init();
        }

        void Init()
        {
            int x = 0,
                y = 0;

            Vertices = new Vertex[nbVertices];
            
            Triangles = new TriangleFan[nbTrianglesFan];

            for (y = 0; y < heightMap.Size; y++)
            {
                for (x = 0; x < heightMap.Size; x++)
                {
                    Vertex v = new Vertex();
                    v.position = new Vector3(x, y, heightMap.Heights[x, y] * heightScale);
                    v.TexCoord = new Vector2((float)x / (float)(heightMap.Size - 1), (float)y / (float)(heightMap.Size - 1));
                    //v.TexCoord = new Vector2(x, y);
                    v.Normal = new Vector3(0, 0, 0);
                    mesh.Vertices[x + y * heightMap.Size] = v;
                }
            }

            List<Vector3>[] normals = new List<Vector3>[nbVertices];

            for (x = 0; x < heightMap.Size - 1; x++)
            {
                for (y = 0; y < heightMap.Size - 1; y++)
                {
                    normals[x + y * (heightMap.Size - 1)] = new List<Vector3>();

                    Vector3 v1 = mesh.Vertices[x + y * (heightMap.Size - 1)].position;
                    Vector3 v2 = mesh.Vertices[x + 1 + y * (heightMap.Size - 1)].position;
                    Vector3 v3 = mesh.Vertices[x + (y + 1) * (heightMap.Size - 1)].position;

                    Vector3 dir = Vector3.Cross(v2 - v1, v3 - v1);
                    normals[x + y * (heightMap.Size - 1)].Add(Vector3.Normalize(dir));
                }
            }

            for (int i = 0; i < normals.Length; i++)
            {
                Vector3 moy = new Vector3(0, 0, 0);

                if (normals[i] != null)
                {
                    foreach (Vector3 v in normals[i])
                    {
                        moy += v;
                    }
                    mesh.Vertices[i].Normal = moy / normals[i].Count;
                }
            }

            buildTriangles();

            mesh.subMeshes.Add(faces);
            model = new ObjModel(mesh);
        }
        void buildTriangles()
        {
            int x = 0;
            int y = 0;

            while (y < heightMap.Size - 1)
            {
                while (x < heightMap.Size - 1)
                {
                    Triangle t = new Triangle();

                    t.Index0 = x + y * heightMap.Size;
                    t.Index1 = x + (y + 1) * heightMap.Size;
                    t.Index2 = x + 1 + y * heightMap.Size;
                    faces.Triangles[(x + y * (heightMap.Size - 1))*2] = t;

                    x++;
                    if (x == heightMap.Size - 1)
                        break;

                    t = new Triangle();
                    t.Index2 = x + (y + 1) * heightMap.Size;
                    t.Index1 = x + 1 + (y + 1) * heightMap.Size;
                    t.Index0 = x + 1 + y * heightMap.Size;                    

                    faces.Triangles[(x + y * (heightMap.Size - 1)) * 2] = t;

                    x++;
                }
                x = 0;
                y++;
            }
        }

        public void render()
        {
            renderTerrain();

            foreach (RoadSegment r in roads)
                r.Render();
    
            foreach (ModelInstance o in objects)
                o.Render();

        }



        public int texture = 0;
        public int shader = 0;
        public TextureUnit texUnit = TextureUnit.Texture0;

        public TriangleFan[] Triangles { get; set; }
        public Vertex[] Vertices { get; set; }

        int trianglesBufferId;
        int verticesBufferId;

        public void prepare()
        {
            GL.GenBuffers(1, out verticesBufferId);
            GL.BindBuffer(BufferTarget.ArrayBuffer, verticesBufferId);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(Vertices.Length * Marshal.SizeOf(typeof(Vertex))), Vertices, BufferUsageHint.DynamicDraw);

            if (Triangles != null)
            {
                GL.GenBuffers(1, out trianglesBufferId);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, trianglesBufferId);
                GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(Triangles.Length * Marshal.SizeOf(typeof(TriangleFan))), Triangles, BufferUsageHint.StaticDraw);
            }
        }
        public void renderTerrain()
        {
            GL.PushAttrib(AttribMask.EnableBit);
            GL.UseProgram(shader);
            if (shader == 0)
                GL.Enable(EnableCap.Texture2D); //Only use this if not using shaders.
            GL.ActiveTexture(texUnit);
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.Color3(Color.White);

            if (trianglesBufferId > 0)
            {
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, trianglesBufferId);
                GL.DrawElements(BeginMode.TriangleFan, Triangles.Length, DrawElementsType.UnsignedInt, IntPtr.Zero);
            }

            GL.UseProgram(0);
            GL.PopAttrib();
        }
    }


}
