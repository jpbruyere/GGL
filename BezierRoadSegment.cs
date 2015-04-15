using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using OTKGL;

namespace OTKGL
{
    [Serializable]
    public class BezierRoadSegment : RoadSegment
    {
        //for debug drawing of handles direction
        Vector3 pathDir;
        Vector3 h3Dir;
        public float xAngle = 0;
        public float yAngle = 0;
        public float zAngle = 0;

        public BezierRoadSegment(Vector2 p1, Vector2 p4,  int _pathSegments = 20) : base()
        {                        
            handles = new Vector3[4];

            pathSegments = _pathSegments;

            handles[0] = new Vector3(p1.X, p1.Y, 0f);
            handles[1] = new Vector3(Vector2.Lerp(p1, p4, 0.25f));
            handles[2] = new Vector3(Vector2.Lerp(p1, p4, 0.75f));
            handles[3] = new Vector3(p4.X, p4.Y, 0f);
        }
        public BezierRoadSegment(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, int _pathSegments = 20) : base()
        {            
            handles = new Vector3[4];

            pathSegments = _pathSegments;

            handles[0] = new Vector3(p1.X, p1.Y, 0f);
            handles[1] = new Vector3(p2.X, p2.Y, 0f);
            handles[2] = new Vector3(p3.X, p3.Y, 0f);
            handles[3] = new Vector3(p4.X, p4.Y, 0f);
        }
        public BezierRoadSegment(Road _road, Vector3[] _handles, int _pathSegments = 20)
            : base(_handles,_pathSegments)
        {
            road = _road;
            road.newSegment = this;

            handles = new Vector3[4];
            handles[0] = new Vector3(Mouse3d.Position);

            for (int h = 1; h < 4; h++)
            {
                handles[h] = handles[0];
            }

            Road.newSegmentInit = true;
           
            road.currentHandleInNewSegment = 3;

            Mouse3d.setPosition(handles[0]);

            preBind(road);

            if (Road.currentSegment != null)
            {
                road.currentHandleInNewSegment = 0;
                Road.currentRoad.checkLinkForNewSegment();
                road.currentHandleInNewSegment = 3;
                preBind(road);
            }        
        }

        public override Vector3 startVector
        {
            get
            {
                Vector3 v = handles[0] - handles[1];
                v.Normalize();
                return v;
            }
        }
        public override Vector3 endVector
        {
            get
            {
                Vector3 v = handles[3] - handles[2];
                v.Normalize();
                return v;
            }
        }

        protected override void ComputePath()
        {
            _cachedLength = 0f;

            path = new Vector3[nbPathPoints];

            // Calculate positions points
            for (int i = 0; i < nbPathPoints; i++)
            {
                float t = i / (float)pathSegments;
                path[i] = Path.CalculateBezierPoint(
                    t, handles[0], handles[1], handles[2], handles[3]);

                    if (world != null)
                        path[i].Z = world.getHeight(path[i].X, path[i].Y);
                //Debug.WriteLine(path[i]);
            }

            if (handleStartIsLinked)            
                path[0] = handleStartLinkReference.path[handleStartLinkedIndex];
            if (handleEndIsLinked)
                path[path.Length - 1] = handleEndLinkReference.path[handleEndLinkedIndex];
        }

        

        public void RenderHandle0Axes()
        {
            GL.Disable(EnableCap.DepthTest);

            //debug draw angle with x
            GL.LineWidth(3f);
            GL.Color3(Color.SeaGreen);
            GL.Begin(BeginMode.Lines);
            GL.Vertex3(handles[0]);
            GL.Vertex3(handles[0] + h3Dir * 3f);
            GL.Color3(Color.Red);
            GL.Vertex3(handles[0]);
            GL.Vertex3(handles[0] + Vector3.UnitX * 3f);
            GL.Color3(Color.Green);
            GL.Vertex3(handles[0]);
            GL.Vertex3(handles[0] + Vector3.UnitY * 3f);
            GL.Color3(Color.Blue);
            GL.Vertex3(handles[0]);
            GL.Vertex3(handles[0] + Vector3.UnitZ * 3f);

            GL.End();
            GL.Enable(EnableCap.DepthTest);
        }

