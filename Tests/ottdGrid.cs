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

		public Vector4 vLight = new Vector4 (-1, -1, -1, 0);
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
		const int _hmSize = 256;
		const int _splatingSize = 2048;
		const float heightScale = 100.0f;

		vaoMesh grid;
		vaoMesh selMesh;

		int IdxPrimitiveRestart = int.MaxValue;

		Vector3[] positionVboData;
		public int[] indicesVboData;
		Vector2[] texVboData;


		public void initGrid()
		{
			const float z = 0.0f;

			positionVboData = new Vector3[_width * _height];
			texVboData = new Vector2[_width * _height];
			indicesVboData = new int[(_width * 2 + 1) * _height];

			for (int y = 0; y < _height; y++) {
				for (int x = 0; x < _width; x++) {				
					positionVboData [_width * y + x] = new Vector3 (x, y, z);
					texVboData [_width * y + x] = new Vector2 ((float)x, (float)y);

					if (y < _height-1) {
						indicesVboData [(_width * 2 + 1) * y + x*2] = _width * y + x;
						indicesVboData [(_width * 2 + 1) * y + x*2 + 1] = _width * (y+1) + x;
					}

					if (x == _width-1) {
						indicesVboData [(_width * 2 + 1) * y + x*2 + 2] = IdxPrimitiveRestart;
					}
				}
			}

			grid = new vaoMesh (positionVboData, texVboData, null);
			grid.indices = indicesVboData;

			gridShader.DiffuseTexture = new TextureArray (
				"#Tests.images.grass_green_d.jpg",
				"#Tests.images.grass_ground_d.jpg",
				"#Tests.images.grass_ground2y_d.jpg",
				"#Tests.images.grass_mix_ylw_d.jpg",
				"#Tests.images.grass_mix_d.jpg",
				"#Tests.images.grass_autumn_orn_d.jpg",
				"#Tests.images.grass_autumn_red_d.jpg",
				"#Tests.images.grass_rocky_d.jpg",
				"#Tests.images.ground_cracks2v_d.jpg",
				"#Tests.images.ground_crackedv_d.jpg",
				"#Tests.images.ground_cracks2y_d.jpg",
				"#Tests.images.ground_crackedo_d.jpg"
			);
			gridShader.SplatTexture = new Texture (_splatingSize, _splatingSize);
			getSplatData ();
			//2048
			//"#Tests.images.grass_green2y_d.jpg",
			GL.BindTexture (TextureTarget.Texture2D, gridShader.SplatTexture);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
			GL.BindTexture (TextureTarget.Texture2D, 0);


		}

		void activateGridShader()
		{
			gridShader.DisplacementMap = voronoiShader.Texture;
			gridShader.Enable ();
			gridShader.LightPos = vLight;
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

			int x = (int)(selPos.X);
			int y = (int)(selPos.Y);

			if (x < 0 || y < 0 || hmData == null)
				return;
			
			int[] sel = new int[] {
				x + y * _width ,
				x + 1 + y * _width, 
				x + 1 + (y + 1) * _width,
				x + (y + 1) * _width
			};

			Vector3[] selMeshPosition = new Vector3[] {
				grid.positions [sel [0]],
				grid.positions [sel [1]],
				grid.positions [sel [2]],
				grid.positions [sel [3]]
			};
			//for (int i = 0; i < selMeshPosition.Length; i++) 
			selMeshPosition [0].Z = hmData[(y*_hmSize + x)*4 + 1] / 256f * heightScale;
			selMeshPosition [1].Z = hmData[(y*_hmSize + x)*4 + 5] / 256f * heightScale;
			selMeshPosition [2].Z = hmData[((y+1)*_hmSize + x)*4 + 5] / 256f * heightScale;
			selMeshPosition [3].Z = hmData[((y+1)*_hmSize + x)*4 + 1] / 256f * heightScale;
			
			selMesh = new vaoMesh(selMeshPosition, 
				null, new int[] {0,1,2,3});

			selMesh.Render(PrimitiveType.LineLoop);
		}

		byte[] hmData;
		byte[] splatData;

		void getSplatData()
		{
			splatData = new byte[_splatingSize*_splatingSize*4];
			GL.BindTexture (TextureTarget.Texture2D, gridShader.SplatTexture);

			GL.GetTexImage (TextureTarget.Texture2D, 0, 
				PixelFormat.Rgba, PixelType.UnsignedByte, splatData);

			GL.BindTexture (TextureTarget.Texture2D, 0);
		}
		void updateSplat()
		{
			GL.BindTexture (TextureTarget.Texture2D, gridShader.SplatTexture);

			GL.TexSubImage2D (TextureTarget.Texture2D,
				0, 0, 0, _splatingSize, _splatingSize, PixelFormat.Bgra, PixelType.UnsignedByte, splatData);

			GL.BindTexture (TextureTarget.Texture2D, 0);
			gridCacheIsUpToDate = false;
			splatTextureIsUpToDate = true;
		}
		void getHeightMapData()
		{
			hmData = new byte[_hmSize*_hmSize*4];
			GL.BindTexture (TextureTarget.Texture2D, voronoiShader.Texture);

			GL.GetTexImage (TextureTarget.Texture2D, 0, 
				PixelFormat.Rgba, PixelType.UnsignedByte, hmData);

			GL.BindTexture (TextureTarget.Texture2D, 0);
		}
		void updateHeightMap()
		{
			GL.BindTexture (TextureTarget.Texture2D, voronoiShader.Texture);

			GL.TexSubImage2D (TextureTarget.Texture2D,
				0, 0, 0, _hmSize, _hmSize, PixelFormat.Bgra, PixelType.UnsignedByte, hmData);

			GL.BindTexture (TextureTarget.Texture2D, 0);
			gridCacheIsUpToDate = false;
			heightMapIsUpToDate = true;
		}

		#region Grid Cache
		bool gridCacheIsUpToDate = false;
		bool heightMapIsUpToDate = true,
			splatTextureIsUpToDate = true;
		QuadVAO cacheQuad;
		Matrix4 cacheProjection;
		int gridCacheTex, gridSelectionTex;
		int fboGrid, depthRenderbuffer;
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
			GL.BindTexture (TextureTarget.Texture2D, gridSelectionTex);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
			GL.BindTexture (TextureTarget.Texture2D, 0);

			// Create Depth Renderbuffer
			GL.GenRenderbuffers( 1, out depthRenderbuffer );
			GL.BindRenderbuffer( RenderbufferTarget.Renderbuffer, depthRenderbuffer );
			GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, (RenderbufferStorage)All.DepthComponent32, cz.Width, cz.Height);

			GL.GenFramebuffers(1, out fboGrid);

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboGrid);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
				TextureTarget.Texture2D, gridCacheTex, 0);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1,
				TextureTarget.Texture2D, gridSelectionTex, 0);
			GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, depthRenderbuffer );


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

			GL.Clear (ClearBufferMask.ColorBufferBit|ClearBufferMask.DepthBufferBit);
			activateGridShader ();

			GL.Disable (EnableCap.AlphaTest);
			GL.Disable (EnableCap.Blend);

			grid.Render(PrimitiveType.TriangleStrip, grid.indices);

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
			GL.DrawBuffer(DrawBufferMode.Back);
			getSelectionTextureData ();

			GL.Enable (EnableCap.AlphaTest);
			GL.Enable (EnableCap.Blend);

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
//				SelectionPos = new Vector3 (selectionMap [selPtr], 
//					selectionMap [selPtr + 1], selectionMap [selPtr + 2]);
				SelectionPos = new Vector3 (
					(float)selectionMap [selPtr] + (float)selectionMap [selPtr + 1] / 255f, 
					(float)selectionMap [selPtr + 2] + (float)selectionMap [selPtr + 3] / 255f, 0f);

				UpdatePtrSplat ();

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

		int ptrSplat = 0;
		public int PtrSplat{ get { return ptrSplat; } }
		public void UpdatePtrSplat(){
			int splatXDisp = (int)Math.Floor((SelectionPos.X - Math.Truncate (SelectionPos.X)) * 4.0f);
			int splatyDisp = (int)Math.Floor((SelectionPos.Y - Math.Truncate (SelectionPos.Y)) * 4.0f);
			//int ptrSplat = (int)((SelectionPos.X + (int)SelectionPos.Y * (float)_splatingSize) * 16f);
			int xDisp = (int)SelectionPos.X * 16 + splatXDisp * 4;
			int yDisp = (int)SelectionPos.Y * _splatingSize * 16 + splatyDisp * _splatingSize * 4;
			ptrSplat = xDisp+yDisp;
			NotifyValueChange ("PtrSplat", ptrSplat);				
		}

		protected override void OnKeyDown (KeyboardKeyEventArgs e)
		{
			int ptrHM = (int)(SelectionPos.X + (int)SelectionPos.Y * _hmSize) * 4 ;
			int ptrHM2 = (int)(SelectionPos.X + (SelectionPos.Y + 1) * _hmSize) * 4;

			base.OnKeyDown (e);
			switch (e.Key) {
			case Key.Space:				
				byte up = 1;

				hmData [ptrHM+1] += up;
//				hmData [ptrHM+5] += up;
//				hmData [ptrHM2+1] += up;
//				hmData [ptrHM2+5] += up;
				heightMapIsUpToDate = false;
				break;
			case Key.Keypad2:
				byte nh = 
					Math.Min(
						Math.Min(
							Math.Min(hmData [ptrHM+1],
								hmData [ptrHM+5]),
							hmData [ptrHM2+1]),
						hmData [ptrHM2+5]);
				hmData [ptrHM+1] = nh;
				hmData [ptrHM+5] = nh;
				hmData [ptrHM2+1] = nh;
				hmData [ptrHM2+5] = nh;
				heightMapIsUpToDate = false;
				break;
			case Key.Delete:				
				hmData = new byte[_hmSize * _hmSize * 4];
				heightMapIsUpToDate = false;
				break;
			case Key.Keypad0:
				splatData = new byte[_splatingSize * _splatingSize * 4];
				splatTextureIsUpToDate = false;
				break;
			case Key.Keypad7:
				splatData[ptrSplat] += 10;
				splatTextureIsUpToDate = false;
				break;
			case Key.Keypad8:
				splatData[ptrSplat+1] += 1;
				splatTextureIsUpToDate = false;
				break;
			case Key.Keypad9:
				splatData[ptrSplat+2] += 1;
				splatTextureIsUpToDate = false;
				break;
			case Key.Keypad4:
				splatData[ptrSplat] -= 10;
				splatTextureIsUpToDate = false;
				break;
			case Key.Keypad5:
				splatData[ptrSplat+1] -= 1;
				splatTextureIsUpToDate = false;
				break;
			case Key.Keypad6:
				splatData[ptrSplat+2] -= 1;
				splatTextureIsUpToDate = false;
				break;
			case Key.G:
				hmData [ptrHM] += 1;
//				hmData [ptrHM + 4] += 1;
//				hmData [ptrHM2] += 1;
//				hmData [ptrHM2 + 4] += 1;
				heightMapIsUpToDate = false;
				break;
			default:
				break;
			}

		}

		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);
			LoadInterface("#Tests.ui.fps.goml").DataSource = this;
			LoadInterface("#Tests.ui.menu.goml").DataSource = this;



			voronoiShader = new GameLib.ShadedTexture ("GGL.Shaders.GameLib.voronoi",_hmSize, _hmSize);
			GL.BindTexture (TextureTarget.Texture2D, voronoiShader.Texture);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
			GL.BindTexture (TextureTarget.Texture2D, 0);
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
			getHeightMapData ();
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


			if (Keyboard [Key.ShiftLeft]) {
				float MoveSpeed = 1f;
				//light movment
				if (Keyboard [Key.Up])
					vLight.X -= MoveSpeed;
				else if (Keyboard [Key.Down])
					vLight.X += MoveSpeed;
				else if (Keyboard [Key.Left])
					vLight.Y -= MoveSpeed;
				else if (Keyboard [Key.Right])
					vLight.Y += MoveSpeed;
				else if (Keyboard [Key.PageUp])
					vLight.Z += MoveSpeed;
				else if (Keyboard [Key.PageDown])
					vLight.Z -= MoveSpeed;
				gridCacheIsUpToDate = false;
				//GL.Light (LightName.Light0, LightParameter.Position, vLight);
			}
			if (!splatTextureIsUpToDate)
				updateSplat ();
			if (heightMapIsUpToDate)
				return;
			
			updateHeightMap ();
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