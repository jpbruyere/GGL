using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using System.Runtime.InteropServices;

namespace GGL
{


    [Serializable]
    public class GenericRoadSegment : Path
    {
        public Vector3[] geometry;
        public int nbGeometryPoints
        {
            get
            {
                if (road.textureIndex == Road.NormalRoad)
                    return nbPathPoints * 2;
                else if (road.textureIndex == Road.Railroad)
                    return nbPathPoints * 4;

                return nbPathPoints * 2;
            }
        }

        protected Quad[] Quads { get; set; }

        [NonSerialized]
        protected int quadsBufferId;

        protected Road road;
        public float tile = 2.0f;

        public float maxSpeed = 300; //km/h

        public float width
        {
            get { return road == null ? 0f : road.width; }
        }
        public static Vector3 vboZadjustment
        { get { return Vector3.UnitZ * 0.1f; } }

        public int texture
        { get { return road == null ? 0 : Road.textures[road.textureIndex]; } }

        public bool IsStation = false;

        public override Vector3 getPathPerpendicularDirection(int indexInPath)
        {
            Vector3 vDir;
            if (geometry != null)
                vDir = geometry[indexInPath * 2] - positions[indexInPath];
            else
                return base.getPathPerpendicularDirection(indexInPath);
            vDir.Normalize();
            return vDir;
        }

        public Vector3 getDirection(int indexInPath)
        {
            if (indexInPath == 0)
                return startVector;
            if (indexInPath == nbPathPoints - 1)
                return endVector;

            Vector3 vDir = geometry[indexInPath * 2] - positions[indexInPath];
            vDir.Normalize();
            return vDir;

        }

        public GenericRoadSegment(Road _road)
            : base()
        {
            this.road = _road;
        }
        public GenericRoadSegment(Road _road, bool isBezier, int _pathSegment) :
            base(World.CurrentWorld, isBezier, _pathSegment)
        {
            inclinaisonMax = MathHelper.Pi / 12f;
            this.road = _road;
            this.tile = road.textureTile;
        }
        public override void preBind()
        {
            base.preBind();
            if (road != null)
                ComputeGeometry();
        }

        public override void bind()
        {
            if (road == null || !isValid)
                return;



            if (!(this is tunnel || this is Bridge))
                world.levelGroundAlongPath(positions, 0.1f, width * 2f);

            CreateVBO();
            //world.reshape();
            road.segments.Add(this);
        }
        public void unbind()
        {
            road.segments.Remove(this);
        }

        public void ComputeGeometry()
        {
            geometry = new Vector3[nbPathPoints * 2];
            Vector3 normal = Vector3.Zero;

            // Calculate geometry
            for (int i = 0; i < nbPathPoints; i++)
            {
                // Note: Because we need to look ahead 1 array index, we make
                // sure that we do not exceed limits of positions[i] array.
                if (i < nbPathPoints - 1)
                {
                    normal = Vector3.Zero;

                    // Normal calculation: nx = by - ay, ny = -(bx - ax)
                    normal = new Vector3(
                        positions[i + 1].Y - positions[i].Y, -(positions[i + 1].X - positions[i].X), 0f
                    );

                    normal.Normalize();
                    normal.Mult(width);
                }

                // Store left extrusion (calculated normal + point position)
                geometry[i * 2] = positions[i] + normal;
                // Store right extrusion (flipped calculated normal + point position)
                geometry[(i * 2) + 1] = positions[i] - normal;

            }
        }

        void CreateVBO()
        {
            if (road.textureIndex == Road.NormalRoad)
                CreateRoadVBO();
            else if (road.textureIndex == Road.Railroad)
                CreateRailRoadVBO();

            Prepare();
        }
        void CreateRoadVBO()
        {
            Vertices = new Vertex[nbGeometryPoints];
            Quads = new Quad[nbPathPoints - 1];

            int j = 0;
            while (j < nbGeometryPoints)
            {
                Vertices[j].TexCoord = new Vector2(computeLength(j / 2) * tile, 0);
                Vertices[j].Normal = Vector3.UnitZ;
                Vertices[j].position = geometry[j] + vboZadjustment;
                j++;
                Vertices[j].Normal = Vector3.UnitZ;
                Vertices[j].TexCoord = new Vector2(computeLength(j / 2) * tile, 1);
                Vertices[j].position = geometry[j] + vboZadjustment;
                j++;
            }

            for (int q = 0; q < nbPathPoints; q++)
            {
                Quad s = new Quad();
                s.Index0 = q * 4;
                s.Index1 = q * 4 + 1;
                s.Index1 = (q + 1) * 4 + 1;
                s.Index1 = (q + 1) * 4;
                Quads[q] = s;
            }

            Prepare();
        }
        public float BalastWidth
        { get { return width * 0.5f; } }
        public float BalastHeight
        { get { return 0.05f; } }

