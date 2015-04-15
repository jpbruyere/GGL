// Released to the public domain. Use, modify and relicense at will.
using System;

using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using OpenTK.Input;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using GLU = OpenTK.Graphics.Glu;
using GGL;
using Examples.Shapes;

using go;
using System.Threading;

using System.Drawing.Imaging;



namespace GGL
{
	public class GGLSimpleTestWindow : GameWindow
    {
		#if _WIN32 || _WIN64
		public const string rootDir = @"d:\";
		#elif __linux__
		public const string rootDir = @"/mnt/data/";
		#endif

		/// <summary>Creates a 800x600 window with the specified title.</summary>
		public GGLSimpleTestWindow(int _width, int _height)
			: base(_width, _height, new OpenTK.Graphics.GraphicsMode(32, 24, 0, 8), "GGL")
		{
			VSync = VSyncMode.On;
		}        

		#region FPS
		static int _fps = 0;
		public static int fpsMin = int.MaxValue;
		public static int fpsMax = 0;
		public static int fps {
			get { return _fps; }
			set {
				_fps = value;
				if (_fps > fpsMax)
					fpsMax = _fps;
				else if (_fps < fpsMin)
					fpsMin = _fps;
			}

		}			
		static void resetFps ()
		{
			fpsMin = int.MaxValue;
			fpsMax = 0;
			_fps = 0;
		}
		#endregion

        public static Matrix4 modelview;
        public static Matrix4 projection;

        public static Vector3 vEye = new Vector3(5.0f, 5f, 7.0f);    // Camera Position
        public static Vector3 vEyeTarget = Vector3.Zero;
        public static Vector3 vLook = new Vector3(0f, 1f, -0.7f);  // Camera vLook Vector

		float _zFar = 6400.0f;

		public float zFar {
			get { return _zFar; }
			set {
				_zFar = value;
			}
		}

		public float zNear = 0.1f;
		public float fovY = (float)Math.PI / 4;

        float MoveSpeed = 0.5f;

		public virtual void drawScene()
        {

        }
		#region Game windows events
		protected override void OnUpdateFrame(FrameEventArgs e)
		{	
			base.OnUpdateFrame(e);
			#region Keyboard handling
			bool processMoveKey = true;

			if (processMoveKey)
			{                
				if (Keyboard[Key.Up])
				{
					moveCamera(-vLookDirOnXYPlane * MoveSpeed);
					UpdateViewMatrix();
				}
				if (Keyboard[Key.Down])
				{
					moveCamera(vLookDirOnXYPlane * MoveSpeed);
					UpdateViewMatrix();
				}
				if (Keyboard[Key.Left])
				{
					moveCamera(vLookPerpendicularOnXYPlane * MoveSpeed);
					UpdateViewMatrix();
				}
				if (Keyboard[Key.Right])
				{
					moveCamera(-vLookPerpendicularOnXYPlane * MoveSpeed);
					UpdateViewMatrix();
				}
				if (Keyboard[Key.PageUp])
				{
					vEye.Z += MoveSpeed;
					UpdateViewMatrix();
				}
				if (Keyboard[Key.PageDown])
				{
					vEye.Z -= MoveSpeed;
					UpdateViewMatrix();
				}
			}
			#endregion		
		}
		protected override void OnRenderFrame(FrameEventArgs e)
		{
			base.OnRenderFrame(e);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			drawScene();

			AxesHelper.Render ();

			SwapBuffers();
		}
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

			Mouse3d.RotationSpeed = 0.02f;
			Keyboard.KeyDown += new EventHandler<KeyboardKeyEventArgs>(Keyboard_KeyDown);
			Mouse.WheelChanged += new EventHandler<MouseWheelEventArgs>(Mouse_WheelChanged);
			Mouse.ButtonDown += new EventHandler<MouseButtonEventArgs>(Mouse_ButtonDown);
			Mouse.ButtonUp += new EventHandler<MouseButtonEventArgs>(Mouse_ButtonUp);
			Mouse.Move += new EventHandler<MouseMoveEventArgs>(Mouse_Move);

			GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);

			GL.Enable(EnableCap.DepthTest);
			GL.DepthFunc(DepthFunction.Less);
			GL.Enable(EnableCap.CullFace);
			GL.Enable(EnableCap.Texture2D);
			GL.FrontFace(FrontFaceDirection.Ccw);

			GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
			GL.ShadeModel(ShadingModel.Smooth);

