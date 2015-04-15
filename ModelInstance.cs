using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;
using GGL;
using OpenTK;
using Jitter.Dynamics;
using Jitter.LinearMath;
using System.Diagnostics;
using System.Drawing;
using Jitter.Dynamics.Constraints;
using OpenTK.Input;

namespace GGL
{
    [Serializable]
    public class ModelInstance : SelectableObject
    {
        public static List<ModelInstance> AnimatedModelInstance = new List<ModelInstance>();
        public static bool trackPosition = false;
        public static bool followView = false;
        public static bool trackView = false;
        public static ModelInstance trackedObject;

        public string Name
        {
            get { return Model.modelList[ModelIndex].Name + "_" + id; }
        }

        public Model model
        { get { return Model.modelList[ModelIndex]; } }

        public int ModelIndex;
        public RigidBody body;
        public bool enableJitterDebugDraw = false;

        public Texture texture;
        public Shader shader;

        public float distFrom(Vector3 v)
        {
            float res = (v - Position).LengthFast;
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
                if (_LOD == value || value < 0 || model is Building)
                    return;
                if (model is ObjModel)
                {
                    if (value >= (model as ObjModel).meshes.Count - 1)
                    {
                        _LOD = (model as ObjModel).meshes.Count - 1;
                        return;
                    }
                }


                _LOD = value;
            }
        }

        public void UpdateLod(Vector3 vEye)
        {
            float distFromEye = distFrom(vEye);
            if (distFromEye < 30)
                LOD = 0;
            else if (distFromEye < 200)
                LOD = 1;
            else
                LOD = 2;

            if (Name == directories.rootDir + @"obj\heolienne.obj_105")
                System.Diagnostics.Debug.WriteLine(_LOD);
        }
        //[NonSerialized]        
        public float x = 0.0f;
        public float y = 0.0f;
        public float z = 0.0f;
        public float angle = 0.0f;

        public float xAngle = 0.0f;
        public float yAngle = 0.0f;
        public float zAngle = 0.0f;
        
        public bool IsFrozen
        {
            get { return body == null ? true : !body.IsActive; }
            set
            {
                if (body != null)
                    body.IsActive = !value;
            }
        }
        public virtual Vector3 Position
        {
            get {
                return body == null ? 
                    new Vector3(x, y, z) :
                    new Vector3(body.Position.X, body.Position.Y, body.Position.Z);            
            }
            set
            {
                if (body != null)
                {
                    body.Position = new JVector(value.X, value.Y, value.Z); 
                }
                else
                {
                    x = value.X;
                    y = value.Y;
                    z = value.Z;
                }

            }
        }
        public virtual Vector3 Orientation
        {
            get 
            {
                return body == null ? Vector3.UnitZ : new Vector3(-body.Orientation.M21, -body.Orientation.M22, -body.Orientation.M23);
            }
        }

        public ModelInstance(int modelID, float _x, float _y, float _z = 0f)
        {
            //if (objMod != null)
            //    modelPath = objMod.objPath;
            x = _x;
            y = _y;
            z = _z;
            ModelIndex = modelID;
            SelectableObject.registerObject(this);

            initJitterDynamics();
        }
        public ModelInstance(int modelID, Vector3 pos, float _zAngle = 0.0f)
        {
            //modelPath = _objClass.objPath;
            zAngle = _zAngle;
            x = pos.X;
            y = pos.Y;
            z = pos.Z;

            ModelIndex = modelID;
            SelectableObject.registerObject(this);

            initJitterDynamics();
        }
        
