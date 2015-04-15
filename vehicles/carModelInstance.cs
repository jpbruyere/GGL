using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using Jitter.Dynamics;
using JitterExtensions;
using OpenTK.Graphics.OpenGL;
using Jitter.LinearMath;
//using System.Drawing;
using Jitter.Collision.Shapes;

using go;
using OpenTK.Input;

namespace GGL
{
    public class carModelInstance : ModelInstance
    {
        public carModelInstance(int modelID, Vector3 pos, float _zAngle = 0.0f)
            : base(modelID, pos, _zAngle)
        {
        }

        protected override void initJitterDynamics()
        {
            #region jitter dynamics
            Model m = model as Model;
            if (m != null)
            {
                CompoundShape.TransformedShape lower = new CompoundShape.TransformedShape(
                    new BoxShape(2.5f, 6f, 2.0f), JMatrix.Identity, JVector.Backward * -1.0f);

                CompoundShape.TransformedShape upper = new CompoundShape.TransformedShape(
                    new BoxShape(2.0f, 3.0f, 0.5f), JMatrix.Identity, JVector.Backward * 0.75f + JVector.Up * 1.0f);

                CompoundShape.TransformedShape[] subShapes = { lower, upper };

                m.shape = new CompoundShape(subShapes);

                carBody = new CarBody(World.CurrentWorld.physicalWorld, m.shape);

                body.Position = new Jitter.LinearMath.JVector(x, y, z);
                //body.Orientation = JMatrix.CreateRotationX(JMath.PiOver2);
                // adjust some driving values
                carBody.SteerAngle = 30;
                carBody.DriveTorque = 155;
                carBody.AccelerationRate = 20;
                carBody.SteerRate = 2f;
                carBody.AdjustWheelValues();
                body.AllowDeactivation = false;
                
                //body.EnableSpeculativeContacts = true;
                //carBody.Tag = BodyTag.DontDrawMe;
                carBody.IsStatic = true;    
            }
            #endregion
        }

        public CarBody carBody
        {
            get { return body as CarBody; }
            set { body = value; }
        }

        public Model wheelModel;


        #region Draw Wheels
        private void DrawWheels()
        {
            GL.MatrixMode(MatrixMode.Modelview);

            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);

            for (int i = 0; i < carBody.Wheels.Length; i++)
            {
                Wheel wheel = carBody.Wheels[i];

                GL.PushMatrix();
                GL.Color3(Color.White);
                Matrix4 t;

                if (i % 2 != 0)
                    t = Matrix4.CreateRotationX(MathHelper.Pi);
                else
                    t = Matrix4.Identity;

                t *=
                    Matrix4.CreateRotationY(MathHelper.PiOver2) *
                    Matrix4.CreateRotationX(MathHelper.DegreesToRadians(wheel.WheelRotation)) *
                    Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(wheel.SteerAngle)) *
                    computeTransformations(wheel.GetWorldPosition(), body.Orientation);

                GL.MultMatrix(ref t);

                if (enableJitterDebugDraw)
                {
                    //drawJitterDebug(carBody.Wheels[i].);
                }else{
                    GL.LineWidth(0.5f);
                    wheelModel.Render();
                }

                GL.PopMatrix();


                GL.Disable(EnableCap.Texture2D);

                //wheel debug
                GL.LineWidth(2.5f);
                GL.PushAttrib(AttribMask.EnableBit);
                GL.Disable(EnableCap.Lighting);
                GL.Begin(BeginMode.Lines);


                Vector3 worldAxis =Conversion.jToOtk( JVector.Transform(new JVector(0, 0, 1), carBody.Orientation));
                Vector3 worldPos = Conversion.jToOtk( carBody.Position + JVector.Transform(wheel.Position, carBody.Orientation));

                GL.Color3(Color.NavyBlue);
                GL.Vertex3(worldPos);
                GL.Vertex3(worldPos + worldAxis*3f);


                GL.Color3(Color.Blue);                
                GL.Vertex3(wheel.grndPos);
                GL.Vertex3(wheel.grndPos + wheel.grndUp);
                
                GL.Color3(Color.MediumBlue);
                GL.Vertex3(wheel.grndPos);
                GL.Vertex3(wheel.grndPos + wheel.grndNormal *2f);

                GL.Color3(Color.Goldenrod);
                GL.Vertex3(wheel.grndPos);
                GL.Vertex3(wheel.grndPos + wheel.grndForward);

                GL.Color3(Color.MediumTurquoise);
                GL.Vertex3(worldPos);
                GL.Vertex3(worldPos + wheel.wUp);

                GL.Color3(Color.Yellow);
                GL.Vertex3(worldPos);
                GL.Vertex3(worldPos + wheel.wFwd);

                GL.Color3(Color.YellowGreen);
                GL.Vertex3(worldPos);
                GL.Vertex3(worldPos + wheel.wLeft);


                GL.End();
                GL.PopAttrib();
            }
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
        }
        #endregion

        private void DrawChassis()
        {



            if (enableJitterDebugDraw)
            {
                drawJitterDebug(carBody);
            }
            else
            {
                JVector depl = JVector.Transform(new JVector(0, 0, 1), carBody.Orientation);
                Matrix4 t = computeTransformations(carBody.Position - depl, carBody.Orientation);

                GL.PushMatrix();
                GL.MultMatrix(ref t);
                model.Render();
                GL.PopMatrix();
            }

        }
        public override void Anim()
        {
            base.Anim();
            //car anim

            float steer = 0, accelerate = 0;

            //if (Interface.Keyboard[Key.Up])
            //    accelerate = 1.0f;
            //else if (Interface.Keyboard[Key.Down])
            //    accelerate = -1.0f;
            //else
            //    accelerate = 0.0f;

            //if (Interface.Keyboard[Key.Left])
            //    steer = 1;
            //else if (Interface.Keyboard[Key.Right])
            //    steer = -1;
            //else
            //    steer = 0;
            (body as CarBody).SetInput(accelerate, steer);
        }
        public override void Render()
        {
            GL.PushAttrib(AttribMask.EnableBit);

            GL.LoadName(id);

            Mesh[] ms = null;

            if (model is ObjModel)
                ms = (model as ObjModel).meshes[LOD];

            if (enableJitterDebugDraw)
                drawJitterDebug();
            else
            {
                model.LOD = LOD;
				            
                DrawChassis();
                DrawWheels();
            }
            //GL.PopMatrix();
            GL.PopAttrib();
        }

        public void drawJitterDebug(RigidBody rb)
        {
            //GL.PushAttrib(AttribMask.EnableBit);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            GL.Disable(EnableCap.Lighting);
            GL.Disable(EnableCap.Texture2D);
            GL.LineWidth(0.5f);
            GL.Color3(Color.LightBlue);
            rb.EnableDebugDraw = true;
            rb.DebugDraw(new JitterDebugDrawer());

            GL.LineWidth(4.5f);
            GL.Color3(Color.Yellow);
            GL.Begin(BeginMode.Lines);
            GL.Vertex3(Conversion.jToOtk(carBody.Position));
            //GL.Vertex3(Conversion.jToOtk(carBody.Position) + Vector3.Cross(new Vector3(-carBody.Orientation.M31, -carBody.Orientation.M32, - carBody.Orientation.M33), Vector3.UnitX));
            GL.Vertex3(Conversion.jToOtk(carBody.Position) + new Vector3(-carBody.Orientation.M21, -carBody.Orientation.M22, -carBody.Orientation.M23));
            GL.End();
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            //GL.PopAttrib();        
        }
    }
}
