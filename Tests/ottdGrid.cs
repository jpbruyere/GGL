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


namespace Ottd3D
{
	class Ottd3DWindow : OpenTKGameWindow, IValueChange
	{
		public enum GameState
		{
			Playing,
			GroundLeveling,
			GroundTexturing
		}

		const int _gridSize = 256;
		const int _hmSize = 256;
		const int _splatingSize = 2048;
		const int _circleTexSize = 1024;
		const float heightScale = 200.0f;

		public GameState CurrentState = GameState.Playing;
		public Vector4 splatBrush = new Vector4(0f, 0f, 10f/255f, 1f);
		public float splatBrushRadius = 0.01f;

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
		public float zFar = 512.0f;
		public float zNear = 0.1f;
		public float fovY = (float)Math.PI / 4;

		float eyeDist = 30;
		float eyeDistTarget = 30f;
		float MoveSpeed = 0.01f;
		float ZoomSpeed = 0.2f;
		float RotationSpeed = 0.01f;

		public Vector4 vLight = new Vector4 (-1, -1, -1, 0);
		#endregion

		Vector3 selPos = Vector3.Zero;
		public Vector3 SelectionPos
		{
			get { return selPos; }
			set {
				selPos = value;
				selPos.Z = hmData[((int)selPos.Y * _hmSize + (int)selPos.X) * 4 + 1] / 256f * heightScale;
				updateSelMesh ();
				NotifyValueChange ("SelectionPos", selPos);
			}
		}
		public Vector2 MousePos {
			get { return new Vector2 (Mouse.X, Mouse.Y); }
		}
		void updateSelMesh(){
			switch (CurrentState) {
			case GameState.GroundLeveling:
				selMesh = new vaoMesh ((int)Math.Round (selPos.X), (int)Math.Round (selPos.Y), selPos.Z, 20.0f, 20.0f);
				break;
			case GameState.GroundTexturing:
			case GameState.Playing:
				selMesh = new vaoMesh (selPos.X, selPos.Y, selPos.Z, 20.0f, 20.0f);
				break;
			default:
				break;
			}
		}

		#region Shaders
		public static GameLib.ShadedTexture hmGenerator;
		public static BrushShader splattingBrushShader;
		public static CircleShader circleShader;
		public static GameLib.VertexDispShader gridShader;
		public static GameLib.Shader simpleTexturedShader;
		public static go.GLBackend.TexturedShader CacheRenderingShader;

		void setTexFilterNeareast(int _tex)
		{
			GL.BindTexture (TextureTarget.Texture2D, _tex);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
			GL.BindTexture (TextureTarget.Texture2D, 0);
		}
		void initShaders()
		{
			circleShader = new CircleShader ("Ottd3D.Shaders.circle",_circleTexSize, _circleTexSize);
			circleShader.Color = Color.White;
			circleShader.Radius = 0.01f;

			splattingBrushShader = new BrushShader ("Ottd3D.Shaders.brush", _splatingSize, _splatingSize);
			setTexFilterNeareast (splattingBrushShader.OutputTex);
			setTexFilterNeareast (splattingBrushShader.InputTex);

			gridShader = new GameLib.VertexDispShader ("Ottd3D.Shaders.VertDisp.vert", "Ottd3D.Shaders.Grid.frag");

			simpleTexturedShader = new GameLib.Shader ();
			CacheRenderingShader = new go.GLBackend.TexturedShader();			

			hmGenerator = new GameLib.ShadedTexture ("GGL.Shaders.GameLib.voronoi",_hmSize, _hmSize);
			setTexFilterNeareast(hmGenerator.OutputTex);

			circleShader.Update ();

			hmGenerator.Update ();

			gridShader.DisplacementMap = hmGenerator.OutputTex;
			gridShader.LightPos = vLight;
			gridShader.MapSize = new Vector2 (_gridSize, _gridSize);
			gridShader.HeightScale = heightScale;

			splattingBrushShader.Clear ();
			gridShader.SplatTexture = splattingBrushShader.OutputTex;
		}

		void updateShadersMatrices(){
			gridShader.ProjectionMatrix = projection;
			gridShader.ModelViewMatrix = modelview;
			gridShader.ModelMatrix = Matrix4.Identity;

			simpleTexturedShader.ProjectionMatrix = projection;
			simpleTexturedShader.ModelViewMatrix = modelview;
			simpleTexturedShader.ModelMatrix = Matrix4.Identity;
		}

