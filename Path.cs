using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Diagnostics;
using GGL;
using Examples.Shapes;


namespace GGL
{
    [Serializable]
    public class Path : SelectableObject
    {
        protected World world;
        public static Path newPath;
        public static bool newPathInit = false;
        public static bool parrallelRight = false;
        

        const float PIOver12 = MathHelper.Pi / 12f;

        public static bool linearLeveling = false;
        public static float linkSensibility = 0.2f;

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

        public int pathSegments = 1;
        public float resolution = 2f;
        public float inclinaisonMax = MathHelper.PiOver4;

        /// <summary>The handles of the curve.</summary>        
        public Vector3[] handles;

        public bool handleStartIsLinked = false;
        public int handleStartLinkedIndex = 0;
        public Path handleEndLinkReference;
        public bool handleEndIsLinked = false;
        public int handleEndLinkedIndex = 0;
        public Path handleStartLinkReference;

        /// <summary>Generated path that is based on handles.</summary>
        public Vector3[] positions;
        public float[] segHorizontalAngles;
        public float[] segVerticalAngles;
        protected Vertex[] Vertices { get; set; }
        [NonSerialized]
        protected int verticesBufferId;

        public int selectedHandle = -1;

        public bool isValid = false;
        protected int invalidIndex = 0;

        public virtual Vector3 startVector
        {
            get
            {
                Vector3 v = positions[0] - positions[1];
                v.Normalize();
                return v;
            }
        }
        public virtual Vector3 endVector
        {
            get
            {
                Vector3 v = positions[nbPathPoints - 1] - positions[nbPathPoints - 2];
                v.Normalize();
                return v;
            }
        }

        public bool isBezierPath
        { get { return handles.Length > 2 ? true : false; } }
        public bool isStraightPath
        { get { return handles.Length == 2 ? true : false; } }


        public int nbPathPoints
        { get { return pathSegments + 1; } }
        public Vector3 getPositiveDirection(int indexInPath)
        {
            if (indexInPath == 0)
                return startVector;
            if (indexInPath >= nbPathPoints - 1)
                return -endVector;

            Vector3 vDir = positions[indexInPath + 1] - positions[indexInPath];
            vDir.Normalize();
            return vDir;
        }
        public Vector3 getNegativeDirection(int indexInPath)
        {
            if (indexInPath == 0)
                return -startVector;
            if (indexInPath >= nbPathPoints - 1)
                return endVector;

            Vector3 vDir = positions[indexInPath - 1] - positions[indexInPath];
            vDir.Normalize();
            return vDir;
        }
        public virtual Vector3 getPathPerpendicularDirection(int indexInPath)
        {
            Vector3 vDir;

            if (indexInPath == 0)
                vDir = new Vector3(new Vector2(startVector).PerpendicularLeft);
            else if (indexInPath == positions.Length - 1)
                vDir = new Vector3(new Vector2(endVector).PerpendicularRight);
            else
                vDir = Vector3.Normalize(Vector3.Lerp(positions[indexInPath - 1], positions[indexInPath + 1], 0.5f) - positions[indexInPath]);

            return vDir;
        }
        public Path(World _world, bool _isBezier = false, int _SegmentsCount = 0)
        {
            this.world = _world;

            if (_SegmentsCount < 1)
            {
                if (_isBezier)
                    pathSegments = 10;
                else
                    pathSegments = 1;
            }
            else
                pathSegments = _SegmentsCount;

            if (_isBezier)
                handles = new Vector3[4];
            else
                handles = new Vector3[2];

            handles[0] = new Vector3(Mouse3d.Position);

            for (int h = 1; h < handles.Length; h++)
            {
                handles[h] = handles[0];
            }


            selectedHandle = handles.Length - 1;

            Mouse3d.setPosition(handles[0]);

            preBind();

            if (selectedObject != null)
            {
                selectedHandle = 0;
                checkSelectedObjectLinkage();
                selectedHandle = handles.Length - 1;
                preBind();
            }
        }

        public Path()
        { }

        public Path(Vector3[] _handles, int pathSegments = 1)
        {
            this.pathSegments = pathSegments;

            this.handles = _handles;

            if (handles != null)
                this.ComputePath();
        }