        void CreateRailRoadVBO()
        {
            Vertices = new Vertex[nbGeometryPoints];
            Quads = new Quad[(nbPathPoints-1) * 3];

            int i = 0;
            while (i < nbPathPoints)
            {
                Vertices[i * 4].TexCoord = new Vector2(computeLength(i) * tile, 0);
                Vertices[i * 4].Normal = getPathPerpendicularDirection(i);
                Vertices[i * 4].position = geometry[i * 2] + BalastWidth * getPathPerpendicularDirection(i);

                Vertices[i * 4 + 1].TexCoord = new Vector2(computeLength(i) * tile, 0.2f);
                Vertices[i * 4 + 1].Normal = Vector3.UnitZ;
                Vertices[i * 4 + 1].position = geometry[i * 2] + BalastHeight * Vector3.UnitZ;

                Vertices[i * 4 + 2].Normal = Vector3.UnitZ;
                Vertices[i * 4 + 2].TexCoord = new Vector2(computeLength(i) * tile, 0.8f);
                Vertices[i * 4 + 2].position = geometry[i * 2 + 1] + BalastHeight * Vector3.UnitZ; ;

                Vertices[i * 4 + 3].TexCoord = new Vector2(computeLength(i) * tile, 1f);
                Vertices[i * 4 + 3].Normal = getPathPerpendicularDirection(i);
                Vertices[i * 4 + 3].position = geometry[i * 2 + 1] - BalastWidth * getPathPerpendicularDirection(i);

                i++;

            }

            for (int q = 0; q < nbPathPoints-1; q++)
            {
                Quad s = new Quad();
                s.Index0 = q * 4;
                s.Index1 = q * 4 + 1;
                s.Index2 = (q + 1) * 4 + 1;
                s.Index3 = (q + 1) * 4;
                Quads[q * 3] = s;

                s = new Quad();
                s.Index0 = q * 4 + 1;
                s.Index1 = q * 4 + 2;
                s.Index2 = (q + 1) * 4 + 2;
                s.Index3 = (q + 1) * 4 + 1;
                Quads[q * 3 + 1] = s;

                s = new Quad();
                s.Index0 = q * 4 + 2;
                s.Index1 = q * 4 + 3;
                s.Index2 = (q + 1) * 4 + 3;
                s.Index3 = (q + 1) * 4 + 2;
                Quads[q * 3 + 2] = s;
            }

            Prepare();
        }

