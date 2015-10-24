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


namespace ottdGridTest
{
	class GameWin : OpenTKGameWindow, IValueChange
	{
		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		#endregion

		#region FPS
		int _fps = 0;

		public int fps {
			get { return _fps; }
			set {
				if (_fps == value)
					return;

				_fps = value;

				if (_fps > fpsMax) {
					fpsMax = _fps;
					ValueChanged.Raise(this, new ValueChangeEventArgs ("fpsMax", fpsMax));
				} else if (_fps < fpsMin) {
					fpsMin = _fps;
					ValueChanged.Raise(this, new ValueChangeEventArgs ("fpsMin", fpsMin));
				}
					
				ValueChanged.Raise(this, new ValueChangeEventArgs ("fps", _fps));
				ValueChanged.Raise (this, new ValueChangeEventArgs ("update",
					this.updateTime.ElapsedMilliseconds.ToString () + " ms"));
			}
		}

		public int fpsMin = int.MaxValue;
		public int fpsMax = 0;
		public string update = "";

		void resetFps ()
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

		public float EyeDist { 
			get { return eyeDist; } 
			set { 
				eyeDist = value; 
				UpdateViewMatrix ();
			} 
		}
		public Vector3 vEyeTarget = new Vector3(32, 32, 0f);
		public Vector3 vLook = Vector3.Normalize(new Vector3(-1f, -1f, 1f));  // Camera vLook Vector
		public float zFar = 300.0f;
		public float zNear = 1.0f;
		public float fovY = (float)Math.PI / 4;

		float eyeDist = 50;
		float eyeDistTarget = 200f;
		float MoveSpeed = 1.0f;
		float RotationSpeed = 0.02f;
		#endregion

		public static GameLib.ShadedTexture voronoiShader;
		public static GameLib.VertexDispShader mainShader;
		public static GameLib.EffectShader redShader;

		const int _width = 64;
		const int _height = 64;

		vaoMesh grid;

		int IdxPrimitiveRestart = int.MaxValue;

		Vector3[] positionVboData;
		public int[] indicesVboData;
		Vector2[] texVboData;


		public void initGrid()
		{
			const float z = 0.0f;

			positionVboData = new Vector3[(_width + 1) * (_height + 1)];
			texVboData = new Vector2[(_width + 1) * (_height + 1)];
			indicesVboData = new int[((_width + 1) * 2 + 1) * (_height + 1)];

			for (int y = 0; y < _height + 1; y++) {
				for (int x = 0; x < _width + 1; x++) {				
					positionVboData [(_width + 1) * y + x] = new Vector3 (x, y, z);
					texVboData [(_width + 1) * y + x] = new Vector2 ((float)x, (float)y);

					if (y < _height) {
						indicesVboData [((_width + 1) * 2 + 1) * y + x*2] = (_width + 1) * (y ) + x;
						indicesVboData [((_width + 1) * 2 + 1) * y + x*2 + 1] = (_width + 1) * (y+1 ) + x;
					}

					if (x == _width) {
						indicesVboData [((_width + 1) * 2 + 1) * y + x*2 + 2] = IdxPrimitiveRestart;
					}
				}
			}

			grid = new vaoMesh (positionVboData, texVboData, indicesVboData);

			mainShader.DiffuseTexture = new Texture ("images/grass4.png");
		}
		void activateGridShader()
		{
			mainShader.DisplacementMap = voronoiShader.Texture;
			mainShader.Enable ();
			mainShader.MapSize = new Vector2 (_width, _height);
			mainShader.ProjectionMatrix = projection;
			mainShader.ModelViewMatrix = modelview;
			mainShader.ModelMatrix = Matrix4.Identity;

		}
		void drawGrid()
		{
			activateGridShader ();
			grid.Render(PrimitiveType.TriangleStrip);
		}

		public void UpdateViewMatrix()
		{
			Rectangle r = this.ClientRectangle;
			GL.Viewport( r.X, r.Y, r.Width, r.Height);
			projection = Matrix4.CreatePerspectiveFieldOfView (fovY, r.Width / (float)r.Height, zNear, zFar);
			Vector3 vEye = vEyeTarget + vLook * eyeDist;
			modelview = Matrix4.LookAt(vEye, vEyeTarget, Vector3.UnitZ);
			GL.GetInteger(GetPName.Viewport, viewport);

			try {
				mainShader.ProjectionMatrix = projection;
				mainShader.ModelViewMatrix = modelview;
				mainShader.ModelMatrix = Matrix4.Identity;
			} catch (Exception ex) {
				Debug.WriteLine ("UpdateViewMatrices: failed to set shader matrices: " + ex.Message);
			}
		}