		#endregion


		string[] groundTextures = new string[]
		{
			"#Ottd3D.images.grass.jpg",
			"#Ottd3D.images.grass_green_d.jpg",
			"#Ottd3D.images.grass_ground_d.jpg",
			"#Ottd3D.images.grass_ground2y_d.jpg",
			"#Ottd3D.images.grass_mix_ylw_d.jpg",
			"#Ottd3D.images.grass_mix_d.jpg",
			"#Ottd3D.images.grass_autumn_orn_d.jpg",
			"#Ottd3D.images.grass_autumn_red_d.jpg",
			"#Ottd3D.images.grass_rocky_d.jpg",
			"#Ottd3D.images.ground_cracks2v_d.jpg",
			"#Ottd3D.images.ground_crackedv_d.jpg",
			"#Ottd3D.images.ground_cracks2y_d.jpg",
			"#Ottd3D.images.ground_crackedo_d.jpg"			
		};

		public string[] GroundTextures { get { return groundTextures; }}

		vaoMesh grid;
		vaoMesh selMesh;

		public void initGrid()
		{
			const float z = 0.0f;
			const int IdxPrimitiveRestart = int.MaxValue;

			Vector3[] positionVboData;
			int[] indicesVboData;
			Vector2[] texVboData;

			positionVboData = new Vector3[_gridSize * _gridSize];
			texVboData = new Vector2[_gridSize * _gridSize];
			indicesVboData = new int[(_gridSize * 2 + 1) * _gridSize];

			for (int y = 0; y < _gridSize; y++) {
				for (int x = 0; x < _gridSize; x++) {				
					positionVboData [_gridSize * y + x] = new Vector3 (x, y, z);
					texVboData [_gridSize * y + x] = new Vector2 ((float)x*0.5f, (float)y*0.5f);

					if (y < _gridSize-1) {
						indicesVboData [(_gridSize * 2 + 1) * y + x*2] = _gridSize * y + x;
						indicesVboData [(_gridSize * 2 + 1) * y + x*2 + 1] = _gridSize * (y+1) + x;
					}

					if (x == _gridSize-1) {
						indicesVboData [(_gridSize * 2 + 1) * y + x*2 + 2] = IdxPrimitiveRestart;
					}
				}
			}

			grid = new vaoMesh (positionVboData, texVboData, null);
			grid.indices = indicesVboData;

			gridShader.DiffuseTexture = new TextureArray (groundTextures);
		
		}
		void drawGrid()
		{
			if (!gridCacheIsUpToDate)
				updateGridFbo ();

			renderGridCache ();
		}
		void drawHoverCase()
		{
			if (selMesh == null)
				return;
			
			simpleTexturedShader.Enable ();
//			GL.LineWidth (2);
//

//
//			if (x < 0 || y < 0 || hmData == null)
//				return;
//			
//			int[] sel = new int[] {
//				x + y * _width ,
//				x + 1 + y * _width, 
//				x + 1 + (y + 1) * _width,
//				x + (y + 1) * _width
//			};
//
//			Vector3[] selMeshPosition = new Vector3[] {
//				grid.positions [sel [0]],
//				grid.positions [sel [1]],
//				grid.positions [sel [2]],
//				grid.positions [sel [3]]
//			};
//			//for (int i = 0; i < selMeshPosition.Length; i++) 
//			selMeshPosition [0].Z = hmData[(y*_hmSize + x)*4 + 1] / 256f * heightScale;
//			selMeshPosition [1].Z = hmData[(y*_hmSize + x)*4 + 5] / 256f * heightScale;
//			selMeshPosition [3].Z = hmData[((y+1)*_hmSize + x)*4 + 5] / 256f * heightScale;
//			selMeshPosition [2].Z = hmData[((y+1)*_hmSize + x)*4 + 1] / 256f * heightScale;
//			
//			selMesh = new vaoMesh(selMeshPosition, 
//				null, new int[] {0,1,2,3});




			GL.BindTexture (TextureTarget.Texture2D, circleShader.OutputTex);
			selMesh.Render(PrimitiveType.TriangleStrip);
			GL.BindTexture (TextureTarget.Texture2D, 0);
		}

