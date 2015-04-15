using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using System.Drawing;
using OpenTK.Graphics.OpenGL;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using GGL;

//using GLU = OpenTK.Graphics.Glu;
using System.Diagnostics;
using Jitter.Collision;
using Jitter.LinearMath;
using Jitter.Collision.Shapes;
using Jitter.Dynamics;

namespace GGL
{
    [Serializable]
    public class Terrain2
    {
        [NonSerialized]
        public static Shader terrainShader;

        public static int size = 128;
        public static float heightScale = 1.0f;
        public static float sizeScale = 1.0f;
        public static int VertexPerLine
        { get { return size + 1; } }

        [NonSerialized]
        public float[,] Heights;

        [NonSerialized]
        public TerrainModel model;  //LOD adjusted model
        public TerrainModel OriginalModel;

        public RigidBody body;
        public bool enableJitterDebugDraw = false;

        public Shader shader
        {
            get { return model.meshe.Faces[0].shader; }
            set { model.meshe.Faces[0].shader = value; }
        }

        public Terrain2(float[,] _heights, int _x = 0, int _y = 0)
        {
            Heights = _heights;
            xWorld = _x;
            yWorld = _y;

            buildVertices();

            //model.createShape();

            //shader = terrainShader;
        }


        public float distFrom(Vector3 v)
        {
            Vector3 center = new Vector3(xWorld * size + size / 2, yWorld * size + size / 2, 0);
            float res = (v - center).LengthFast;
            //Debug.WriteLine("LOD {0} {1} => {2} vEyeDist={3}", xWorld, yWorld, LOD,res);
            return res;
        }
        [NonSerialized]
        int _LOD = 0;
        public int LOD
        {
            get { return _LOD; }
            set
            {

                if (_LOD == value)
                    return;

                _LOD = value;

                int oldDivider = RenderDivider;

                if ((int)Math.Pow(2, value) > (size))
                    RenderDivider = size;
                else
                    RenderDivider = (int)Math.Pow(2, _LOD);

                if (RenderDivider != oldDivider)
                    updateRenderedMesh();
            }
        }
        public void updateLOD()
        {
            LOD = (int)(distFrom(World.vEye) / Terrain2.size);
        }


        public int RenderResolution
        {
            get { return (int)size / RenderDivider; }
        }

        [NonSerialized]
        public int RenderDivider = 1;

        //public int RenderDivider
        //{
        //    get { return (int)Math.Pow(2, LOD); }
        //}

        public void updateRenderedMesh()
        {
            Mesh m = model.meshe;

            Mesh res = new Mesh();




            res.Vertices = new Vertex[nbVertices];

            for (int y = 0; y < RenderResolution + 1; y++)
            {
                for (int x = 0; x < RenderResolution + 1; x++)
                {
                    res.Vertices[x + y * (RenderResolution + 1)] = OriginalModel.meshe.Vertices[x * RenderDivider + y * RenderDivider * (size + 1)];
                }
            }

            res.Faces.Add(buildTriangles());

            model = new TerrainModel(res);
            shader = Terrain2.terrainShader;
        }

        public void reshape()
        {
            model.Prepare();
        }

        public float getHeight(Vector2 v)
        {
            return getHeightVector(v).Z;
        }
        public float getHeight(int x, int y)
        {
            try
            {
                return getVertex(x, y).position.Z;
            }
            catch
            {

                return 0f;
            }
        }
        public float getHeight(float x, float y)
        {
            try
            {
                return getHeight(new Vector2(x, y));
            }
            catch
            {

                return 0f;
            }
        }
        //in fact, get position of vertex
        public Vector3 getHeightVector(Vector2 v)
        {
            int x = (int)v.X;
            int y = (int)v.Y;

            if (x < 0 || x > size || y < 0 || y > size)
                return new Vector3(v.X, v.Y, 0);


            Vector3 v1 = getHeightVector(x, y);
            Vector3 v2 = getHeightVector(x + 1, y);
            Vector3 v3 = getHeightVector(x, y + 1);
            Vector3 v4 = getHeightVector(x + 1, y + 1);

            float xCase = v.X % 1;
            float yCase = v.Y % 1;

            return Vector3.BaryCentric(v1, v2, v3, xCase, yCase);

        }
        public Vector3 getAverageHeightVector(Vector2 v, int nbCase = 1)
        {
            int x = (int)v.X;
            int y = (int)v.Y;

            if (x < 0 || x > size || y < 0 || y > size)
                return new Vector3(v.X, v.Y, 0);


            Vector3 v1 = getHeightVector(x - nbCase, y - nbCase);
            Vector3 v2 = getHeightVector(x + nbCase, y - nbCase);
            Vector3 v3 = getHeightVector(x - nbCase, y + nbCase);
            Vector3 v4 = getHeightVector(x + nbCase, y + nbCase);
            return Vector3.BaryCentric(v1, v2, v4, v.X % 1, v.Y % 1);
        }
        public Vector3 getHeightVector(int x, int y)
        {
            return new Vector3(x, y, getHeight(x, y));
        }

