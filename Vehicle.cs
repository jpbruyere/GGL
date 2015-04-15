using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GGL;
using OpenTK;
using System.Diagnostics;
using OpenTK.Graphics.OpenGL;
using System.Drawing;

namespace GGL
{
    [Serializable]
    public class Vehicle : ModelInstance
    {
        public enum Order
        {
            stop,
            fullThrottle,
        }

        public enum Sens
        {
            forward,
            backward,
        }
        Road road;
        public int roadIndex = -1;
        public int currentSegmentIndex = 0;

        public float speed = 0f;            //km/h
        public float maxSpeed = 120f;       //km/h
        public float acceleration = 2.0f;   //metre par seconde²
        public float deceleration = 2.0f;
        public Order currentOrder = Order.fullThrottle;
        public Sens currentDirection = Sens.forward;

        public float xAngleVehicle = 0.0f;
        public float yAngleVehicle = 0.0f;
        public float zAngleVehicle = 0.0f;

        public static float fromKHtoMS(float KH)
        {
            return KH / 3.6f;
        }
        public static float fromMSToKH(float MS)
        {
            return MS * 3.6f;
        }

        public List<Vehicle> Wagons = new List<Vehicle>();
        //public int currentPathPointIndex = 0; //helper to speedup position matching along the positions


        public Vehicle(int modelID, Vector3 position, float _zAngle = 0.0f)
            : base(modelID, position, _zAngle)
        { }
        public Vehicle(int modelID)
            : base(modelID, Vector3.Zero, 0.0f)
        { }

        public void Bind(World w)
        {
            road = w.roads[roadIndex];

            id = ModelInstance.NextAvailableID;
            SelectableObject.objectsDic.Add(id, this);

            this.x = road.segments[0].positions[0].X;
            this.y = road.segments[0].positions[0].Y;
            this.z = (road.segments[0].positions[0] + GenericRoadSegment.vboZadjustment).Z;


            this.move(0f);

        }

        public void attachWagon(Vehicle v)
        {
            v.id = ModelInstance.NextAvailableID;
            SelectableObject.objectsDic.Add(v.id, v);

            v.road = road;
            v.x = road.segments[0].positions[0].X;
            v.y = road.segments[0].positions[0].Y;
            v.z = (road.segments[0].positions[0] + GenericRoadSegment.vboZadjustment).Z;


            float trainMvt = v.model.bounds.width   / World.unity;
            this.moveTrainAndWagon(trainMvt);

            this.Wagons.Add(v);

            //setup angles
            v.move(0f);

        }

        Matrix4 transformations;

        public void Bind(int _roadIndex, World w)
        {
            w.vehicles.Add(this);
            roadIndex = _roadIndex;
            Bind(w);
        }

        public void reverseDirection()
        {
            switch (currentDirection)
            {
                case Sens.forward:
                    currentDirection = Sens.backward;
                    break;
                case Sens.backward:
                    currentDirection = Sens.forward;
                    break;
                default:
                    break;
            }
            if (zAngleVehicle == 0)
                zAngleVehicle = MathHelper.Pi;
            else
                zAngleVehicle = 0;
            //foreach (Vehicle w in Wagons)
            //{
            //    //if (Wagons.IndexOf(w) == Wagons.Count - 1)
            //    //    break;
            //    float trainMvt = w.model.bounds.x1 *2 / Game.unity;
            //    this.moveTrainAndWagon(trainMvt);
            //}
        }

        Vector3 vDir = Vector3.Zero;

        public float actualPosInSegment;   //0->1

        public const float SegmentIncrement = 0.01f;

        public void computeVDir()
        {
            Vector3 v1 = road.segments[currentSegmentIndex].getVPosInSegment(actualPosInSegment);
            Vector3 v2 = Vector3.Zero;

            //actualPosInSegment = positionInSegment();

            float nextPosInSegment = 0f;

            switch (currentDirection)
            {
                case Sens.forward:
                    nextPosInSegment = actualPosInSegment + 0.02f;
                    break;
                case Sens.backward:
                    nextPosInSegment = actualPosInSegment - 0.02f;
                    break;
            }


            v2 = road.segments[currentSegmentIndex].getVPosInSegment(nextPosInSegment);

            switch (currentDirection)
            {
                case Sens.forward:
                    vDir = v1 - v2;
                    break;
                case Sens.backward:
                    vDir = v1 - v2;
                    break;
            }

            vDir.Normalize();
        }