		byte[] hmData;//height map
		byte[] splatData;//ground texture splatting
		byte[] selectionMap;//has grid positions as colors

//		void getSplatData()
//		{
//			splatData = new byte[_splatingSize*_splatingSize*4];
//			GL.BindTexture (TextureTarget.Texture2D, splattingBrushShader.OutputTex);
//
//			GL.GetTexImage (TextureTarget.Texture2D, 0, 
//				PixelFormat.Rgba, PixelType.UnsignedByte, splatData);
//
//			GL.BindTexture (TextureTarget.Texture2D, 0);
//		}
		void updateSplatting()
		{
			float radiusDiv = 40f / (float)_circleTexSize;
			splattingBrushShader.Radius = circleShader.Radius * radiusDiv;
			splattingBrushShader.Center = SelectionPos.Xy * 4f / (float)(_splatingSize);
			splattingBrushShader.Update ();
			gridShader.SplatTexture = splattingBrushShader.OutputTex;
			gridCacheIsUpToDate = false;
		}
//		void updateSplat()
//		{
//			GL.BindTexture (TextureTarget.Texture2D, gridShader.SplatTexture);
//
//			GL.TexSubImage2D (TextureTarget.Texture2D,
//				0, 0, 0, _splatingSize, _splatingSize, PixelFormat.Bgra, PixelType.UnsignedByte, splatData);
//
//			GL.BindTexture (TextureTarget.Texture2D, 0);
//			gridCacheIsUpToDate = false;
//			splatTextureIsUpToDate = true;
//		}
		void getHeightMapData()
		{			
			GL.BindTexture (TextureTarget.Texture2D, hmGenerator.OutputTex);

			GL.GetTexImage (TextureTarget.Texture2D, 0, 
				PixelFormat.Rgba, PixelType.UnsignedByte, hmData);

			GL.BindTexture (TextureTarget.Texture2D, 0);
		}
		void updateHeightMap()
		{
			GL.BindTexture (TextureTarget.Texture2D, hmGenerator.OutputTex);

			GL.TexSubImage2D (TextureTarget.Texture2D,
				0, 0, 0, _hmSize, _hmSize, PixelFormat.Bgra, PixelType.UnsignedByte, hmData);

			GL.BindTexture (TextureTarget.Texture2D, 0);
			gridCacheIsUpToDate = false;
			heightMapIsUpToDate = true;

			//force update of selection mesh
			SelectionPos = selPos;
		}
		void getSelectionTextureData()
		{
			GL.BindTexture (TextureTarget.Texture2D, gridSelectionTex);

			GL.GetTexImage (TextureTarget.Texture2D, 0, 
				PixelFormat.Rgba, PixelType.UnsignedByte, selectionMap);

			GL.BindTexture (TextureTarget.Texture2D, 0);
		}

		#region Grid Cache
		bool gridCacheIsUpToDate = false,
			heightMapIsUpToDate = true,
			splatTextureIsUpToDate = true;
		QuadVAO cacheQuad;
		Matrix4 cacheProjection;
		int gridCacheTex, gridSelectionTex;
		int fboGrid, depthRenderbuffer;
		DrawBuffersEnum[] dbe = new DrawBuffersEnum[]
		{
			DrawBuffersEnum.ColorAttachment0 ,
			DrawBuffersEnum.ColorAttachment1
		};
		

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