        public void RenderHandles()
        {
            #region Draw point handles (white large sphere)
            GL.MatrixMode(MatrixMode.Modelview);

            
            for (int i = 0; i < 4; i++)
            {
                //if (!((handleStartIsLinked && handleEndIsLinked) && (i == 1 || i == 2)))
                //{
                    GL.Color3(Color.Yellow);
                    if (road.currentHandleInNewSegment == i)
                    {
                        if ((handleStartIsLinked && i == 0) || (handleEndIsLinked && i == 3))
                            GL.Color3(Color.Green);
                        else
                            GL.Color3(Color.CornflowerBlue);
                    }
                    else
                    {
                        if ((handleStartIsLinked && i == 0) || (handleEndIsLinked && i == 3))
                            GL.Color3(Color.GreenYellow);
                    }
                    GL.PushMatrix();
                    
                    float scale = (handles[i] - World.vEye).LengthFast *0.03f;
                    
                    GL.LoadName(RoadSegment.HANDLE_NEW_0 + i);
                    GL.Translate(handles[i]);
                    GL.Scale(new Vector3(scale, scale, scale));
                   
                    sphere.Draw();
                    GL.PopMatrix();
                //}
            }
            #endregion

            #region Draw lines of handles
            GL.LineWidth(1f);
            GL.Color3(Color.WhiteSmoke);
            GL.Begin(BeginMode.Lines);
            for (int i = 0; i < 4; i++) GL.Vertex3(handles[i]);
            GL.End();
            #endregion
        
        }

        public void renderDistToGround()
        {
            GL.LineWidth(2f);
            GL.Color3(Color.LightGray);
            GL.Begin(BeginMode.Lines);
            for (int i = 0; i < nbPathPoints; i++)
            {
                GL.Vertex3(path[i]);
                GL.Vertex3(world.getHeightVector(new Vector2(path[i])));
            }
            GL.End();
        }
        
        public override void RenderPath()
        {

            GL.PushAttrib(AttribMask.EnableBit);

            GL.Disable(EnableCap.Lighting);
            GL.Disable(EnableCap.Texture2D);

            RenderHandles();

            GL.Disable(EnableCap.DepthTest);
            

            #region Process path curve


            // Draw line of the curve path
            GL.PointSize(2f);
            if (isValid)
                GL.Color3(Color.Chartreuse);
            else
                GL.Color3(Color.Crimson);

            GL.Begin(BeginMode.LineStrip);
            for (int i = 0; i < nbPathPoints; i++) GL.Vertex3(path[i]);
            GL.End();

            renderDistToGround();

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

        public override void InitHeightForHandles()
        {
            float height0 = world.getHeight(new Vector2(handles[0]));
            float height3 = world.getHeight(new Vector2(handles[3]));

            if (handleStartIsLinked)
                handles[0].Z = handleStartLinkReference.path[handleStartLinkedIndex].Z;
            else
                handles[0].Z = height0;
            if (handleEndIsLinked)
                handles[3].Z = handleEndLinkReference.path[handleEndLinkedIndex].Z;
            else
                handles[3].Z = height3;

            handles[1].Z = world.getHeight(new Vector2(handles[1]));// + dif * 0.25f;
            handles[2].Z = world.getHeight(new Vector2(handles[2]));// +dif * 0.75f;  
      
            //debug
            h3Dir = handles[3] - handles[0];
            h3Dir.Normalize();

            xAngle = MathHelper.RadiansToDegrees( Vector3.CalculateAngle(new Vector3(new Vector2(h3Dir)), Vector3.UnitX));
            yAngle = MathHelper.RadiansToDegrees(Vector3.CalculateAngle(new Vector3(new Vector2(h3Dir)), Vector3.UnitY));
            zAngle = MathHelper.RadiansToDegrees(Vector3.CalculateAngle(h3Dir, Vector3.UnitZ));
        }        

        public void computeHandle1_2WhenSingleLinked(int targetPathIndexInCurrentSegment, BezierRoadSegment targetSegment, int targetHandleInNewSegment)
        {

            pathDir = targetSegment.getPathDirection(targetPathIndexInCurrentSegment);
            pathDir.Z = 0f;

            h3Dir = handles[3] - handles[0];
            h3Dir.Normalize();
            h3Dir.Z = 0f;

            float a = Vector3.CalculateAngle(pathDir, h3Dir);
            

            if (targetHandleInNewSegment == 0)
            {
                if (a < MathHelper.PiOver2)
                    handles[1] = handles[0] + targetSegment.getDirection(targetPathIndexInCurrentSegment) * computeLength() / 3f;
                else
                    handles[1] = handles[0] - targetSegment.getDirection(targetPathIndexInCurrentSegment) * computeLength() / 3f;
            }
            else
            {
                if (a < MathHelper.PiOver2)
                    handles[2] = handles[3] + targetSegment.getDirection(targetPathIndexInCurrentSegment) * computeLength() / 3f;
                else
                    handles[2] = handles[3] + targetSegment.getDirection(targetPathIndexInCurrentSegment) * computeLength() / 3f;
            }
        }
    }
}