        public virtual void Prepare()
        {
            if (id == 0)
                SelectableObject.registerObject(this);

            if (Vertices != null)
            {
                GL.GenBuffers(1, out verticesBufferId);
                GL.BindBuffer(BufferTarget.ArrayBuffer, verticesBufferId);
                GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(Vertices.Length * Marshal.SizeOf(typeof(Vertex))), Vertices, BufferUsageHint.StaticDraw);
            }
        }

        public virtual void Render()
        {

            GL.PushAttrib(AttribMask.EnableBit);

            GL.Disable(EnableCap.Lighting);
            GL.Disable(EnableCap.Texture2D);
            GL.UseProgram(0);

            #region Draw point handles (white large sphere)
            GL.MatrixMode(MatrixMode.Modelview);

            for (int i = 0; i < handles.Length; i++)
            {
                GL.Color3(Color.LightSlateGray);
                if ((handleStartIsLinked && i == 0) || (handleEndIsLinked && i == handles.Length - 1))
                {
                    if (selectedHandle == i)
                        GL.Color3(Color.GreenYellow);
                    else
                        GL.Color3(Color.LightGreen);
                }
                else if (selectedHandle == i)
                    GL.Color3(Color.Yellow);

                float scale = (handles[i] - World.vEye).LengthFast * 0.03f;

                GL.PushMatrix();
                GL.LoadName(Path.HANDLE_NEW_0 + i);
                GL.Translate(handles[i]);
                GL.Scale(new Vector3(scale, scale, scale));

                sphere.Draw();

                GL.PopMatrix();
            }

            #endregion

            GL.Disable(EnableCap.DepthTest);
            GL.LineWidth(1f);
            if (isBezierPath)
            {
                #region Draw lines of handles                
                GL.Color3(Color.WhiteSmoke);
                GL.Begin(BeginMode.Lines);
                for (int i = 0; i < 4; i++) GL.Vertex3(handles[i]);
                GL.End();
                #endregion
            }

            #region Process path curve
            // Draw line of the curve path
            

            if (isValid)
                GL.Color3(Color.Green);
            else
                GL.Color3(Color.Red);

            GL.Begin(BeginMode.LineStrip);
            for (int i = 0; i < nbPathPoints; i++) GL.Vertex3(positions[i]);
            GL.End();

            // Draw segment points (to see how the cuve path is divided)
            GL.PointSize(2f);
            GL.Color3(Color.White);
            GL.Begin(BeginMode.Points);
            for (int i = 0; i < nbPathPoints; i++)
            {
                if (!isValid && i == invalidIndex)
                    GL.Color3(Color.Red);

                GL.Vertex3(positions[i]);
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

            
            //debug: draw perpendicular angles
            GL.Color3(Color.Magenta);
            GL.Begin(BeginMode.Lines);
            for (int i = 0; i < nbPathPoints; i++)
            {
                GL.Vertex3(positions[i]);
                GL.Vertex3(positions[i] + getPathPerpendicularDirection(i));

            }
            GL.End();

            //GL.Enable(EnableCap.DepthTest);
            //GL.Disable(EnableCap.ColorMaterial);
            GL.PopAttrib();
        }

        public virtual void bind()
        {
        }

        public void checkSelectedObjectLinkage()
        {
            bool linked = false;
            if (SelectableObject.selectedObject is Path)
            {

                Path currentPath = SelectableObject.selectedObject as Path;

                if (handleStartIsLinked && selectedHandle == 0
                    && Mouse3d.Delta.LengthFast < linkSensibility * 5f)
                    return;
                if (handleEndIsLinked && selectedHandle == handles.Length - 1
                    && Mouse3d.Delta.LengthFast < linkSensibility * 5f)
                    return;


                //link new segment with currentsegment
                for (int i = 0; i < currentPath.nbPathPoints; i++)
                {
                    Vector3 diff = currentPath.positions[i] - Mouse3d.Position;
                    if (diff.Length < linkSensibility)
                    {
                        linked = true;

                        handles[selectedHandle] = currentPath.positions[i];

                        if (selectedHandle == 0)
                        {
                            handleStartLinkedIndex = i;
                            handleStartIsLinked = true;
                            handleStartLinkReference = currentPath;
                        }
                        else
                        {
                            handleEndLinkedIndex = i;
                            handleEndIsLinked = true;
                            handleEndLinkReference = currentPath;
                        }

                        if (isBezierPath)
                        {
                            if (handleStartIsLinked && handleEndIsLinked && (handleStartLinkReference != handleEndLinkReference)
                                && (i != 0 && i != currentPath.nbPathPoints - 1))
                            {
                                //position handles 1-2 for junction
                                Vector3 v1 = handleStartLinkReference.positions[handleStartLinkedIndex];
                                Vector3 v2 = handleEndLinkReference.positions[handleEndLinkedIndex];
                                Vector3 junctionDir = v2 - v1;
                                junctionDir.Normalize();

                                Vector3 vPathDirPos = handleStartLinkReference.getPositiveDirection(handleStartLinkedIndex);
                                Vector3 vPathDirNeg = handleStartLinkReference.getNegativeDirection(handleStartLinkedIndex);

                                float angle = Vector3.CalculateAngle(junctionDir, vPathDirPos);
                                if (angle > MathHelper.PiOver2)
                                    handles[1] = handles[0] + vPathDirNeg * computeLength() / 3f;
                                else
                                    handles[1] = handles[0] + vPathDirPos * computeLength() / 3f;

                                vPathDirPos = handleEndLinkReference.getPositiveDirection(handleEndLinkedIndex);
                                vPathDirNeg = handleEndLinkReference.getNegativeDirection(handleEndLinkedIndex);

                                angle = Vector3.CalculateAngle(junctionDir, vPathDirPos);
                                if (angle > MathHelper.PiOver2)
                                    handles[2] = handles[3] - vPathDirNeg * computeLength() / 3f;
                                else
                                    handles[2] = handles[3] - vPathDirPos * computeLength() / 3f;

                            }
                            else
                            {
                                if (i == 0)
                                {
                                    if (selectedHandle == 0)
                                        handles[1] = handles[0] + currentPath.startVector * computeLength() / 3;
                                    else
                                        handles[2] = handles[3] + currentPath.startVector * computeLength() / 3;
                                }
                                else if (i == currentPath.nbPathPoints - 1)
                                {
                                    if (selectedHandle == 0)
                                        handles[1] = handles[0] + currentPath.endVector * computeLength() / 3;
                                    else
                                        handles[2] = handles[3] + currentPath.endVector * computeLength() / 3;
                                }
                                else
                                {
                                    if (isBezierPath)
                                        computeHandle1_2WhenSingleLinked(i, currentPath, selectedHandle);
                                }
                            }
                        }

                        break;
                    }
                }
            }
            if (!linked)
            {
                moveHandleToMousePosition();
            }
            Debug.WriteLine("linked = " + linked);
        }

        public void moveHandleToMousePosition()
        {
            Vector3 hDiff;
            switch (selectedHandle)
            {
                case 0:
                    hDiff = handles[0] - handles[1];
                    handles[0] = Mouse3d.Position;
                    handleStartLinkedIndex = 0;
                    handleStartIsLinked = false;
                    if (isBezierPath)
                    {
                        handles[1] = handles[0] - hDiff;
                        if (handleEndIsLinked && handleEndLinkReference.isBezierPath)
                            computeHandle1_2WhenSingleLinked(handleEndLinkedIndex, handleEndLinkReference, 3);
                    }
                    break;
                case 1:
                    if (isBezierPath)
                    {
                        if (handleStartIsLinked)
                            handles[1] = Mouse3d.Position;  //should allow only on linkref direction with minimal length for curve
                        else
                            handles[1] = Mouse3d.Position;
                    }
                    else
                    {
                        handles[1] = Mouse3d.Position;
                        handleEndLinkedIndex = 0;
                        handleEndIsLinked = false;
                    }
                    break;
                case 2:
                    if (!handleEndIsLinked)
                        handles[2] = Mouse3d.Position;
                    break;
                case 3:
                    hDiff = handles[3] - handles[2];
                    handles[3] = Mouse3d.Position;
                    handles[2] = handles[3] - hDiff;
                    handleEndLinkedIndex = 0;
                    handleEndIsLinked = false;
                    if (handleStartIsLinked && handleStartLinkReference.isBezierPath)
                        computeHandle1_2WhenSingleLinked(handleStartLinkedIndex, handleStartLinkReference, 0);

                    break;
            }
        }
        public void computeHandle1_2WhenSingleLinked(int targetPathIndexInCurrentSegment, Path targetPath, int targetHandleInNewSegment)
        {
            if (targetPathIndexInCurrentSegment == 0 || targetPathIndexInCurrentSegment == targetPath.nbPathPoints - 1)
                return;

            Vector3 pathDir = targetPath.getPositiveDirection(targetPathIndexInCurrentSegment);
            pathDir.Z = 0f;

            Vector3 h3Dir = handles[3] - handles[0];
            h3Dir.Normalize();
            h3Dir.Z = 0f;

            float a = Vector3.CalculateAngle(pathDir, h3Dir);
            Debug.WriteLine(a);

            if (targetHandleInNewSegment == 0)
            {
                if (a < MathHelper.PiOver2)
                    handles[1] = handles[0] + targetPath.getPathPerpendicularDirection(targetPathIndexInCurrentSegment) * computeLength() / 3f;
                else
                    handles[1] = handles[0] - targetPath.getPathPerpendicularDirection(targetPathIndexInCurrentSegment) * computeLength() / 3f;
            }
            else
            {
                if (a < MathHelper.PiOver2)
                    handles[2] = handles[3] + targetPath.getPathPerpendicularDirection(targetPathIndexInCurrentSegment) * computeLength() / 3f;
                else
                    handles[2] = handles[3] + targetPath.getPathPerpendicularDirection(targetPathIndexInCurrentSegment) * computeLength() / 3f;
            }
        }
        void ComputeParallelPath()
        {
            _cachedLength = 0f;

            positions = new Vector3[nbPathPoints];

            // Calculate positions points

            int startIndex = Math.Min(handleEndLinkedIndex, handleStartLinkedIndex);
            for (int i = 0; i < nbPathPoints; i++)
            {
                //check handle 1 or 2 direction for parallell positionnin
                if (Path.parrallelRight)
                    positions[i] = handleEndLinkReference.positions[startIndex + i] + handleEndLinkReference.getPathPerpendicularDirection(startIndex + i) * 2;
                else
                    positions[i] = handleEndLinkReference.positions[startIndex + i] - handleEndLinkReference.getPathPerpendicularDirection(startIndex + i) * 2;

            }
        }

        public virtual void preBind()
        {

            if (handleStartIsLinked &&
                handleEndIsLinked &&
                handleEndLinkReference == handleStartLinkReference &&
                handleEndLinkedIndex != handleStartLinkedIndex)
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
                InitHeightForHandles();

                ComputePath();
                if (linearLeveling)
                    SmootHeights();

                computePathAngles();
                validatePath();
            }



            //Debug.WriteLine("path segments: " + pathSegments);

        }