        public override void Render()
        {
            //GL.Color3(Color.LightGreen);
            //GL.Disable(EnableCap.DepthTest);
            //GL.PointSize(2f);
            //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Point);
            //GL.Begin(BeginMode.Points);
            //{

            //    GL.Vertex3(v1);
            //    GL.Vertex3(v2);
            //}
            //GL.End();
            //GL.LineWidth(1.5f);
            //GL.Begin(BeginMode.Lines);
            //{
            //    GL.Vertex3(v2);
            //    GL.Vertex3(v2 + vDir * 3f);
            //}
            //GL.End();

            //GL.Color3(Color.Red);
            //GL.Begin(BeginMode.Lines);
            //{
            //    GL.Vertex3(v2);
            //    GL.Vertex3(v2 + Vector3.UnitZ * 3f);
            //}
            //GL.End();

            //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

            //GL.Enable(EnableCap.DepthTest);


            GL.MatrixMode(MatrixMode.Modelview);
            Matrix4 Rot = Matrix4.CreateRotationZ(zAngleVehicle); 
            Rot *= Matrix4.CreateRotationY(yAngle);
            Rot *= Matrix4.CreateRotationZ(zAngle);
            Rot *= Matrix4.CreateRotationX(xAngle);
            //Matrix4 Rot = Matrix4.CreateRotationZ(zAngle);

            Matrix4 transformation = Rot * Matrix4.CreateTranslation(x, y, z);

            GL.PushMatrix();
            GL.MultMatrix(ref transformation);

            GL.LoadName(id);
            GL.Enable(EnableCap.Lighting);
            model.Render();

            GL.PopMatrix();

           

            foreach (Vehicle v in Wagons)
            {
                v.Render();
            }
        }

        public Vector3 Position
        {
            get { return new Vector3(x, y, z); }
            set
            {
                x = value.X;
                y = value.Y;
                z = value.Z;
            }

        }

        //donne la position entre p0 et pNbPtInPath en pour 1 (0->1)
        //float positionInSegment()
        //{
        //    float totalLength = road.segments[currentSegmentIndex].computeLength();


        //    float length = 0f;

        //    for (int i = 0; i < currentPathPointIndex; i++)
        //    {
        //        length += (road.segments[currentSegmentIndex].positions[i + 1] - road.segments[currentSegmentIndex].positions[i]).Length;
        //    }

        //    float lengthV1Pos = (Position - v1).Length;

        //    float result = (length + lengthV1Pos) / totalLength;
        //    Debug.WriteLine("pos:{0}", result);
        //    return result;
        //}


        public void animate()
        {
            //computeVDir();

            float sec = (float)(World.CurrentWorld.elapsedMiliseconds / 1000);

            switch (currentOrder)
            {
                case Order.stop:
                    if (speed > 0f)
                    {
                        speed -= fromMSToKH(deceleration * sec);
                        if (speed < 0f)
                            speed = 0f;
                    }
                    break;
                case Order.fullThrottle:
                    if (speed < maxSpeed)
                    {
                        speed += fromMSToKH(acceleration * sec);
                    }
                    break;
                default:
                    break;
            }

            //dist parcourue en M
            float distM = fromKHtoMS(speed) * sec;

            moveTrainAndWagon(distM);
        }