        protected virtual void initJitterDynamics()
        {
            #region jitter dynamics
            Model m = model as Model;
            if (m != null)
            {
                if (m.shape != null)
                {
                    body = new RigidBody(m.shape);
                    body.Position = new Jitter.LinearMath.JVector(x, y, z);
                    body.Material.Restitution = 0.0f;                    
                    //body.EnableSpeculativeContacts = true;
                    body.LinearVelocity = Jitter.LinearMath.JVector.Zero;
                    body.Mass = 50f;
                    body.IsActive = false;

                    body.Tag = false;
                }
            }
            #endregion        
        }



        
        //public ModelInstance(Model _objClass, float _x, float _y, float _z = 0f)
        //{
        //    x = _x;
        //    y = _y;
        //    z = _z;
        //    model = _objClass;
        //    Prepare();
        //}
        //public ModelInstance(Model _objClass, Vector3 pos, float _zAngle = 0.0f)
        //{
        //    //modelPath = _objClass.objPath;
        //    zAngle = _zAngle;
        //    x = pos.X;
        //    y = pos.Y;
        //    z = pos.Z;

        //    model = _objClass;
        //    Prepare();
        //}

        public Matrix4 computeTransformations(JVector p, JMatrix r)
        {
            return new Matrix4(
                r.M11, r.M12, r.M13, 0,
                r.M21, r.M22, r.M23, 0,
                r.M31, r.M32, r.M33, 0,
                p.X, p.Y, p.Z, 1);
        }
        Matrix4 Transformations
        {
            get
            {
                Matrix4 transformation;

                if (body != null)
                {
                    //Debug.WriteLine(body.Position + " " + body.Orientation.M11, body.Orientation.M12, body.Orientation.M13);
                    if (model is ObjModel)
                        transformation = Matrix4.CreateTranslation(0, 0, -model.bounds.height / 2f);
                    else
                        transformation = Matrix4.Identity;// Matrix4.CreateTranslation(-model.bounds.width / 2f, -model.bounds.length / 2f, -model.bounds.height / 2f);
                    transformation *= computeTransformations(body.Position, body.Orientation);
                    //Positionne par rapport au centre de gravité
                }
                else
                {
                    Matrix4 Rot = Matrix4.CreateRotationY(yAngle);
                    Rot *= Matrix4.CreateRotationZ(zAngle);
                    Rot *= Matrix4.CreateRotationX(xAngle);
                    //Matrix4 Rot = Matrix4.CreateRotationZ(zAngle);
                    transformation = Rot * Matrix4.CreateTranslation(x, y, z);
                }

                return transformation;
            }
        
        }
        public virtual void Anim()
        { 
        
        }
        public virtual void Render()
        {
            GL.PushAttrib(AttribMask.EnableBit);

            GL.MatrixMode(MatrixMode.Modelview);
            GL.PushMatrix();

            Matrix4 t = Transformations;
            

            GL.LoadName(id);            

            Mesh[] ms = null;

            if (model  is ObjModel)
                ms = (model as ObjModel).meshes[LOD];


            foreach (Animation a in animations)
            {
                t *= a.transformation;
            }            

            if (enableJitterDebugDraw)
                drawJitterDebug();
            else
            {
                GL.MultMatrix(ref t);

                model.LOD = LOD;

                model.Render();
            }
            GL.PopMatrix();
            GL.PopAttrib();
        }

        public virtual void drawJitterDebug()
        {
            //GL.PushAttrib(AttribMask.EnableBit);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            GL.Disable(EnableCap.Lighting);
            GL.LineWidth(2.5f);
            GL.Color3(Color.White);
            body.EnableDebugDraw = true;
            body.DebugDraw(new JitterDebugDrawer());
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            //GL.PopAttrib();        
        }
        public void drawBoundingBox()
        {
            Matrix4 t = Transformations;


            GL.PushMatrix();
            GL.MultMatrix(ref t);

            model.bounds.render();


            GL.Color3(Color.Blue);

            GL.PopMatrix();
        }

        public List<Animation> animations = new List<Animation>();

        public void AddAnimation(Animation a)
        {
            a.mi = this;
            if (!AnimatedModelInstance.Contains(this))
                AnimatedModelInstance.Add(this);
            
            animations.Add(a);
        }
        public void DeleteAnimation(Animation a)
        {
            animations.Remove(a);
            if (animations.Count == 0)
                AnimatedModelInstance.Remove(this);
        }


        
    }
}