        public virtual void InitHeightForHandles()
        {
            handles[0].Z = world.getHeight(new Vector2(handles[0]));
            handles[handles.Length - 1].Z = world.getHeight(new Vector2(handles[handles.Length - 1]));
        }

        public void SmootHeights()
        {
            float denivelation = handles[handles.Length - 1].Z - handles[0].Z;

            //compute horizontal length, without z component
            float totalLength = 0;
            for (int i = 0; i < nbPathPoints - 1; i++)
            {
                totalLength += (new Vector2(positions[i + 1]) - new Vector2(positions[i])).Length;
            }


            float actualLength = 0f;

            for (int i = 0; i < nbPathPoints - 1; i++)
            {
                actualLength += (new Vector2(positions[i + 1]) - new Vector2(positions[i])).Length;


                float newHeight = handles[0].Z + denivelation / totalLength * actualLength;
                positions[i + 1].Z = newHeight;
            }
            //path[nbPathPoints - 1].Z = handles[handles.Length - 1].Z;
            this._cachedLength = 0;
        }

        protected void ComputePath()
        {
            _cachedLength = 0f;

            positions = new Vector3[nbPathPoints];

            if (handles.Length > 2)
            {
                // Calculate positions points
                for (int i = 0; i < nbPathPoints; i++)
                {
                    float t = i / (float)pathSegments;
                    positions[i] = CalculateBezierPoint(
                        t, handles[0], handles[1], handles[2], handles[3]);

                    //Debug.WriteLine(path[i]);
                }
            }
            else
            {
                for (int i = 0; i < nbPathPoints; i++)
                {
                    float t = i / (float)pathSegments;
                    positions[i] = Vector3.Lerp(handles[0], handles[1], t);
                }
            }

            if (world != null)
            {
                for (int i = 0; i < nbPathPoints; i++)
                    positions[i].Z = world.getHeight(positions[i].X, positions[i].Y);
            }

            if (handleStartIsLinked)
                positions[0] = handleStartLinkReference.positions[handleStartLinkedIndex];
            if (handleEndIsLinked)
                positions[positions.Length - 1] = handleEndLinkReference.positions[handleEndLinkedIndex];
        }


