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

		//public static Vector3 vEye = new Vector3(150.0f, 50.0f, 1.5f);    // Camera Position
		public static Vector3 vEye = new Vector3(-20.0f, -20.0f, 30.0f);    // Camera Position
		public static Vector3 vEyeTarget;// = new Vector3(40f, 50f, 0.1f);
		public static Vector3 vLook = new Vector3(0.5f, 0.5f, -0.2f);  // Camera vLook Vector
		public static Vector3 vMouse = Vector3.Zero;

		float _zFar = 200.0f;

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

		public static GameLib.ShadedTexture voronoiShader;
		public static GameLib.VertexDispShader mainShader;
		public static GameLib.EffectShader redShader;

		public GameWin ()
			: base(800, 600,"test")
		{}

		Container g;

		#region table
		vaoMesh plane;
		Texture tex;

		int IdxPrimitiveRestart = int.MaxValue;

		int vaoHandle,
		positionVboHandle,
		normalsVboHandle,
		texVboHandle,
		eboHandle;

		Vector3[] positionVboData;
		public int[] indicesVboData;
		Vector2[] texVboData;

		const int _width = 128;
		const int _height = 128;

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

			CreateVBOs ();
			CreateVAOs ();

			mainShader.DiffuseTexture = new Texture ("images/grass4.png");
		}

		void drawGrid()
		{
//			redShader.Enable ();
//			redShader.ProjectionMatrix = projection;
//			redShader.ModelViewMatrix = modelview;
//			redShader.ModelMatrix = Matrix4.Identity;
			mainShader.DisplacementMap = voronoiShader.Texture;
			mainShader.Enable ();
			mainShader.MapSize = new Vector2 (_width, _height);
			mainShader.ProjectionMatrix = projection;
			mainShader.ModelViewMatrix = modelview;
			mainShader.ModelMatrix = Matrix4.Identity;

			GL.BindVertexArray(vaoHandle);

			GL.DrawElementsInstanced(PrimitiveType.TriangleStrip, indicesVboData.Length,
				DrawElementsType.UnsignedInt, IntPtr.Zero,16);	
			//			GL.DrawElements(PrimitiveType.TriangleStrip, indicesVboData.Length,
			//				DrawElementsType.UnsignedInt, IntPtr.Zero);	

			GL.BindVertexArray (0);

		}

		void deleteVAOs()
		{
			GL.DeleteBuffer (positionVboHandle);
			GL.DeleteBuffer (texVboHandle);
			GL.DeleteBuffer (eboHandle);
			GL.DeleteVertexArray (vaoHandle);
		}

		void CreateVBOs()
		{
			positionVboHandle = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
			GL.BufferData<Vector3>(BufferTarget.ArrayBuffer,
				new IntPtr(positionVboData.Length * Vector3.SizeInBytes),
				positionVboData, BufferUsageHint.StaticDraw);

			texVboHandle = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, texVboHandle);
			GL.BufferData<Vector2>(BufferTarget.ArrayBuffer,
				new IntPtr(texVboData.Length * Vector2.SizeInBytes),
				texVboData, BufferUsageHint.StaticDraw);
			//
			eboHandle = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, eboHandle);
			GL.BufferData(BufferTarget.ElementArrayBuffer,
				new IntPtr(sizeof(uint) * indicesVboData.Length),
				indicesVboData, BufferUsageHint.StaticDraw);

			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
		}

		void CreateVAOs()
		{
			vaoHandle = GL.GenVertexArray();
			GL.BindVertexArray(vaoHandle);

			GL.EnableVertexAttribArray(0);
			GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
			GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, true, Vector3.SizeInBytes, 0);

			GL.EnableVertexAttribArray(1);
			GL.BindBuffer(BufferTarget.ArrayBuffer, texVboHandle);
			GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, true, Vector2.SizeInBytes, 0);

			GL.BindBuffer(BufferTarget.ElementArrayBuffer, eboHandle);

			GL.BindVertexArray(0);
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


			voronoiShader = new GameLib.ShadedTexture ("GGL.Shaders.GameLib.voronoi",2048,2048);
			//mainShader = new GameLib.VertexDispShader ("GGL.Shaders.GameLib.VertDispInstanced.vert","GGL.Shaders.GameLib.VertDispNormFilt.frag");
			mainShader = new GameLib.VertexDispShader ("GGL.Shaders.GameLib.VertDispInstancedSingleLight.vert","GGL.Shaders.GameLib.VertDispSingleLight.frag");
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

			Mouse.WheelChanged += new EventHandler<MouseWheelEventArgs>(Mouse_WheelChanged);
			Mouse.Move += new EventHandler<MouseMoveEventArgs>(Mouse_Move);
		}
		protected override void OnRenderFrame (FrameEventArgs e)
		{
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

				drawGrid ();


			base.OnRenderFrame (e);
			SwapBuffers ();
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