        void moveTrainAndWagon(float distInMetters)
        {
            move(distInMetters);
            //computeVDir();
            foreach (Vehicle v in Wagons)
            {                
                v.move(distInMetters);
                //v.computeVDir();
            }        
        }
        Vector3 v1;
        Vector3 v2;
        void move(float distInMetters)
        {
            v1 = road.segments[currentSegmentIndex].getVPosInSegment(actualPosInSegment); 
            v2 = Vector3.Zero;

            float distOpenGL = distInMetters * World.unity;

            float distInSegment = distOpenGL / road.segments[currentSegmentIndex].computeLength();

            float newPosInSegment = 0f;
            switch (currentDirection)
            {
                case Sens.forward:
                    newPosInSegment = actualPosInSegment + distInSegment;
                    break;
                case Sens.backward:
                    newPosInSegment = actualPosInSegment - distInSegment;
                    break;
            }
            //Debug.WriteLine(newPosInSegment);
            if (newPosInSegment > 1.0)
            {
                //should go to next segment
                //search for matching positions point position

                GenericRoadSegment curRs = road.segments[currentSegmentIndex];

                bool matchFound = false;

                foreach (GenericRoadSegment rs in road.segments)
                {
                    if (rs != curRs)
                    {
                        for (int i = 0; i < rs.nbPathPoints; i++)
                        {
                            if ((rs.positions[i] - curRs.positions[curRs.nbPathPoints - 1]).LengthFast < 0.03f)
                            {
                                //match
                                float remainingDistOpenGL = (newPosInSegment - 1.0f) * road.segments[currentSegmentIndex].computeLength();
                                currentSegmentIndex = road.segments.IndexOf(rs);
                                float remainingDistInNextSegment = remainingDistOpenGL / road.segments[currentSegmentIndex].computeLength();

                                //test next segment orientation
                                if (i == 0)
                                {
                                    //same direction
                                    newPosInSegment = remainingDistInNextSegment;
                                }
                                else if (i == rs.nbPathPoints - 1)
                                {
                                    //opposite
                                    reverseDirection();
                                    newPosInSegment = 1 - remainingDistInNextSegment;
                                }
                                else
                                {
                                    //carefour
                                }

                                matchFound = true;
                                break;
                            }
                        }
                    }
                    if (matchFound)
                        break;
                }

                if (matchFound)
                {
                    Position = road.segments[currentSegmentIndex].getVPosInSegment(newPosInSegment);
                    actualPosInSegment = newPosInSegment;
                }
                else
                {
                    speed = 0f;
                    reverseDirection();
                }
            }
            else if (newPosInSegment < 0.0)
            {
                //should go to next segment
                //search for matching positions point position

                GenericRoadSegment curRs = road.segments[currentSegmentIndex];

                bool matchFound = false;

                foreach (GenericRoadSegment rs in road.segments)
                {
                    if (rs != curRs)
                    {
                        for (int i = 0; i < rs.nbPathPoints; i++)
                        {
                            if ((rs.positions[i] - curRs.positions[0]).LengthFast < 0.03f)
                            {
                                //match
                                float remainingDistOpenGL = -newPosInSegment * road.segments[currentSegmentIndex].computeLength();
                                currentSegmentIndex = road.segments.IndexOf(rs);
                                float remainingDistInNextSegment = remainingDistOpenGL / road.segments[currentSegmentIndex].computeLength();

                                //should test next segment orientation
                                if (i == rs.nbPathPoints - 1)
                                {
                                    //same direction
                                    newPosInSegment = 1f - remainingDistInNextSegment;
                                }
                                else if (i == 0)
                                {
                                    //opposite
                                    reverseDirection();
                                    newPosInSegment = remainingDistInNextSegment;
                                }
                                else
                                {
                                    //carefour
                                }

                                matchFound = true;
                                break;
                            }
                        }
                    }
                }

                if (matchFound)
                {
                    Position = road.segments[currentSegmentIndex].getVPosInSegment(newPosInSegment);
                    actualPosInSegment = newPosInSegment;
                }
                else
                {
                    speed = 0f;
                    reverseDirection();
                }
            }
            else
            {
                Position = road.segments[currentSegmentIndex].getVPosInSegment(newPosInSegment);
                actualPosInSegment = newPosInSegment;
            }

            v2 = Position;

            if (currentDirection == Sens.forward)
                vDir = v2 - v1;
            else
                vDir = v1 - v2;

            vDir.Normalize();

            //computeVDir();
            float a = Vector3.CalculateAngle(vDir,new Vector3(vDir.X,vDir.Y,0f));
            if (!float.IsNaN(a))
                yAngle= a;
            if (vDir.Z < 0)
                yAngle= -yAngle;


            //Debug.WriteLine(yAngle + " " + vDir + " " + v1 + " " + v2);



            ////if (currentDirection == Sens.forward)
            ////{
            ////    if (vDir.Y < 0)
            ////        yAngle = M Vector3.CalculateAngle(vDir, Vector3.UnitZ);
            ////    else
            ////        yAngle = Vector3.CalculateAngle(vDir, Vector3.UnitZ) ;
            ////}
            ////else
            ////    yAngle = -MathHelper.PiOver2 + Vector3.CalculateAngle(vDir, Vector3.UnitZ);

            /////////yAngle = Vector3.CalculateAngle(vDir, Vector3.UnitY);

            a = Vector3.CalculateAngle(new Vector3(vDir.X, vDir.Y, 0f), Vector3.UnitX);
            if (!float.IsNaN(a))
            {
                zAngle= MathHelper.Pi + a ;
            }
            if (vDir.Y < 0)
                zAngle= -zAngle;

            //zAngle = MathHelper.PiOver2;
           
        }

    }
}