        void CreateRailRoadVBOV2()
        {
            Vertices = new Vertex[nbGeometryPoints];
            Quads = new Quad[(nbPathPoints - 1) * 3];

            int i = 0;
            while (i < nbPathPoints)
            {
                Vertices[i * 4].TexCoord = new Vector2(computeLength(i) * tile, 0);
                Vertices[i * 4].Normal = getPathPerpendicularDirection(i);
                Vertices[i * 4].position = geometry[i * 2] + BalastWidth * getPathPerpendicularDirection(i);

                Vertices[i * 4 + 1].TexCoord = new Vector2(computeLength(i) * tile, 0.225f);
                Vertices[i * 4 + 1].Normal = Vector3.UnitZ;
                Vertices[i * 4 + 1].position = geometry[i * 2] + BalastHeight * Vector3.UnitZ;

                Vertices[i * 4 + 2].Normal = Vector3.UnitZ;
                Vertices[i * 4 + 2].TexCoord = new Vector2(computeLength(i) * tile, 0.8f);
                Vertices[i * 4 + 2].position = geometry[i * 2 + 1] + BalastHeight * Vector3.UnitZ; ;

                Vertices[i * 4 + 3].TexCoord = new Vector2(computeLength(i) * tile, 1f);
                Vertices[i * 4 + 3].Normal = getPathPerpendicularDirection(i);
                Vertices[i * 4 + 3].position = geometry[i * 2 + 1] - BalastWidth * getPathPerpendicularDirection(i);

                i++;

            }

            for (int q = 0; q < nbPathPoints - 1; q++)
            {
                Quad s = new Quad();
                s.Index0 = q * 4;
                s.Index1 = q * 4 + 1;
                s.Index2 = (q + 1) * 4 + 1;
                s.Index3 = (q + 1) * 4;
                Quads[q * 3] = s;

                s = new Quad();
                s.Index0 = q * 4 + 1;
                s.Index1 = q * 4 + 2;
                s.Index2 = (q + 1) * 4 + 2;
                s.Index3 = (q + 1) * 4 + 1;
                Quads[q * 3 + 1] = s;

                s = new Quad();
                s.Index0 = q * 4 + 2;
                s.Index1 = q * 4 + 3;
                s.Index2 = (q + 1) * 4 + 3;
                s.Index3 = (q + 1) * 4 + 2;
                Quads[q * 3 + 2] = s;
            }

            Prepare();
        }
        public override void Prepare()
        {
            base.Prepare();

            if (Quads != null)
            {
                GL.GenBuffers(1, out quadsBufferId);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, quadsBufferId);
                GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(Quads.Length * Marshal.SizeOf(typeof(Quad))), Quads, BufferUsageHint.StaticDraw);
            }
        }

        public void RenderGeom()
        {
            GL.PushAttrib(AttribMask.EnableBit);

            GL.Disable(EnableCap.Lighting);
            GL.Disable(EnableCap.Texture2D);

            GL.MatrixMode(MatrixMode.Modelview);

            GL.Disable(EnableCap.DepthTest);

            GL.PointSize(5f);
            GL.Color3(Color.LightBlue);
            GL.Begin(BeginMode.Points);
            for (int i = 0; i < nbPathPoints; i++)
            {
                GL.Vertex3(positions[i]);
            }
            GL.End();


            //GL.Enable(EnableCap.DepthTest);
            //GL.Enable(EnableCap.Lighting);

            GL.PopAttrib();
        }

        public override void Render()
        {
#if DEBUG
            if (wiredFrame)
            {
                RenderGeom();
            }
            //for (int i = 0; i < 4; i++)
            //{
            //    txtHandle[i].render();
            //}
#endif

            if (Quads == null)
                base.Render();
            else
            {
                GL.PushAttrib(AttribMask.EnableBit);
                GL.PushMatrix();

                //GL.Enable(EnableCap.Lighting);
                //GL.Disable(EnableCap.ColorMaterial);

                GL.Color3(Color.White);

                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, texture);
                GL.Enable(EnableCap.Texture2D);

                //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

                GL.PushClientAttrib(ClientAttribMask.ClientVertexArrayBit);
                GL.EnableClientState(EnableCap.VertexArray);

                GL.BindBuffer(BufferTarget.ArrayBuffer, verticesBufferId);
                GL.InterleavedArrays(InterleavedArrayFormat.T2fN3fV3f, Marshal.SizeOf(typeof(Vertex)), IntPtr.Zero);

                GL.LoadName(id);

                GL.BindBuffer(BufferTarget.ElementArrayBuffer, quadsBufferId);
                GL.DrawElements(BeginMode.Quads, Quads.Length * 4, DrawElementsType.UnsignedInt, IntPtr.Zero);



                GL.Disable(EnableCap.Texture2D);

                GL.PopClientAttrib();

                
                GL.PopMatrix();
                GL.LoadName(-1);

                //debug: draw perpendicular angles
                GL.Disable(EnableCap.DepthTest);
                GL.Color3(Color.Magenta);
                GL.Begin(BeginMode.Lines);
                for (int i = 0; i < nbPathPoints; i++)
                {
                    GL.Vertex3(positions[i]);
                    GL.Vertex3(positions[i] + getPathPerpendicularDirection(i));

                }
                GL.End();

               // GL.Enable(EnableCap.DepthTest);
                GL.PopAttrib();
            }
        }
    }
}
