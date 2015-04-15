using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Diagnostics;
using OTKGL;
using Examples.Shapes;


namespace OTKGL
{
    [Serializable]
    public class RoadSegment : SelectableObject
    {
#if DEBUG
        public static bool wiredFrame = false;
        //Text[] txtHandle = new Text[4]; 
#endif
        [NonSerialized]
        public static DrawableShape sphere = new SlicedSphere(0.4f, Vector3d.Zero, SlicedSphere.eSubdivisions.Two, new SlicedSphere.eDir[] { SlicedSphere.eDir.All }, true);

        public const int HANDLE_NEW_0 = 10;
        public const int HANDLE_NEW_1 = 11;
        public const int HANDLE_NEW_2 = 12;
        public const int HANDLE_NEW_3 = 13;

        public float tile = 2.0f;

        public bool IsStation = false;
        public bool isValid = false;
        public float maxSpeed = 300; //km/h

        public float width
        {
            get { return road == null ? 0f : road.width; }
        }

        public virtual Vector3 startVector
        {
            get
            {
                Vector3 v = handles[0] - handles[1];
                v.Normalize();
                return v;
            }
        }
        public virtual Vector3 endVector
        {
            get
            {
                Vector3 v = handles[1] - handles[0];
                v.Normalize();
                return v;
            }
        }

        public int nbPathPoints
        { get { return pathSegments + 1; } }
        public int nbGeometryPoints
        { get { return nbPathPoints * 2; } }

        public Vector3 getDirection(int indexInPath)
        {
            if (indexInPath == 0)
                return startVector;
            if (indexInPath == nbPathPoints - 1)
                return endVector;

            Vector3 vDir = geometry[indexInPath * 2] - path[indexInPath];
            vDir.Normalize();
            return vDir;

        }
        public Vector3 getPositiveDirection(int indexInPath)
        {
            if (indexInPath == 0)
                return startVector;
            if (indexInPath >= nbPathPoints - 1)
                return -endVector;

            Vector3 vDir = path[indexInPath + 1] - path[indexInPath];
            vDir.Normalize();
            return vDir;
        }
        public Vector3 getNegativeDirection(int indexInPath)
        {
            if (indexInPath == 0)
                return -startVector;
            if (indexInPath >= nbPathPoints - 1)
                return endVector;

            Vector3 vDir = path[indexInPath - 1] - path[indexInPath];
            vDir.Normalize();
            return vDir;
        }
        public Vector3 getPathDirection(int indexInPath)
        {
            Vector3 vDir = geometry[indexInPath * 2] - path[indexInPath];
            vDir.Normalize();
            return vDir;

        }

        #region Path properties
        /// <summary>How many segments road has.</summary>
        protected int pathSegments = 1;
        /// <summary>The handles of the curve.</summary>        
        public Vector3[] handles;
        public bool handleStartIsLinked = false;
        public int handleStartLinkedIndex = 0;
        public RoadSegment handleEndLinkReference;
        public bool handleEndIsLinked = false;
        public int handleEndLinkedIndex = 0;
        public RoadSegment handleStartLinkReference;
        /// <summary>Generated path that is based on handles.</summary>
        public Vector3[] path;
        /// <summary>The actual vertex geometry of the path.</summary>
        public Vector3[] geometry;
        #endregion

        protected QuadStrip[] Quads { get; set; }
        protected Vertex[] Vertices { get; set; }

        [NonSerialized]
        protected int verticesBufferId;
        [NonSerialized]
        protected int quadsBufferId;
        [NonSerialized]

        protected Road road;

        protected World world
        {
            get { return road == null ? null : road.world; }
        }
        public static Vector3 vboZadjustment
        { get { return Vector3.UnitZ * 0.1f; } }

        public int texture
        { get { return road == null ? 0 : Road.textures[road.textureIndex]; } }


        public virtual void Prepare()
        {
            id = ModelInstance.NextAvailableID;
            SelectableObject.objectsDic.Add(id, this);

            if (Vertices != null)
            {
                GL.GenBuffers(1, out verticesBufferId);
                GL.BindBuffer(BufferTarget.ArrayBuffer, verticesBufferId);
                GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(Vertices.Length * Marshal.SizeOf(typeof(Vertex))), Vertices, BufferUsageHint.StaticDraw);
            }