        public void setHeight(int x, int y, float height, bool singleVertex = false)
        {
            Vector3 v1 = getHeightVector(x, y);
            setHeightForVertex(getVertexIndex(new Vector2(v1)), height);

            if (singleVertex)
                return;

            Vector3 v2 = getHeightVector(x, y + 1);
            Vector3 v3 = getHeightVector(x + 1, y + 1);
            Vector3 v4 = getHeightVector(x + 1, y);

            setHeightForVertex(getVertexIndex(new Vector2(v2)), height);
            setHeightForVertex(getVertexIndex(new Vector2(v3)), height);
            setHeightForVertex(getVertexIndex(new Vector2(v4)), height);
        }
        public void setHeight(Vector2 v, float height)
        {
            setHeight((int)v.X, (int)v.Y, height);
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

        int nbVertices
        {
            get { return (int)Math.Pow(RenderResolution + 1, 2.0); }
        }
        int nbTriangles
        {
            get { return (int)Math.Pow(RenderResolution, 2.0) * 2; }
        }

        //position in world
        public int xWorld = 0;
        public int yWorld = 0;

        public void setVertexPosition(int vertexIndex, Vector3 pos)
        {
            OriginalModel.meshe.Vertices[vertexIndex].position = pos;
        }
        public void setHeightForVertex(int vertexIndex, float height)
        {
            OriginalModel.meshe.Vertices[vertexIndex].position.Z = height;
        }
        public int getVertexIndex(int x, int y)
        {
            return x + y * (size + 1);
        }
        public int getVertexIndex(Vector2 v)
        {
            return getVertexIndex((int)v.X, (int)v.Y);
        }
        public Vertex getNeareastVertex(float x, float y)
        {
            return getVertex((int)Math.Round(x, 0, MidpointRounding.AwayFromZero), (int)Math.Round(y, 0, MidpointRounding.AwayFromZero));
        }
        public Vertex getVertex(Vector2 v)
        {
            return getVertex((int)v.X, (int)v.Y);
        }
        public Vertex getVertex(int x, int y)
        {

            try
            {
                return OriginalModel.meshe.Vertices[x + y * (size + 1)];
            }
            catch (Exception)
            {
                return new Vertex();
            }
        }
        public void deleteTriangle(int x, int y)
        {
            int triangleIndex = OriginalModel.meshe.Faces[0].findFirstTriangleByVertexIDInIndex0(getVertexIndex(x, y));

            List<Triangle> triangles = OriginalModel.meshe.Faces[0].Triangles.ToList();
            if (triangleIndex < 0)
                return;
            triangles.RemoveAt(triangleIndex);
            OriginalModel.meshe.Faces[0].Triangles = triangles.ToArray();
        }
        public void deleteQuad(int x, int y)
        {
            int triangleIndex = OriginalModel.meshe.Faces[0].findFirstTriangleByVertexIDInIndex0(getVertexIndex(x, y));

            List<Triangle> triangles = OriginalModel.meshe.Faces[0].Triangles.ToList();
            if (triangleIndex < 0)
                return;
            triangles.RemoveAt(triangleIndex);
            triangles.RemoveAt(triangleIndex - 1);
            OriginalModel.meshe.Faces[0].Triangles = triangles.ToArray();
        }

        public Triangle getTriangle(int x, int y)
        {

            try
            {
                return OriginalModel.meshe.Faces[0].Triangles[(x + y * size)];
            }
            catch (Exception)
            {
                return new Triangle();
            }
        }
        public void buildVertices()
        {
            Mesh mesh = new Mesh();
            if (RenderDivider == 0)
                RenderDivider = 1;

            int x = 0,
                y = 0;

            mesh.Vertices = new Vertex[nbVertices];

            float texScaleFactor = (float)size * World.worldSize;

            float texWorldDeltaX = (float)xWorld / World.worldSize;
            float texWorldDeltaY = (float)yWorld / World.worldSize;

            for (y = 0; y < size + 1; y++)
            {
                for (x = 0; x < size + 1; x++)
                {
                    Vertex v = new Vertex();
                    v.position = new Vector3(x*sizeScale, y*sizeScale, Heights[x, y] * heightScale);
                    v.TexCoord = new Vector2(texWorldDeltaX + (float)x / texScaleFactor, texWorldDeltaY + (float)y / texScaleFactor);
                    v.Normal = new Vector3(0, 0, 1);
                    mesh.Vertices[x + y * (size + 1)] = v;
                }
            }

            List<Vector3>[] normals = new List<Vector3>[nbVertices];
            for (x = 0; x < size+1; x++)
            {
                for (y = 0; y < size+1; y++)
                {
                    normals[x + y * (size + 1)] = new List<Vector3>();
                }
            }
            for (x = 0; x < size; x++)
            {
                for (y = 0; y < size; y++)
                {             
                    Vector3 v1 = mesh.Vertices[x + y * (size + 1)].position;
                    Vector3 v2 = mesh.Vertices[x + 1 + y * (size + 1)].position;
                    Vector3 v3 = mesh.Vertices[x + (y + 1) * (size + 1)].position;

                    Vector3 dir = Vector3.Cross(v2 - v1, v3 - v1);
                    dir.Normalize();
                    normals[x + y * (size+1)].Add(dir);
                    normals[x + 1 + y * (size + 1)].Add(dir);
                    normals[x + (y + 1) * (size + 1)].Add(dir);

                    v1 = mesh.Vertices[x + 1 + (y + 1) * (size + 1)].position;
                    v3 = mesh.Vertices[x + 1 + y * (size + 1)].position;
                    v2 = mesh.Vertices[x + (y + 1) * (size + 1)].position;

                    dir = Vector3.Cross(v2 - v1, v3 - v1);
                    dir.Normalize();
                    normals[x + 1 + (y+1) * (size + 1)].Add(dir);
                    normals[x + 1 + y * (size + 1)].Add(dir);
                    normals[x + (y + 1) * (size + 1)].Add(dir);
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
                    mesh.Vertices[i].Normal = Vector3.Normalize( moy / normals[i].Count);
                }
            }

            mesh.Faces.Add(buildTriangles());

            OriginalModel = new TerrainModel(mesh);
            model = OriginalModel;
        }
        FaceGroup buildTriangles()
        {
            FaceGroup faces = new FaceGroup();

            faces.Quads = new Quad[0];
            faces.Triangles = new Triangle[nbTriangles];

            int x = 0;
            int y = 0;

            while (y < RenderResolution)
            {
                while (x < RenderResolution)
                {
                    Triangle t = new Triangle();

                    t.Index0 = x + y * (RenderResolution + 1);
                    t.Index2 = x + (y + 1) * (RenderResolution + 1);
                    t.Index1 = x + 1 + y * (RenderResolution + 1);
                    faces.Triangles[(x * 2 + y * 2 * (RenderResolution))] = t;

                    t = new Triangle();
                    t.Index0 = x + 1 + (y + 1) * (RenderResolution + 1);
                    t.Index2 = x + 1 + y * (RenderResolution + 1);
                    t.Index1 = x + (y + 1) * (RenderResolution + 1);

                    faces.Triangles[(x * 2 + y * 2 * (RenderResolution)) + 1] = t;

                    x++;
                }
                x = 0;
                y++;
            }

            return faces;
        }

        public void render()
        {
            GL.PushAttrib(AttribMask.EnableBit);

            GL.Enable(EnableCap.Multisample);

            //GLU.tes should test tesselation

            GL.MatrixMode(MatrixMode.Modelview);
            GL.PushMatrix();
            GL.Translate(xWorld * Terrain2.size * Terrain2.sizeScale, yWorld * Terrain2.size * Terrain2.sizeScale, 0f);

            if (enableJitterDebugDraw)
            {
                //model.drawCollisionNormals();
                drawJitterDebug();
            }
            else
                model.Render();


            GL.PopMatrix();

            GL.PopAttrib();
        }

        public void drawJitterDebug()
        {
            //GL.PushAttrib(AttribMask.EnableBit);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            GL.Disable(EnableCap.Lighting);
            GL.LineWidth(0.5f);
            GL.Color3(Color.Green);
            body.EnableDebugDraw = true;
            body.DebugDraw(new JitterDebugDrawer());
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            //GL.PopAttrib();        
        }
    }



    [Serializable]
    public class TerrainModel : Model
    {
        public TerrainModel(Mesh m, string _name = "")
        {
            meshe = m;
            Name = _name;
            Prepare();
        }
        public Mesh meshe;

        public override void Prepare()
        {
            meshe.ComputeBounds();
            meshe.Prepare();
        }
        public override void Render()
        {
            meshe.Render();


        }
    }

}