		#region Mouse
		void Mouse_Move(object sender, MouseMoveEventArgs e)
		{
			if (e.XDelta != 0 || e.YDelta != 0)
			{
				if (e.Mouse.MiddleButton == OpenTK.Input.ButtonState.Pressed) {
					Vector3 tmp = Vector3.Transform (vLook, 
						Matrix4.CreateRotationX (-e.YDelta * RotationSpeed));
					tmp.Normalize();
					if (tmp.Y >= 0f || tmp.Z <= 0f)
						return;
					vLook = tmp;
					UpdateViewMatrix ();
					return;
				}
				if (e.Mouse.LeftButton == ButtonState.Pressed) {
					
				}
				if (e.Mouse.RightButton == ButtonState.Pressed) {
					Matrix4 m = Matrix4.CreateTranslation (-e.XDelta, e.YDelta, 0);
					vEyeTarget = Vector3.Transform (vEyeTarget, m);
					UpdateViewMatrix();
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

			EyeDist -= e.Delta * speed;
//			if (eyeDistTarget < zNear+10)
//				eyeDistTarget = zNear+10;
//			else if (eyeDistTarget > zFar-100)
//				eyeDistTarget = zFar-100;
			//Animation.StartAnimation(new Animation<float> (this, "EyeDist", eyeDistTarget, (eyeDistTarget - eyeDist) * 0.2f));
		}
		#endregion

		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);
			LoadInterface("ui/fps.goml").DataSource = this;



			voronoiShader = new GameLib.ShadedTexture ("GGL.Shaders.GameLib.voronoi",512,512);
			//mainShader = new GameLib.VertexDispShader ("GGL.Shaders.GameLib.VertDispInstanced.vert","GGL.Shaders.GameLib.VertDispNormFilt.frag");
			//mainShader = new GameLib.VertexDispShader ("GGL.Shaders.GameLib.VertDispInstancedSingleLight.vert","GGL.Shaders.GameLib.VertDispSingleLight.frag");
			mainShader = new GameLib.VertexDispShader ("GGL.Shaders.GameLib.VertDisp.vert","GGL.Shaders.GameLib.Texture.frag");
			redShader = new GameLib.EffectShader ("GGL.Shaders.GameLib.red");

			GL.ClearColor(0.0f, 0.0f, 0.2f, 1.0f);
			GL.Enable(EnableCap.DepthTest);
			GL.DepthFunc(DepthFunction.Less);
			//			GL.Enable(EnableCap.CullFace);
			GL.PrimitiveRestartIndex (int.MaxValue);
			GL.Enable (EnableCap.PrimitiveRestart);

			GL.Enable (EnableCap.Blend);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

			initGrid ();

			voronoiShader.Update ();

			this.MouseWheelChanged += new EventHandler<MouseWheelEventArgs>(Mouse_WheelChanged);
			this.MouseMove += new EventHandler<MouseMoveEventArgs>(Mouse_Move);
		}
		private int frameCpt = 0;
		protected override void OnUpdateFrame (FrameEventArgs e)
		{


			base.OnUpdateFrame (e);

			fps = (int)RenderFrequency;
			if (frameCpt > 200) {
				resetFps ();
				frameCpt = 0;

			}
			frameCpt++;

			UpdateViewMatrix ();
		}

		protected override void OnResize (EventArgs e)
		{
			base.OnResize (e);
			UpdateViewMatrix();
		}
		public override void GLClear ()
		{
			GL.ClearColor(0.1f, 0.1f, 0.3f, 1.0f);
			GL.Clear (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
		}
		public override void OnRender (FrameEventArgs e)
		{
			drawGrid ();
		}

		#region Main and CTOR
		[STAThread]
		static void Main ()
		{
			Console.WriteLine ("starting example");

			using (GameWin win = new GameWin( )) {
				win.Run (30.0);
			}
		}
		public GameWin ()
			: base(1024, 800,"test")
		{}
		#endregion
	}
}