            if (Quads != null)
            {
                GL.GenBuffers(1, out quadsBufferId);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, quadsBufferId);
                GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(Quads.Length * Marshal.SizeOf(typeof(QuadStrip))), Quads, BufferUsageHint.StaticDraw);
            }
#if DEBUG
            //id text helper
            //for (int i = 0; i < 4; i++)
            //{
            //    txtHandle[i] = new Text(handles[i],string.Format("h{0}",i));
            //}
#endif
        }
        public virtual void Render()
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
            GL.PushAttrib(AttribMask.EnableBit);
            GL.PushMatrix();

            GL.Enable(EnableCap.Lighting);
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

            if (Quads.Length > 0)
            {
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, quadsBufferId);
                GL.DrawElements(BeginMode.QuadStrip, Quads.Length * 2, DrawElementsType.UnsignedInt, IntPtr.Zero);
            }


            GL.Disable(EnableCap.Texture2D);

            GL.PopClientAttrib();

            GL.PopAttrib();
            GL.PopMatrix();
            GL.LoadName(-1);
        }
        public virtual void RenderPath()
        {

            GL.PushAttrib(AttribMask.EnableBit);

            GL.Disable(EnableCap.Lighting);
            GL.Disable(EnableCap.Texture2D);

            #region Draw point handles (white large sphere)
            GL.MatrixMode(MatrixMode.Modelview);

            GL.Color3(Color.Yellow);
            if (road.currentHandleInNewSegment == 0)
            {
                if (handleStartIsLinked)
                    GL.Color3(Color.Green);
                else
                    GL.Color3(Color.CornflowerBlue);
            }
            else if (handleStartIsLinked)
                GL.Color3(Color.GreenYellow);

            GL.PushMatrix();
            GL.LoadName(RoadSegment.HANDLE_NEW_0);
            GL.Translate(handles[0]);
            sphere.Draw();
            GL.PopMatrix();

            GL.Color3(Color.Yellow);
            if (road.currentHandleInNewSegment == 0)
            {
                if (handleEndIsLinked)
                    GL.Color3(Color.Green);
                else
                    GL.Color3(Color.CornflowerBlue);
            }
            else if (handleEndIsLinked)
                GL.Color3(Color.GreenYellow);


            GL.PushMatrix();
            GL.LoadName(RoadSegment.HANDLE_NEW_1);
            GL.Translate(handles[1]);
            sphere.Draw();

            GL.PopMatrix();

            #endregion

            GL.Disable(EnableCap.DepthTest);

            #region Process path curve
            // Draw line of the curve path
            GL.LineWidth(1f);

            if (isValid)
                GL.Color3(Color.Green);
            else
                GL.Color3(Color.Red);

            GL.Begin(BeginMode.LineStrip);
            for (int i = 0; i < nbPathPoints; i++) GL.Vertex3(path[i]);
            GL.End();

            // Draw segment points (to see how the cuve path is divided)
            GL.PointSize(2f);
            GL.Color3(Color.White);
            GL.Begin(BeginMode.Points);
            for (int i = 0; i < nbPathPoints; i++)
            {
                if (!isValid && i == invalidIndex)
                    GL.Color3(Color.Red);

                GL.Vertex3(path[i]);
            }
            GL.End();
            #endregion

            if (Vertices != null)
            {
                GL.PointSize(5f);
                GL.Color3(Color.LightBlue);
                GL.Begin(BeginMode.Points);
                for (int i = 0; i < nbPathPoints; i++)
                {
                    GL.Vertex3(Vertices[i * 2].position);
                    GL.Vertex3(Vertices[i * 2 + 1].position);

                }
                GL.End();
            }
            GL.Enable(EnableCap.DepthTest);
            //GL.Disable(EnableCap.ColorMaterial);
            GL.PopAttrib();
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
                GL.Vertex3(path[i]);
            }
            GL.End();


            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Lighting);

            GL.PopAttrib();
        }

        public void unbind()
        {
            road.segments.Remove(this);
        }
        public virtual void bind()
        {
            if (road == null)
                return;

            if (!(this is tunnel || this is Bridge))
                world.levelGroundAlongPath(path, 0.1f, width * 2f);

            CreateRoadVBO();
            //world.reshape();
            road.segments.Add(this);
        }

        //public float ComputeXAngle()
        //{ 
        //    Vector2 vDir = new Vector2(getDirection(0))
        //}

        void ComputeParallelPath()
        {
            _cachedLength = 0f;

            path = new Vector3[nbPathPoints];

            // Calculate positions points

            int startIndex = Math.Min(handleEndLinkedIndex, handleStartLinkedIndex);
            for (int i = 0; i < nbPathPoints; i++)
            {
                //check handle 1 or 2 direction for parallell positionnin
                if (road.parrallelRight)
                    path[i] = handleEndLinkReference.path[startIndex + i] + handleEndLinkReference.getPathDirection(startIndex + i) * road.width * 2;
                else
                    path[i] = handleEndLinkReference.path[startIndex + i] - handleEndLinkReference.getPathDirection(startIndex + i) * road.width * 2;

            }
        }

        public virtual void preBind(Road road)
        {
            this.road = road;


            if (handleStartIsLinked && handleEndIsLinked)
            {
                if (handleEndLinkReference == handleStartLinkReference)
                {
                    //station segment has only 2 points
                    if (handleEndLinkReference.nbPathPoints > 2)
                    {
                        int newPathSegmentsCount = Math.Abs(handleStartLinkedIndex - handleEndLinkedIndex);
                        if (newPathSegmentsCount > 0)
                            pathSegments = newPathSegmentsCount;

                        ComputeParallelPath();
                    }
                }
                else
                {
                    ComputePath();
                    computePathAngles();
                    validatePath();
                }
            }
            else
            {
                InitHeightForHandles();

                //create temp path with default path segments for length calculation
                ComputePath();

                if (this is BezierRoadSegment)
                {
                    int newPathSegmentsCount = (int)computeLength() * 2;
                    if (newPathSegmentsCount > 0)
                        pathSegments = newPathSegmentsCount;

                    ComputePath();
                }

                if (Road.smoothRoadLeveling)
                    SmootHeights();

                computePathAngles();
                validatePath();
            }

            Debug.WriteLine("path segments: " + pathSegments);

            ComputeGeometry();
        }

        public virtual void InitHeightForHandles()
        {
            handles[0].Z = world.getHeight(new Vector2(handles[0]));
            handles[1].Z = world.getHeight(new Vector2(handles[1]));
        }

        public void SmootHeights()
        {
            float denivelation = handles[handles.Length - 1].Z - handles[0].Z;

            //compute horizontal length, without z component
            float totalLength = 0;
            for (int i = 0; i < nbPathPoints - 1; i++)
            {
                totalLength += (new Vector2(path[i + 1]) - new Vector2(path[i])).Length;
            }


            float actualLength = 0f;

            for (int i = 0; i < nbPathPoints - 1; i++)
            {
                actualLength += (new Vector2(path[i + 1]) - new Vector2(path[i])).Length;


                float newHeight = handles[0].Z + denivelation / totalLength * actualLength;
                path[i + 1].Z = newHeight;
            }
            //path[nbPathPoints - 1].Z = handles[handles.Length - 1].Z;
            this._cachedLength = 0;
        }

        public RoadSegment(Road _road)
        {
            this.road = _road;
            this.pathSegments = 1;
        }
        public RoadSegment()
        { }
        public RoadSegment(Vector3[] _handles, int pathSegments = 1)
        {
            this.pathSegments = pathSegments;

            this.handles = _handles;

            if (handles != null)
            {
                this.ComputePath();
                this.ComputeGeometry();
            }
        }



        protected virtual void ComputePath()
        {
            _cachedLength = 0f;

            path = new Vector3[nbPathPoints];
            path[0] = handles[0];
            path[1] = handles[1];
        }

        static float PIOver12 = MathHelper.Pi / 12f;

        protected float[] segVerticalAngle;
        protected int invalidIndex = 0;

        public float inclinaisonMax = PIOver12;

        public virtual void validatePath()
        {
            isValid = true;
            int i;
            for (i = 0; i < pathSegments; i++)
            {
                if (segVerticalAngle[i] > inclinaisonMax)
                {
                    isValid = false;
                    invalidIndex = i;
                    break;
                }
            }
        }

        public void computePathAngles()
        {
            Vector3[] segDirs = new Vector3[pathSegments];
            for (int i = 0; i < pathSegments; i++)
            {
                segDirs[i] = path[i + 1] - path[i];
                segDirs[i].Normalize();
            }

            //need multipathsegment computation
            //float[] segHorizontalAngleZ = new float[pathSegments];
            segVerticalAngle = new float[pathSegments];

            for (int i = 0; i < pathSegments; i++)
            {
                segVerticalAngle[i] = Vector3.CalculateAngle(segDirs[i], new Vector3(segDirs[i].X, segDirs[i].Y, 0f));
                if (float.IsNaN(segVerticalAngle[i]))
                    segVerticalAngle[i] = 0;
            }

            //for (int i = 0; i < pathSegments; i++)
            //{
            //    Debug.WriteLine("{0} => {1}", i, MathHelper.RadiansToDegrees(segVerticalAngle[i]) );                
            //}

        }

        public void ComputeGeometry()
        {
            geometry = new Vector3[nbGeometryPoints];
            Vector3 normal = Vector3.Zero;

            // Calculate geometry
            for (int i = 0; i < nbPathPoints; i++)
            {
                // Note: Because we need to look ahead 1 array index, we make
                // sure that we do not exceed limits of path[i] array.
                if (i < nbPathPoints - 1)
                {
                    normal = Vector3.Zero;

                    // Normal calculation: nx = by - ay, ny = -(bx - ax)
                    normal = new Vector3(
                        path[i + 1].Y - path[i].Y, -(path[i + 1].X - path[i].X), 0f
                    );

                    normal.Normalize();
                    normal.Mult(width);
                }

                // Store left extrusion (calculated normal + point position)
                geometry[i * 2] = path[i] + normal;
                // Store right extrusion (flipped calculated normal + point position)
                geometry[(i * 2) + 1] = path[i] - normal;

            }
        }
        

        void CreateRoadVBO()
        {
            Vertices = new Vertex[nbGeometryPoints];
            Quads = new QuadStrip[nbPathPoints];

            int j = 0;
            while (j < nbGeometryPoints)
            {
                Vertices[j].TexCoord = new Vector2(computeLength(j/2) * tile, 0);
                Vertices[j].Normal = Vector3.UnitZ;
                Vertices[j].position = geometry[j] + vboZadjustment;
                j++;
                Vertices[j].Normal = Vector3.UnitZ;
                Vertices[j].TexCoord = new Vector2(computeLength(j/2) * tile, 1);
                Vertices[j].position = geometry[j] + vboZadjustment;
                j++;
            }

            for (int q = 0; q < nbPathPoints; q++)
            {
                QuadStrip s = new QuadStrip();
                s.Index0 = q * 2 + 1;
                s.Index1 = q * 2;
                Quads[q] = s;
            }

            Prepare();
        }


        protected float _cachedLength = 0f;
        //0=> total length
        public float computeLength(int index = -1)
        {

            
            if (_cachedLength == 0f && index > -2)
                _cachedLength = computeLength(-2);    

            if (index == -1)
                return _cachedLength;

            if (index == -2)
                index = path.Length - 1;    //compute //cachedLength

            float length = 0;
            for (int i = 0; i < index; i++)
            {
                length += (path[i + 1] - path[i]).Length;
            }
            return length;
        }

        public Vector3 getVPosInSegment(float t)
        {
            float lengthTarget = computeLength() * t;
            float l = 0f;

            for (int i = 0; i < nbPathPoints - 1; i++)
            {
                Vector3 vDir = path[i + 1] - path[i];
                float tmp = vDir.Length;
                if (tmp + l > lengthTarget)
                {
                    //interpolation linéaire
                    float remainingLength = lengthTarget - l;
                    //get ratio in current segment
                    float ratio = remainingLength / tmp;
                    return Vector3.Lerp(path[i], path[i + 1], ratio);
                }
                else
                    l += tmp;
            }

            return Vector3.Zero;
        }


    }

}