		#region FBO
		void initGridFbo()
		{
			System.Drawing.Size cz = ClientRectangle.Size;

			gridCacheTex = new Texture (cz.Width, cz.Height);
			gridSelectionTex = new Texture (cz.Width, cz.Height);

			setTexFilterNeareast (gridSelectionTex);


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

			gridShader.Enable ();

			//4th component of selection texture is used as coordinate, not as alpha
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
		#endregion

		#endregion

		public void UpdateViewMatrix()
		{
			Rectangle r = this.ClientRectangle;
			GL.Viewport( r.X, r.Y, r.Width, r.Height);
			projection = Matrix4.CreatePerspectiveFieldOfView (fovY, r.Width / (float)r.Height, zNear, zFar);
			Vector3 vEye = vEyeTarget + vLook * eyeDist;
			modelview = Matrix4.LookAt(vEye, vEyeTarget, Vector3.UnitZ);
			GL.GetInteger(GetPName.Viewport, viewport);

			updateShadersMatrices ();

			gridCacheIsUpToDate = false;
		}			



		int ptrSplat = 0;
		int ptrHM = 0;

		public int PtrSplat{ get { return ptrSplat; } }
		public int PtrHM{ get { return ptrHM; } }

		public void UpdatePtrSplat(){
			int splatXDisp = (int)Math.Floor((SelectionPos.X - Math.Truncate (SelectionPos.X)) * 4.0f);
			int splatyDisp = (int)Math.Floor((SelectionPos.Y - Math.Truncate (SelectionPos.Y)) * 4.0f);
			//int ptrSplat = (int)((SelectionPos.X + (int)SelectionPos.Y * (float)_splatingSize) * 16f);
			int xDisp = (int)SelectionPos.X * 16 + splatXDisp * 4;
			int yDisp = (int)SelectionPos.Y * _splatingSize * 16 + splatyDisp * _splatingSize * 4;
			ptrSplat = xDisp+yDisp;
			NotifyValueChange ("PtrSplat", ptrSplat);				
		}
		void updatePtrHm()
		{
			ptrHM = ((int)Math.Round(SelectionPos.X) + (int)Math.Round(SelectionPos.Y) * _hmSize) * 4 ;
			NotifyValueChange ("PtrHM", ptrHM);
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
		#region Interface
		GraphicObject splattingMenu = null;
		void initInterface()
		{
			this.MouseButtonUp += Mouse_ButtonUp;
			this.MouseWheelChanged += new EventHandler<MouseWheelEventArgs>(Mouse_WheelChanged);
			this.MouseMove += new EventHandler<MouseMoveEventArgs>(Mouse_Move);

			LoadInterface("#Ottd3D.ui.fps.goml").DataSource = this;
			LoadInterface("#Ottd3D.ui.menu.goml").DataSource = this;
		}
		#region Mouse
		void Mouse_Move(object sender, MouseMoveEventArgs e)
		{			
			if (e.XDelta != 0 || e.YDelta != 0)
			{
				NotifyValueChange("MousePos", MousePos);
				int selPtr = (e.X * 4 + (ClientRectangle.Height - e.Y) * ClientRectangle.Width * 4);
				//				SelectionPos = new Vector3 (selectionMap [selPtr], 
				//					selectionMap [selPtr + 1], selectionMap [selPtr + 2]);
				SelectionPos = new Vector3 (
					(float)selectionMap [selPtr] + (float)selectionMap [selPtr + 1] / 255f, 
					(float)selectionMap [selPtr + 2] + (float)selectionMap [selPtr + 3] / 255f, 0f);

				switch (CurrentState) {
				case GameState.Playing:					
				case GameState.GroundLeveling:
					updatePtrHm ();
					break;
				case GameState.GroundTexturing:
					UpdatePtrSplat ();
					break;
				}

				if (e.Mouse.MiddleButton == OpenTK.Input.ButtonState.Pressed) {
					if (Keyboard [Key.ShiftLeft]) {
						Vector3 v = new Vector3 (
							Vector2.Normalize (vLook.Xy.PerpendicularLeft));
						Vector3 tmp = Vector3.Transform (vLook, 
							Matrix4.CreateRotationZ (-e.XDelta * RotationSpeed) *
							Matrix4.CreateFromAxisAngle (v, -e.YDelta * RotationSpeed));
						tmp.Normalize ();
						if (tmp.Z <= 0f)
							return;
						vLook = tmp;
					} else {
						Vector3 vH = new Vector3(Vector2.Normalize(vLook.Xy.PerpendicularLeft) * e.XDelta * MoveSpeed * eyeDist);
						Vector3 vV = new Vector3(Vector2.Normalize(vLook.Xy) * e.YDelta * MoveSpeed * eyeDist);
						vEyeTarget -= vH + vV;						
					}
					UpdateViewMatrix ();
					return;
				}

			}

		}			
		void Mouse_WheelChanged(object sender, MouseWheelEventArgs e)
		{
			float speed = ZoomSpeed * eyeDist;
			if (Keyboard [Key.ShiftLeft]) {
				if (CurrentState == GameState.GroundTexturing) {
					if (e.Delta > 0)
						splatBrushRadius *= 1.25f;
					else
						splatBrushRadius *= 0.8f;
					if (splatBrushRadius > 0.5f)
						splatBrushRadius = 0.5f;
					else if (splatBrushRadius < 0.0125f)
						splatBrushRadius = 0.0125f;
					circleShader.Radius = splatBrushRadius;
					circleShader.Update ();
					return;
				}
			}
			else if (Keyboard[Key.ControlLeft])
				speed *= 20.0f;

			eyeDistTarget -= e.Delta * speed;
			if (eyeDistTarget < zNear+5)
				eyeDistTarget = zNear+5;
			else if (eyeDistTarget > zFar-100)
				eyeDistTarget = zFar-100;
			Animation.StartAnimation(new Animation<float> (this, "EyeDist", eyeDistTarget, (eyeDistTarget - eyeDist) * 0.2f));
		}
		void Mouse_ButtonUp (object sender, MouseButtonEventArgs e)
		{
			switch (CurrentState) {
			case GameState.Playing:
				break;
			case GameState.GroundLeveling:
				if (e.Button == MouseButton.Left) {
					hmData [ptrHM+1] += 1;
					heightMapIsUpToDate = false;
				} else if (e.Button == MouseButton.Right) {
					if (hmData [ptrHM + 1] == 0)
						break;
					hmData [ptrHM+1] -= 1;
					heightMapIsUpToDate = false;
				}	
				break;
			case GameState.GroundTexturing:
				break;
			}
		}
		#endregion

		void onGameStateChange (object sender, ValueChangeEventArgs e)
		{
			if (e.MemberName != "IsChecked" || (bool)e.NewValue != true)
				return;

			if (CurrentState == GameState.GroundTexturing)
				splattingMenu.Visible = false;
			
			GraphicObject g = sender as GraphicObject;
			switch (g.Name) {
			case "play":
				CurrentState = GameState.Playing;
				break;
			case "hmEdit":
				CurrentState = GameState.GroundLeveling;
				circleShader.Radius = 0.002f;
				circleShader.Update ();
				break;
			case "splatEdit":
				CurrentState = GameState.GroundTexturing;
				circleShader.Radius = splatBrushRadius;
				circleShader.Update ();

				if (splattingMenu == null) {
					splattingMenu = LoadInterface ("#Ottd3D.ui.SpattingBrush.goml");
					splattingMenu.DataSource = this;
				}else
					splattingMenu.Visible = true;
				break;
			}
			//force update of position mesh
			SelectionPos = selPos;
		}
		void onSelectedBrushChanged(object sender, SelectionChangeEventArgs e){
			splatBrush.Y = (float)Array.IndexOf (GroundTextures, e.NewValue.ToString())/255f;

//			mainMenu.Visible = true;
//			imgSelection.Visible = false;
//			ImagePath = e.NewValue.ToString();
		}
		#endregion

		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);

			initInterface ();

			initShaders ();

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

			hmData = new byte[_hmSize*_hmSize*4];
			getHeightMapData ();

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

			if (CurrentState == GameState.GroundTexturing) {
				MouseState mouse = Mouse.GetState ();
				if (mouse[MouseButton.Left]) {
//					if (splatData [ptrSplat + 1] != CurrentTexture) {
//						splatData [ptrSplat + 1] = CurrentTexture;
//						splatData [ptrSplat] = 0;
//					} else {
//
//						if ((int)splatData [ptrSplat] + splatPressure > 255)
//							splatData [ptrSplat] = 255;
//						else
//							splatData [ptrSplat] += (byte)splatPressure;
//					}
					splattingBrushShader.Color = splatBrush;
					updateSplatting ();
				} else if (mouse[MouseButton.Right]) {
//					if ((int)splatData [ptrSplat] - splatPressure < 0)
//						splatData [ptrSplat] = 0;
//					else
//						splatData[ptrSplat] -= (byte)splatPressure;
//
//					splatTextureIsUpToDate = false;
					splattingBrushShader.Color = new Vector4(0f,0f,0f,1f);
					updateSplatting();
				}							
			}


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
//			if (!splatTextureIsUpToDate)
//				updateSplat ();
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
			GL.Clear (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
		}
		public override void OnRender (FrameEventArgs e)
		{
			drawGrid ();
			drawHoverCase ();
		}

		#region Main and CTOR
		[STAThread]
		static void Main ()
		{
			Console.WriteLine ("starting example");

			using (Ottd3DWindow win = new Ottd3DWindow( )) {
				win.Run (30.0);
			}
		}
		public Ottd3DWindow ()
			: base(1024, 800,"test")
		{}
		#endregion
	}
}