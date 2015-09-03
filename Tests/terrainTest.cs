#define MONO_CAIRO_DEBUG_DISPOSE


using System;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

using System.Diagnostics;

//using GGL;
using go;
using System.Threading;
using GGL;


namespace terrainTest
{
	class GameWin : OpenTKGameWindow
	{
		#region FPS
		static int _fps = 0;

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

		public static int fpsMin = int.MaxValue;
		public static int fpsMax = 0;

		static void resetFps ()
		{
			fpsMin = int.MaxValue;
			fpsMax = 0;
			_fps = 0;
		}
		#endregion

		#region  scene matrix and vectors
		public static Matrix4 modelview;
		public static Matrix4 projection;
		public static int[] viewport = new int[4];

		//public static Vector3 vEye = new Vector3(150.0f, 50.0f, 1.5f);    // Camera Position
		public static Vector3 vEye = new Vector3(-1.0f, -1.0f, 1.0f);    // Camera Position
		public static Vector3 vEyeTarget;// = new Vector3(40f, 50f, 0.1f);
		public static Vector3 vLook = new Vector3(0.5f, 0.5f, -0.5f);  // Camera vLook Vector
		public static Vector3 vMouse = Vector3.Zero;

		float _zFar = 1280.0f;

		public float zFar {
			get { return _zFar; }
			set {
				_zFar = value;
			}
		}

		public float zNear = 0.001f;
		public float fovY = (float)Math.PI / 4;

		float MoveSpeed = 1.0f;
		float RotationSpeed = 0.02f;
		#endregion

		public static GameLib.EffectShader voronoiShader;
		public static GameLib.Shader mainShader;
		public static GameLib.EffectShader redShader;

		public GameWin ()
			: base(800, 600,"test")
		{}

		Container g;
		Label labFps, labFpsMin, labFpsMax, labUpdate;

		#region table
		vaoMesh plane;
		Texture tex;

		public void initPlane()
		{
			const float _width = 1f;
			const float _height = 1f;
			const float texTileX = 1f;
			const float texTileY = 1f;
			const float z = 0.0f;

			plane = new vaoMesh (0, 0, z, _width, _height, texTileX, texTileY);
			tex = new Texture ("images/marble1.png");
		}
		void drawPlane()
		{
//			redShader.Enable ();
//			redShader.ProjectionMatrix = projection;
//			redShader.ModelViewMatrix = modelview;
//			redShader.ModelMatrix = Matrix4.Identity;
			mainShader.Enable ();
			mainShader.ProjectionMatrix = projection;
			mainShader.ModelViewMatrix = modelview;
			mainShader.ModelMatrix = Matrix4.Identity;
			GL.BindTexture(TextureTarget.Texture2D, voronoiShader.Texture);
			//GL.BindTexture(TextureTarget.Texture2D, tex);
			plane.Render(PrimitiveType.TriangleStrip);

		}
		#endregion

		public void UpdateViewMatrix()
		{
			Rectangle r = this.ClientRectangle;
			GL.Viewport( r.X, r.Y, r.Width, r.Height);
			projection = Matrix4.CreatePerspectiveFieldOfView(fovY, r.Width / (float)r.Height, zNear, zFar);
			vEyeTarget = vEye + vLook;
			modelview = Matrix4.LookAt(vEye, vEyeTarget, Vector3.UnitZ);

			//GL.GetInteger(GetPName.Viewport, viewport);
			try {
				mainShader.ProjectionMatrix = projection;
				mainShader.ModelViewMatrix = modelview;
				mainShader.ModelMatrix = Matrix4.Identity;
			} catch (Exception ex) {

			}
		}
		#region vLookCalculations
		Vector3 vLookDirOnXYPlane
		{
			get
			{
				return Vector3.NormalizeFast(new Vector3 (vLook.X, vLook.Y, 0));
			}
		}
		public Vector3 vLookPerpendicularOnXYPlane
		{
			get
			{
				Vector3 vHorizDir = Vector3.Cross(Vector3.NormalizeFast(new Vector3 (vLook.X, vLook.Y, 0)), Vector3.UnitZ);
				return vHorizDir;
			}
		}

