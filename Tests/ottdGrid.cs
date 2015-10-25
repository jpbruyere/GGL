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
		public void NotifyValueChange(string propName, object newValue)
		{
			ValueChanged.Raise(this, new ValueChangeEventArgs (propName, newValue));
		}
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
					NotifyValueChange ("fpsMax", fpsMax);
				} else if (_fps < fpsMin) {
					fpsMin = _fps;
					NotifyValueChange ("fpsMin", fpsMin);
				}
					
				NotifyValueChange ("fps", _fps);
				NotifyValueChange ("update",
					this.updateTime.ElapsedMilliseconds.ToString () + " ms");
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
		public float zFar = 1000.0f;
		public float zNear = 1.0f;
		public float fovY = (float)Math.PI / 4;

		float eyeDist = 30;
		float eyeDistTarget = 30f;
		float MoveSpeed = 1.0f;
		float RotationSpeed = 0.02f;
		#endregion

		Vector3 selPos = Vector3.Zero;
		public Vector3 SelectionPos
		{
			get { return selPos; }
			set {
				selPos = value;
				NotifyValueChange ("SelectionPos", selPos);
			}
		}
		public Vector2 MousePos {
			get { return new Vector2 (Mouse.X, Mouse.Y); }
		}

		public static GameLib.ShadedTexture voronoiShader;
		public static GameLib.VertexDispShader gridShader;
		public static GameLib.EffectShader redShader;
		public static go.GLBackend.TexturedShader CacheRenderingShader;

		const int _width = 256;
		const int _height = 256;
		const float heightScale = 15.0f;

		vaoMesh grid;
		vaoMesh selMesh;

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

			grid = new vaoMesh (positionVboData, texVboData, null);
			grid.indices = indicesVboData;

			gridShader.DiffuseTexture = new Texture ("images/grass4.png");
		}
		void activateGridShader()
		{
			gridShader.DisplacementMap = voronoiShader.Texture;
			gridShader.Enable ();
			gridShader.MapSize = new Vector2 (_width, _height);
			gridShader.HeightScale = heightScale;
			gridShader.ProjectionMatrix = projection;
			gridShader.ModelViewMatrix = modelview;
			gridShader.ModelMatrix = Matrix4.Identity;

		}
		void drawGrid()
		{
			if (!gridCacheIsUpToDate)
				updateGridFbo ();

			renderGridCache ();
			drawHoverCase ();
		}
		void drawHoverCase()
		{
			redShader.Enable ();
			redShader.ProjectionMatrix = projection;
			redShader.ModelViewMatrix = modelview;
			redShader.ModelMatrix = Matrix4.Identity;

			GL.LineWidth (2);

			int x = (int)(selPos.X -1);
			int y = (int)(selPos.Y -1);

			if (x < 0 || y < 0)
				return;
			
			int[] sel = new int[] {
				x + y * (_width + 1),
				x + 1 + y * (_width + 1), 
				x + 1 + (y + 1) * (_width + 1),
				x + (y + 1) * (_width + 1)
			};

			Vector3[] selMeshPosition = new Vector3[] {
				grid.positions [sel [0]],
				grid.positions [sel [1]],
				grid.positions [sel [2]],
				grid.positions [sel [3]]
			};
			for (int i = 0; i < selMeshPosition.Length; i++) 
				selMeshPosition [i].Z = selPos.Z / 256f * heightScale;
			
			selMesh = new vaoMesh(selMeshPosition, 
				null, new int[] {0,1,2,3});

			selMesh.Render(PrimitiveType.LineLoop);
		}
		#region Grid Cache
		bool gridCacheIsUpToDate = false;
		QuadVAO cacheQuad;
		Matrix4 cacheProjection;
		int gridCacheTex, gridSelectionTex;
		int fboGrid;
		DrawBuffersEnum[] dbe = new DrawBuffersEnum[]
		{
			DrawBuffersEnum.ColorAttachment0 ,
			DrawBuffersEnum.ColorAttachment1};
		
		byte[] selectionMap;

		void createCache(){
			selectionMap = new byte[ClientRectangle.Width*ClientRectangle.Height*4];

			if (cacheQuad != null)
				cacheQuad.Dispose ();
			cacheQuad = new QuadVAO (0, 0, ClientRectangle.Width, ClientRectangle.Height, 0, 1, 1, -1);
			cacheProjection = Matrix4.CreateOrthographicOffCenter 
				(0, ClientRectangle.Width, 0, ClientRectangle.Height, 0, 1);
			initGridFbo ();
		}
		void renderGridCache(){
			bool depthTest = GL.GetBoolean (GetPName.DepthTest);

			GL.Disable (EnableCap.DepthTest);

			CacheRenderingShader.Enable ();
			CacheRenderingShader.ProjectionMatrix = cacheProjection;
			CacheRenderingShader.ModelViewMatrix = Matrix4.Identity;
			CacheRenderingShader.Color = new Vector4(1f,1f,1f,1f);

			GL.ActiveTexture (TextureUnit.Texture0);
			GL.BindTexture (TextureTarget.Texture2D, gridCacheTex);
			cacheQuad.Render (PrimitiveType.TriangleStrip);
			GL.BindTexture (TextureTarget.Texture2D, 0);

			if (depthTest)
				GL.Enable (EnableCap.DepthTest);
		}
		#endregion

		#region FBO
		void initGridFbo()
		{
			System.Drawing.Size cz = ClientRectangle.Size;

			gridCacheTex = new Texture (cz.Width, cz.Height);
			gridSelectionTex = new Texture (cz.Width, cz.Height);

			GL.GenFramebuffers(1, out fboGrid);

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboGrid);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
				TextureTarget.Texture2D, gridCacheTex, 0);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1,
				TextureTarget.Texture2D, gridSelectionTex, 0);

			if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
			{
				throw new Exception(GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer).ToString());
			}

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
		}
		void updateGridFbo()
		{						
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboGrid);
			GL.DrawBuffers(2, dbe);

			GL.Clear (ClearBufferMask.ColorBufferBit);
			activateGridShader ();

			grid.Render(PrimitiveType.TriangleStrip, grid.indices);

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
			GL.DrawBuffer(DrawBufferMode.Back);
			getSelectionTextureData ();
			
			gridCacheIsUpToDate = true;
		}
		void getSelectionTextureData()
		{
			GL.BindTexture (TextureTarget.Texture2D, gridSelectionTex);

			GL.GetTexImage (TextureTarget.Texture2D, 0, 
				PixelFormat.Rgba, PixelType.UnsignedByte, selectionMap);

			GL.BindTexture (TextureTarget.Texture2D, 0);
		}
		#endregion


		public void UpdateViewMatrix()
		{
			Rectangle r = this.ClientRectangle;
			GL.Viewport( r.X, r.Y, r.Width, r.Height);
			projection = Matrix4.CreatePerspectiveFieldOfView (fovY, r.Width / (float)r.Height, zNear, zFar);
			Vector3 vEye = vEyeTarget + vLook * eyeDist;
			modelview = Matrix4.LookAt(vEye, vEyeTarget, Vector3.UnitZ);
			GL.GetInteger(GetPName.Viewport, viewport);

			try {
				gridShader.ProjectionMatrix = projection;
				gridShader.ModelViewMatrix = modelview;
				gridShader.ModelMatrix = Matrix4.Identity;
			} catch (Exception ex) {
				Debug.WriteLine ("UpdateViewMatrices: failed to set shader matrices: " + ex.Message);
			}
			gridCacheIsUpToDate = false;
		}			

		#region Mouse
		void Mouse_Move(object sender, MouseMoveEventArgs e)
		{			
			if (e.XDelta != 0 || e.YDelta != 0)
			{
				NotifyValueChange("MousePos", new Vector2 (Mouse.X, Mouse.Y));
				int selPtr = (e.X * 4 + (ClientRectangle.Height - e.Y) * ClientRectangle.Width * 4);
				SelectionPos = new Vector3 (selectionMap [selPtr], 
					selectionMap [selPtr + 1], selectionMap [selPtr + 2]);

				if (e.Mouse.MiddleButton == OpenTK.Input.ButtonState.Pressed) {					
					Vector3 v = new Vector3 (
						            Vector2.Normalize (vLook.Xy.PerpendicularLeft));
					Vector3 tmp = Vector3.Transform (vLook, 
						Matrix4.CreateRotationZ (-e.XDelta * RotationSpeed) *
						Matrix4.CreateFromAxisAngle (v, -e.YDelta * RotationSpeed));
					tmp.Normalize();
					if (tmp.Z <= 0f)
						return;
					vLook = tmp;
					UpdateViewMatrix ();
					return;
				}
				if (e.Mouse.LeftButton == ButtonState.Pressed) {
					
				}
				if (e.Mouse.RightButton == ButtonState.Pressed) {
					Vector3 vH = new Vector3(Vector2.Normalize(vLook.Xy.PerpendicularLeft) * e.XDelta * MoveSpeed);
					Vector3 vV = new Vector3(Vector2.Normalize(vLook.Xy) * e.YDelta * MoveSpeed);
					vEyeTarget -= vH + vV;
						
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

			eyeDistTarget -= e.Delta * speed;
//			if (eyeDistTarget < zNear+10)
//				eyeDistTarget = zNear+10;
//			else if (eyeDistTarget > zFar-100)
//				eyeDistTarget = zFar-100;
			Animation.StartAnimation(new Animation<float> (this, "EyeDist", eyeDistTarget, (eyeDistTarget - eyeDist) * 0.2f));
		}
		#endregion

		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);
			LoadInterface("ui/fps.goml").DataSource = this;



			voronoiShader = new GameLib.ShadedTexture ("GGL.Shaders.GameLib.voronoi",512,512);
			//mainShader = new GameLib.VertexDispShader ("GGL.Shaders.GameLib.VertDispInstanced.vert","GGL.Shaders.GameLib.VertDispNormFilt.frag");
			//mainShader = new GameLib.VertexDispShader ("GGL.Shaders.GameLib.VertDispInstancedSingleLight.vert","GGL.Shaders.GameLib.VertDispSingleLight.frag");
			gridShader = new GameLib.VertexDispShader ("Tests.Shaders.VertDisp.vert", "Tests.Shaders.Grid.frag");
			redShader = new GameLib.EffectShader ("GGL.Shaders.GameLib.red");
			CacheRenderingShader = new go.GLBackend.TexturedShader();

			GL.ClearColor(0.0f, 0.0f, 0.2f, 1.0f);
			GL.Enable(EnableCap.DepthTest);
			GL.DepthFunc(DepthFunction.Less);
			//			GL.Enable(EnableCap.CullFace);
			GL.PrimitiveRestartIndex (int.MaxValue);
			GL.Enable (EnableCap.PrimitiveRestart);

			GL.Enable (EnableCap.Blend);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

			initGrid ();

			createCache ();

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

			Animation.ProcessAnimations ();
		}

		protected override void OnResize (EventArgs e)
		{
			base.OnResize (e);
			UpdateViewMatrix();
		}
		public override void GLClear ()
		{
			GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
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