			Console.WriteLine("\n\n*************************************");
			Console.WriteLine("GL version: " + GL.GetString (StringName.Version));
			Console.WriteLine("GL vendor: " + GL.GetString (StringName.Vendor));
			Console.WriteLine("GLSL version: " + GL.GetString (StringName.ShadingLanguageVersion));
			Console.WriteLine("*************************************\n");                     
        }
        protected override void OnUnload(EventArgs e)
        {
            //GL.DeleteTextures(1, ref objMesh.texture);
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            Interface.renderBounds = this.ClientRectangle;
            Debug.WriteLine(this.ClientRectangle.ToString());

            UpdateViewMatrix();
        }        
        #endregion

        #region Mouse Handling

        void Mouse_Move(object sender, MouseMoveEventArgs e)
        {
            go.Mouse.Position = e.Position;

            Interface.ProcessMousePosition();

            if (go.Mouse.Delta != 0)
            {
				if (go.Mouse.MiddleButton == MouseButtonStates.Pressed)
                {
                    Matrix4 m = Matrix4.CreateRotationZ(go.Mouse.Delta.X * Mouse3d.RotationSpeed);
                    m *= Matrix4.CreateFromAxisAngle(-vLookPerpendicularOnXYPlane, go.Mouse.Delta.Y * Mouse3d.RotationSpeed);

                    vEye = Vector3.Transform(vEye, Matrix4.CreateTranslation(-vEyeTarget) * m * Matrix4.CreateTranslation(vEyeTarget));
                    UpdateViewMatrix();
                }
				if (go.Mouse.RightButton == MouseButtonStates.Pressed)
                {

                    Matrix4 m = Matrix4.CreateRotationZ(go.Mouse.Delta.X * Mouse3d.RotationSpeed);
                    Matrix4 m2 = Matrix4.Rotate(vLookPerpendicularOnXYPlane, -go.Mouse.Delta.Y * Mouse3d.RotationSpeed);

                    vEyeTarget = Vector3.Transform(vEyeTarget, Matrix4.CreateTranslation(-vEye) * m * m2 * Matrix4.CreateTranslation(vEye));

                    //vLook = Vector3.Transform(vLook, m2);
                    UpdateViewMatrix();

                }
            }
        }

        void Mouse_ButtonUp(object sender, MouseButtonEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButton.Left:
					go.Mouse.LeftButton = MouseButtonStates.Released;
                    break;
                case MouseButton.Middle:
					go.Mouse.MiddleButton = MouseButtonStates.Released;
                    break;
                case MouseButton.Right:
					go.Mouse.RightButton = MouseButtonStates.Released;
                    break;
                default:
                    break;
            }
            Interface.ProcessMouseButtonUp();
        }
        void Mouse_ButtonDown(object sender, MouseButtonEventArgs e)
        {
			if (Interface.MouseIsInInterface) {
				Interface.ProcessMouseButtonDown ();
				return;
			}
            switch (e.Button)
            {
                case MouseButton.Left:
					go.Mouse.LeftButton = MouseButtonStates.Pressed;
                    break;
                case MouseButton.Middle:
					go.Mouse.MiddleButton = MouseButtonStates.Pressed;
                    break;
                case MouseButton.Right:
					go.Mouse.RightButton = MouseButtonStates.Pressed;
                    break;
                default:
                    break;
            }
        }

        void Mouse_WheelChanged(object sender, MouseWheelEventArgs e)
        {
            float speed = MoveSpeed;
            if (Keyboard[Key.ShiftLeft])
                speed *= 0.1f;
            else if (Keyboard[Key.ControlLeft])
                speed *= 20.0f;

            vLook = Vector3.NormalizeFast(vEye - vEyeTarget);
            vEye -= vLook * e.Delta * speed;
            UpdateViewMatrix();
        }
        #endregion

        #region keyboard Handling
        void Keyboard_KeyDown(object sender, KeyboardKeyEventArgs e)
        {
			switch (e.Key) {
			default:
				break;
			}            
			
            

        }
        #endregion

        #region vLookCalculations
        Vector3 vLookDirOnXYPlane
        {
            get
            {
                Vector3 v = Vector3.NormalizeFast(vEye - vEyeTarget);
                v.Z = 0;
                return v;
            }
        }
        public Vector3 vLookPerpendicularOnXYPlane
        {
            get
            {
                Vector3 vLook = Vector3.NormalizeFast(vEye - vEyeTarget);
                vLook.Z = 0;

                Vector3 vHorizDir = Vector3.Cross(vLook, Vector3.UnitZ);
                return vHorizDir;
            }
        }

        void moveCamera(Vector3 v)
        {
            vEye += v;
            vEyeTarget += v;
        }
        #endregion

        public void UpdateViewMatrix()
        {
            GL.Viewport(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height);
            projection = Matrix4.CreatePerspectiveFieldOfView(fovY, ClientRectangle.Width / (float)ClientRectangle.Height, zNear, zFar);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();

            GL.LoadMatrix(ref projection);

            modelview = Matrix4.LookAt(vEye, vEyeTarget, Vector3.UnitZ);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            GL.LoadMatrix(ref modelview);
        }
    }
}