        public virtual void validatePath()
        {
            isValid = true;
            //int i;
            //for (i = 0; i < pathSegments; i++)
            //{
            //    if (segVerticalAngles[i] > inclinaisonMax)
            //    {
            //        isValid = false;
            //        invalidIndex = i;
            //        break;
            //    }
            //}
        }

        public void computePathAngles()
        {
            int arraySizeIncrement = 0;

            if (handleStartIsLinked)
                arraySizeIncrement++;
            if (handleEndIsLinked)
                arraySizeIncrement++;

            Vector3[] segDirs = new Vector3[pathSegments];
            for (int i = 0; i < pathSegments; i++)
            {
                segDirs[i] = positions[i + 1] - positions[i];
                segDirs[i].Normalize();
            }

            //need multipathsegment computation
            //float[] segHorizontalAngleZ = new float[pathSegments];
            segVerticalAngles = new float[pathSegments + arraySizeIncrement];

            for (int i = 0; i < pathSegments; i++)
            {
                segVerticalAngles[i] = Vector3.CalculateAngle(segDirs[i], new Vector3(segDirs[i].X, segDirs[i].Y, 0f));
                if (float.IsNaN(segVerticalAngles[i]))
                    segVerticalAngles[i] = 0;
            }


            segHorizontalAngles = new float[pathSegments + arraySizeIncrement];

            for (int i = 0; i < pathSegments - 1; i++)
            {
                segHorizontalAngles[i] = Vector3.CalculateAngle(new Vector3(segDirs[i].X, segDirs[i].Y, 0f), new Vector3(segDirs[i + 1].X, segDirs[i + 1].Y, 0f));
                if (float.IsNaN(segHorizontalAngles[i]))
                    segHorizontalAngles[i] = 0;
            }
            //for (int i = 0; i < pathSegments; i++)
            //{
            //    Debug.WriteLine("{0} => {1}", i, MathHelper.RadiansToDegrees(segVerticalAngle[i]) );                
            //}

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
                index = positions.Length - 1;    //compute //cachedLength

            float length = 0;
            for (int i = 0; i < index; i++)
            {
                length += (positions[i + 1] - positions[i]).Length;
            }
            return length;
        }

        public Vector3 getVPosInSegment(float t)
        {
            float lengthTarget = computeLength() * t;
            float l = 0f;

            for (int i = 0; i < nbPathPoints - 1; i++)
            {
                Vector3 vDir = positions[i + 1] - positions[i];
                float tmp = vDir.Length;
                if (tmp + l > lengthTarget)
                {
                    //interpolation linéaire
                    float remainingLength = lengthTarget - l;
                    //get ratio in current segment
                    float ratio = remainingLength / tmp;
                    return Vector3.Lerp(positions[i], positions[i + 1], ratio);
                }
                else
                    l += tmp;
            }

            return Vector3.Zero;
        }

        public static Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            Vector3 p = uuu * p0;
            p += 3 * uu * t * p1;
            p += 3 * u * tt * p2;
            p += ttt * p3;

            return p;
        }
    }

}