		void moveCamera(Vector3 v)
		{
			vEye += v;
			vEyeTarget += v;
		}
		#endregion

		void Mouse_Move(object sender, MouseMoveEventArgs e)
		{
			if (e.XDelta != 0 || e.YDelta != 0)
			{
				if (e.Mouse.MiddleButton == OpenTK.Input.ButtonState.Pressed) {
					Matrix4 m = Matrix4.CreateRotationX (-e.YDelta * RotationSpeed);
					vLook = Vector3.Transform (vLook, m);
					UpdateViewMatrix ();
					return;
				}
				if (e.Mouse.RightButton == ButtonState.Pressed) {

					Matrix4 m = Matrix4.CreateRotationZ (-e.XDelta * RotationSpeed);
					Matrix4 m2 = Matrix4.Rotate (vLookPerpendicularOnXYPlane, -e.YDelta * RotationSpeed);

					vLook = Vector3.Transform (vLook, m * m2);

					//vLook = Vector3.Transform(vLook, m2);
					UpdateViewMatrix ();
					return;
				}

			}

		}			
		void Mouse_WheelChanged(object sender, MouseWheelEventArgs e)
		{
			float speed = MoveSpeed;
			if (Keyboard[Key.ShiftLeft])
				speed *= 0.1f;
			else if (Keyboard[Key.ControlLeft])
				speed *= 20.0f;

			vEye += vLook * e.Delta * speed;
			//vLook.Z += e.Delta * 0.1f;
			UpdateViewMatrix();
		}

		protected override void OnResize (EventArgs e)
		{
			base.OnResize (e);
			UpdateViewMatrix();
		}
		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);
			LoadInterface("ui/fps.goml", out g);

			labFps = g.FindByName ("labFps") as Label;
			labFpsMin = g.FindByName ("labFpsMin") as Label;
			labFpsMax = g.FindByName ("labFpsMax") as Label;
			labUpdate = g.FindByName ("labUpdate") as Label;

			voronoiShader = new GameLib.EffectShader ("GGL.Shaders.GameLib.voronoi",2048,2048);
			mainShader = new GameLib.Shader ();
			redShader = new GameLib.EffectShader ("GGL.Shaders.GameLib.red");

			GL.ClearColor(0.0f, 0.0f, 0.2f, 1.0f);
			GL.Enable(EnableCap.DepthTest);
			GL.DepthFunc(DepthFunction.Less);
//			GL.Enable(EnableCap.CullFace);
//			GL.PrimitiveRestartIndex (int.MaxValue);
//			GL.Enable (EnableCap.PrimitiveRestart);
			GL.Enable (EnableCap.Blend);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

			initPlane ();

			voronoiShader.Update ();

			Mouse.WheelChanged += new EventHandler<MouseWheelEventArgs>(Mouse_WheelChanged);
			Mouse.Move += new EventHandler<MouseMoveEventArgs>(Mouse_Move);
		}
		protected override void OnRenderFrame (FrameEventArgs e)
		{
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);


			drawPlane ();

			base.OnRenderFrame (e);
			SwapBuffers ();
		}

		private int frameCpt = 0;
		protected override void OnUpdateFrame (FrameEventArgs e)
		{

			base.OnUpdateFrame (e);


			fps = (int)RenderFrequency;


			if (frameCpt > 20) {				
				resetFps ();
				frameCpt = 0;
				voronoiShader.Update ();
			}
			frameCpt++;

			UpdateViewMatrix ();
		}

		[STAThread]
		static void Main ()
		{
			Console.WriteLine ("starting example");

			using (GameWin win = new GameWin( )) {
				win.Run (30.0);
			}
		}
	}
}