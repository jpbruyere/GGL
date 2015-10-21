// Released to the public domain. Use, modify and relicense at will.




#define DEBUG
using System;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using OpenTK.Input;

//using System.Drawing;
//using System.Drawing.Imaging;
using System.Diagnostics;
using System.Collections.Generic;

using System.IO;
using GLU = OpenTK.Graphics.Glu;
using GGL;
using Examples.Shapes;
using go;
using System.Threading;

//using UnicodeTools;

using System.Drawing.Imaging;

namespace test
{
	enum GameState
	{
		Stopped,
		Run
	}
	class Shoothemup : OpenTKGameWindow
	{
		#if _WIN32 || _WIN64
		public const string rootDir = @"d:\";
		#elif __linux__
		public const string rootDir = @"/mnt/data/";
		#endif

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

		public static double elapsedTime = 0;
		public static float elapsedSeconds = 0f;

		#region matrices and view vectors
		public static Matrix4 modelview;
		public static Matrix4 projection;
		public static Vector3 vEye = new Vector3 (0.0f, -20f, 35.0f);    // Camera Position
		public static Vector3 vLook = new Vector3 (0f, 0.7f, -0.7f);  // Camera vLook Vector
		public static Vector4 vLight = new Vector4 (5.0f, 5.0f, 100.0f, 1.0f);

		public static float FocusAngle {
			get { return Vector3.CalculateAngle (vLook, Vector3.UnitZ); }
		}

		float zFar = 6400.0f;
		float zNear = 0.1f;
		float fovY = (float)Math.PI / 4;
		float MoveSpeed = 0.5f;
		#endregion

		GameState currentState = GameState.Run;

		void init ()
		{


			Keyboard.KeyDown += new EventHandler<KeyboardKeyEventArgs> (Keyboard_KeyDown);
			Mouse.WheelChanged += new EventHandler<MouseWheelEventArgs> (Mouse_WheelChanged);

			Mouse.Move += new EventHandler<MouseMoveEventArgs> (Mouse_Move);
			//Interface.Keyboard = Keyboard;

			GL.ClearColor (0.0f, 0.0f, 0.0f, 0.0f);

			GL.Enable (EnableCap.DepthTest);
			//GL.Enable(EnableCap.VertexProgramPointSize);
			//GL.DepthFunc(DepthFunction.Lequal);
			//GL.PolygonOffset(5.0f, 0.0f);

			GL.Hint (HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
			GL.ShadeModel (ShadingModel.Smooth);

			GL.Enable (EnableCap.CullFace);
			//GL.Enable (EnableCap.ColorMaterial);
			GL.Enable (EnableCap.Blend);
			GL.BlendFunc (BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

			//GL.FrontFace (FrontFaceDirection.Ccw);

			//GL.Enable(EnableCap.Lighting);
			//GL.Enable(EnableCap.Normalize);
			//GL.Enable(EnableCap.Light0);

			//GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
			//GL.LineWidth(1f);

			Material.initShaders ();

			UpdateViewMatrix ();

			initLights ();

			createScene ();   


		}
		void initLights()
		{
			float[] lmKa = { 0.0f, 0.0f, 0.0f, 0.0f };

			GL.LightModel(LightModelParameter.LightModelAmbient, lmKa);
			GL.LightModel(LightModelParameter.LightModelLocalViewer, 1f);
			GL.LightModel(LightModelParameter.LightModelTwoSide, 0f);

			//GL.Light(LightName.Light0, LightParameter.SpotDirection, vSpot);
			//GL.Light(LightName.Light0, LightParameter.SpotExponent, 30);
			//GL.Light(LightName.Light0, LightParameter.SpotCutoff, 180);

			float Kc = 1.0f;
			float Kl = 0.0f;
			float Kq = 0.0f;

			GL.Light(LightName.Light0, LightParameter.ConstantAttenuation, Kc);
			GL.Light(LightName.Light0, LightParameter.LinearAttenuation, Kl);
			GL.Light(LightName.Light0, LightParameter.QuadraticAttenuation, Kq);

			float[] light_Ka = { 0.5f, 0.5f, 0.5f, 1.0f };
			float[] light_Kd = { 0.9f, 0.9f, 0.9f, 1.0f };
			float[] light_Ks = { 1.0f, 1.0f, 1.0f, 1.0f };

			GL.Light(LightName.Light0, LightParameter.Position, vLight);
			GL.Light(LightName.Light0, LightParameter.Ambient, light_Ka);
			GL.Light(LightName.Light0, LightParameter.Diffuse, light_Kd);
			GL.Light(LightName.Light0, LightParameter.Specular, light_Ks);
		}

		ModelInstance ship;
		int asteroridModelID = 0;
		List<ModelInstance> asteroids = new List<ModelInstance>();

		ModelInstance miTest;

		float time = 0f;

		BOquads plane,pl;

		Material matTech;

		float yPos = 0f;
		Particle pExplode;
		Particle p0,p1,p2,p3,p4,p5,p6;

		void createScene ()
		{
			plane = BOquads.createPlaneZup(50.0f, 200.0f, 1.0f, 4.0f, -1.5f);
			plane.Prepare ();

			//pl = BOquads.createPlaneZup(5.0f, 5.0f, 1.0f, 1.0f);
			//pl.Prepare ();

			matTech = new Material ("Tech") {
				shader = Material.phongNormalShader,
				DiffuseMap = new Texture (rootDir + @"Images/texture/structures/639-diffuse.jpg"),
				NormalMap = new Texture (rootDir + @"Images/texture/structures/639-normal.jpg"),
				Diffuse = Color.DimGray
			};
					

			MultiSpriteMaterial matExplode = new MultiSpriteMaterial(
				//new Texture (rootDir + @"Images/texture/test/explode.png",false), 4, 4);
				//new Texture (rootDir + @"Images/texture/test/fs2_ani_expl_tex.jpg",false), 8, 8);
				new Texture (rootDir + @"Images/texture/test/explode2.png",false), 8, 8);
				//new Texture (rootDir + @"Images/texture/test/explode_lightning.png",false), 4, 4);
			//new Texture (rootDir + @"Images/texture/test/sottomarino-esplosioni.png",false), 4, 4);
				//new Texture (rootDir + @"Images/texture/test/explosionTest.png",false), 4, 4);

			pExplode = new Particle (20f, 20f, matExplode, 1);

			p0 = new Particle(3f,3f,
				new MultiSpriteMaterial(
					new Texture (rootDir + @"Images/texture/test/explosion3.png",false),
					4, 4),1);
			p1 = new Particle(5f,50f,
				new MultiSpriteMaterial(
					new Texture (rootDir + @"Images/texture/effects/lightning_0.png",false),
					8, 1),1);

			p2 = new Particle(10f,10f,
				new MultiSpriteMaterial(
					new Texture (rootDir + @"Images/texture/effects/torch.png",false),
					8, 8),1);

			p3 = new Particle(10f,10f,
				new MultiSpriteMaterial(
					new Texture (rootDir + @"Images/texture/effects/djin.png",false),
					5, 8),1);
			p4 = new Particle(20f,20f,
				new MultiSpriteMaterial(
					new Texture (rootDir + @"Images/texture/effects/ani7.png",false),
					4, 4),1);

			p5 = new Particle(2f,2f,
					new Material {
						DiffuseMap = new Texture (rootDir + @"Images/texture/test/flare1.png", false),
						shader = Material.simpleShader
					}
				);

			initAsteroids ();
			initSpaceShip ();

			Mesh.ShowBoundingBox = true;

		}
		void initAsteroids()
		{
			ObjModel omAst = ObjModel.Load (rootDir + @"blender/asteroid.obj");
			asteroridModelID = Model.registerModel (omAst);

						
		}
		void addRandomAsteroid()
		{
			ModelInstance miA = new ModelInstance (asteroridModelID, new Vector3 ((float)(-20 + rand.NextDouble () * 40), 70, 0.0f));
			miA.LinearSpeed = -Vector3.UnitY * (float)(0.2 + rand.NextDouble() * 0.5);
			asteroids.Add (miA);
		}

		void initSpaceShip()
		{
			MultiSpriteMaterial flame = new MultiSpriteMaterial (
				new Texture (rootDir + @"Images/texture/test/thruster2.png"), 1, 8);
			
			ObjModel omShip = ObjModel.Load (rootDir + @"obj/spaceship.obj");
			ship = new ModelInstance (Model.registerModel (omShip), new Vector3 (0, 0, 0));

			float yBottom = -3.5f;
			float yTop = -2.0f;

			BOquads test = BOquads.createPlane2 (
				new Vector3 (0.3f, yBottom, 0.4f),
				new Vector3 (0.3f, yTop, 0.4f),
				new Vector3 (-0.7f, yTop, 0.4f),
				new Vector3 (-0.7f, yBottom, 0.4f),
				1,1);
			test = test + BOquads.createPlane2 (
				new Vector3 (0.7f, yBottom, 0.4f),
				new Vector3 (0.7f, yTop, 0.4f),
				new Vector3 (-0.3f, yTop, 0.4f),
				new Vector3 (-0.3f, yBottom, 0.4f),
				1,1);

			Mesh eh = new Mesh (test);
			eh.Faces [0].material = flame;

			omShip += eh;		

		}

		void Draw ()
		{
			GL.Clear (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			//drawHelpers ();


			GL.MatrixMode (MatrixMode.Modelview);
			GL.PushMatrix ();

			GL.Color3 (Color.White);
			GL.Translate (0, -(yPos-90f), 0);
			matTech.Enable ();
			plane.Render ();
			matTech.Disable ();

			GL.PopMatrix ();

			if (currentState == GameState.Run) {
				GL.PushMatrix ();
				GL.Rotate(FocusAngle*MathHelper.Pi,Vector3.UnitX);

				ParticleInstance.RenderParticles ();

				GL.PopMatrix ();
				ship.Render ();
				foreach (ModelInstance a in asteroids)
					a.Render ();				

			}
			//Interface.OpenGLDraw ();
			//DrawInterface();

			SwapBuffers ();
		}

		void drawHelpers ()
		{

			GL.PointSize (10.0f);
			GL.Begin (BeginMode.Points);
			GL.Color3 (Color.Yellow);
			GL.Vertex3 ((double)(vLight.X), (double)(vLight.Y), (double)(vLight.Z));
			GL.End ();

			GL.Begin (BeginMode.Lines);
			{
				GL.Color3 (Color.Red);
				GL.Vertex3 (Vector3.Zero);
				GL.Vertex3 (Vector3.UnitX);

				GL.Color3 (Color.YellowGreen);
				GL.Vertex3 (Vector3.Zero);
				GL.Vertex3 (Vector3.UnitY);

				GL.Color3 (Color.Blue);
				GL.Vertex3 (Vector3.Zero);
				GL.Vertex3 (Vector3.UnitZ);
			}
			GL.End ();
		}

        #region Game windows events
		/// <summary>Load resources here.</summary>
		/// <param name="e">Not used.</param>
		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);
			init ();
		}

		protected override void OnUnload (EventArgs e)
		{

		}
		/// <summary>
		/// Called when your window is resized. Set your viewport here. It is also
		/// a good place to set up your projection matrix (which probably changes
		/// along when the aspect ratio of your window).
		/// </summary>
		/// <param name="e">Not used.</param>
		protected override void OnResize (EventArgs e)
		{
			base.OnResize (e);

//			Interface.renderBounds = this.ClientRectangle;
//			Interface.createOpenGLSurface ();
//

			UpdateViewMatrix ();
		}
		int frameCpt = 0;
		private int animFrameCpt = 0;

		const float decelFactor = 0.7f;
		const float maxLateralSpeed = 10.0f;
		const float maxVerticalSpeed = 10.0f;

		float _lateralSpeed = 0f;
		float _verticalSpeed = 0f;
		float LateralSpeed
		{
			get { return _lateralSpeed; }
			set {
				_lateralSpeed = value;
				if (Math.Abs (_lateralSpeed) < 0.00001f)
					_lateralSpeed = 0f;
				else if (_lateralSpeed > maxLateralSpeed)
					_lateralSpeed = maxLateralSpeed;
				else if (_lateralSpeed < -maxLateralSpeed)
					_lateralSpeed = -maxLateralSpeed;
				
			}
		}
		float VerticalSpeed
		{
			get { return _verticalSpeed; }
			set{
				_verticalSpeed = value;
				if (Math.Abs (_verticalSpeed) < 0.00001f)
					_verticalSpeed = 0f;
				else if (_verticalSpeed > maxVerticalSpeed)
					_verticalSpeed = maxVerticalSpeed;
				else if (_verticalSpeed < -maxVerticalSpeed)
					_verticalSpeed = -maxVerticalSpeed;

			}
		}

		Random rand = new Random ();
		/// <summary>
		/// Called when it is time to setup the next frame. Add you game logic here.
		/// </summary>
		/// <param name="e">Contains timing information for framerate independent logic.</param>
		///         
		protected override void OnUpdateFrame (FrameEventArgs e)
		{
			base.OnUpdateFrame (e);

			fps = (int)RenderFrequency;

			yPos += 0.4f;

			if (yPos > 50.0f)
				yPos = 0.0f;

			frameCpt++;

			if (currentState != GameState.Run)
				return;

			#region CollisionDetection

			int i;

			(Material.multiSpriteShader as MultiSpriteShader).frame++;
			ParticleInstance.UpdateParticles ();

			List<ParticleInstance> newParticules = new List<ParticleInstance> ();
			List<ParticleInstance> particulesToRemove  = new List<ParticleInstance> ();
			foreach (ParticleInstance pi in ParticleInstance.Particles ) {
				i = 0;
				while(i < asteroids.Count){
					ModelInstance asteroid = asteroids[i];
					if (asteroid.PointIsIn(pi.position)) {
						asteroids.Remove (asteroid);
						particulesToRemove.Add (pi);
						newParticules.Add (
							new ParticleInstance (p4, asteroid.Position, Vector3.Zero, Vector3.Zero));
						continue;
					}
					i++;
				}					
				if (pi.position.Y > 70)
					particulesToRemove.Add (pi);     				
			}
			foreach (ParticleInstance pi in particulesToRemove) {
				ParticleInstance.Particles.Remove (pi);
			}

			foreach (ParticleInstance pi in newParticules) {
				ParticleInstance.Particles.Add (pi);
			}
			i = 0;
			while (i < asteroids.Count) {
				ModelInstance a = asteroids [i];
				if (a.PointIsIn(ship.Position + new Vector3(0,2.5f,0.1f)) ||
					a.PointIsIn(ship.Position + new Vector3(-2f,0.5f,0.1f)) ||
					a.PointIsIn(ship.Position + new Vector3(+2f,0.5f,0.1f))) {
					asteroids.Remove (a);
					ParticleInstance.Particles.Add (
						new ParticleInstance (pExplode, ship.Position, Vector3.Zero, Vector3.Zero));					
				}
					
				if (a.Position.Y < -10f) {
					asteroids.Remove (a);
					continue;
				}
				a.Anim ();
				i++;
			}

			#endregion

			if (rand.NextDouble () > 0.9)
				addRandomAsteroid ();


			//Animation.ProcessAnimations();
	
			ship.Position += LateralSpeed * Vector3.UnitX ;
			ship.Position += VerticalSpeed * Vector3.UnitY;
			ship.yAngle = LateralSpeed * (float)Math.PI * 0.1f;						


			foreach (ModelInstance a in asteroids)
				a.zAngle += 0.1f;
		

			#region Keyboard handling
			bool processMoveKey = true;

			if (processMoveKey) {
				if (Keyboard [Key.Space] && frameCpt % 4 == 0)
					ParticleInstance.Particles.Add (
						new ParticleInstance (p5,ship.Position + new Vector3(0,0.1f,0.1f),new Vector3(0,2.0f,0), Vector3.Zero));
				
				if (Keyboard [Key.ShiftLeft]) {
					//light movment
					if (Keyboard [Key.Up])
						vLight.X -= MoveSpeed * 0.5f;
					else if (Keyboard [Key.Down])
						vLight.X += MoveSpeed * .5f;
					else if (Keyboard [Key.Left])
						vLight.Y -= MoveSpeed * .5f;
					else if (Keyboard [Key.Right])
						vLight.Y += MoveSpeed * .5f;
					else if (Keyboard [Key.PageUp])
						vLight.Z += MoveSpeed * .5f;
					else if (Keyboard [Key.PageDown])
						vLight.Z -= MoveSpeed * .5f;
					GL.Light(LightName.Light0, LightParameter.Position, vLight);
				} else {
					//ship.yAngle = 0f;
					if (Keyboard [Key.Up])
						VerticalSpeed += 0.2f;					
					else if (Keyboard [Key.Down])
						VerticalSpeed -= 0.2f;
					else if (VerticalSpeed != 0f)
						VerticalSpeed = VerticalSpeed * decelFactor;
					
					if (Keyboard [Key.Left])
						LateralSpeed -= 0.2f;
					else if (Keyboard [Key.Right])
						LateralSpeed += 0.2f;
					else{
						if (LateralSpeed != 0f)
							LateralSpeed = LateralSpeed * decelFactor;

					}
						

					if (Keyboard [Key.PageUp]) {
						vEye.Z += MoveSpeed;
						UpdateViewMatrix ();
					}
					if (Keyboard [Key.PageDown]) {
						vEye.Z -= MoveSpeed;
						UpdateViewMatrix ();
					}
					//if (Keyboard[Key.Insert])
					//{
					//    //viewPort.Inflate(-1, -1);
					//    fovY -= (float)Math.PI / 32;
					//    if (fovY < 0)
					//        fovY = 0;
					//}
					//if (Keyboard[Key.Delete])
					//{
					//    //viewPort.Inflate(1, 1);
					//    fovY += (float)Math.PI / 32;
					//}
				}
			}
			#endregion
            
		}

		public override void GLClear ()
		{
			GL.ClearColor(0.1f, 0.1f, 0.3f, 1.0f);
			GL.Clear (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
		}
		public override void OnRender (FrameEventArgs e)
		{			
			Draw ();
		}

        #endregion

        #region Mouse Handling
		private void mousePicking ()
		{
			const int buffSize = 24;

			World.Picking = true;

			int x = Mouse.X;
			int y = Mouse.Y;
			//Debug.WriteLine("X:" + x + " Y:" + y);

			int[] selectBuffer = new int[buffSize];
			GL.SelectBuffer (buffSize, selectBuffer);

			GL.MatrixMode (MatrixMode.Projection);
			GL.PushMatrix ();
			GL.RenderMode (RenderingMode.Select);

			int[] viewport = new int[4];
			GL.GetInteger (GetPName.Viewport, viewport);

			/*
                restrict the draw to an area around the cursor
            */
			GL.LoadIdentity ();
			GLU.PickMatrix (x - 1, viewport [3] - y + viewport [1] - 1, 2.0, 2.0, viewport);

			float fAspect = (float)viewport [2] / (float)viewport [3];

			GLU.Perspective (45f, fAspect, 1.0, 4250);


			GL.InitNames ();
			GL.PushName (-1);


			Draw ();

			GL.MatrixMode (MatrixMode.Projection);

			GL.PopMatrix ();

			int hits = GL.RenderMode (RenderingMode.Render);

			//CardInstance tmp = null; 

			for (int i = 3; i < selectBuffer.Length; i += 4) {
				int selectedIndex = selectBuffer [i];
//                if (CardInstance.CardInstanceDic.ContainsKey(selectedIndex))
//                {
//                    tmp = CardInstance.CardInstanceDic[selectBuffer[i]];
                    
//                    break;
//                }
			}
//            CardInstance.selectedCard = tmp;
			UpdateViewMatrix ();


			GL.PushName (-1);

			World.Picking = false;
		}

		void Mouse_Move (object sender, MouseMoveEventArgs e)
		{
//			go.Mouse.Position = e.Position;
//
//			Interface.ProcessMousePosition ();
//
//			if (go.Mouse.Delta != 0) {
//				if (go.Mouse.RightButton == MouseButtonStates.Pressed) {
//					//camera rotation
//					Matrix4 m = Matrix4.CreateRotationZ (go.Mouse.Delta.X * Mouse3d.RotationSpeed);
//					vLook = Vector3.Transform (vLook, m);
//
//					Matrix4 m2 = Matrix4.Rotate (vLookPerpendicularOnXYPlane, -go.Mouse.Delta.Y * Mouse3d.RotationSpeed);
//
//					vLook = Vector3.Transform (vLook, m2);
//					UpdateViewMatrix ();
//				}
//			}
		}
	
	
		void Mouse_WheelChanged (object sender, MouseWheelEventArgs e)
		{
			float speed = MoveSpeed;
			if (Keyboard [Key.ShiftLeft])
				speed *= 0.1f;
			else if (Keyboard [Key.ControlLeft])
				speed *= 20.0f;

			vEye += vLook * e.Delta * speed;
			UpdateViewMatrix ();
		}
        #endregion

        #region keyboard Handling
		void Keyboard_KeyDown (object sender, KeyboardKeyEventArgs e)
		{


			switch (e.Key) {
			case Key.W:
				ParticleInstance.Particles.Add (
					new ParticleInstance (p0, new Vector3(0,20,0), Vector3.Zero, Vector3.Zero));
				break;
			case Key.X:
				ParticleInstance.Particles.Add (
					new ParticleInstance (p1, new Vector3(0,20,0), Vector3.Zero, Vector3.Zero));
				break;
			case Key.C:
				ParticleInstance.Particles.Add (
					new ParticleInstance (p2, new Vector3(0,20,0), Vector3.Zero, Vector3.Zero));
				break;
			case Key.V:
				ParticleInstance.Particles.Add (
					new ParticleInstance (p3, new Vector3(0,20,0.1f), Vector3.Zero, Vector3.Zero));
				break;
			case Key.B:
				ParticleInstance.Particles.Add (
					new ParticleInstance (p4, new Vector3(0,20,0.1f), Vector3.Zero, Vector3.Zero));
				break;
			case Key.N:
				ParticleInstance.Particles.Add (
					new ParticleInstance (pExplode, new Vector3(0,10,0), Vector3.Zero, Vector3.Zero));
				break;
			case Key.A:
				addRandomAsteroid ();
				break;
			default:
				break;
			}
		}
        #endregion

        #region vLookCalculations
		Vector3 vLookDirOnXYPlane {
			get {
				Vector3 v = vLook;
				v.Z = 0;
				v.Normalize ();
				return v;
			}
		}

		public Vector3 vLookPerpendicularOnXYPlane {
			get {
				//vecteur perpendiculaire sur le plan x,y
				Vector3 v = new Vector3 (new Vector2 (vLook).PerpendicularLeft);
				v.Normalize ();
				return v;
			}
		}

        #endregion

		public void UpdateViewMatrix ()
		{
			GL.Viewport (ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height);
			projection = Matrix4.CreatePerspectiveFieldOfView (fovY, ClientRectangle.Width / (float)ClientRectangle.Height, zNear, zFar);
			GL.MatrixMode (MatrixMode.Projection);
			GL.LoadIdentity ();
			GL.LoadMatrix (ref projection);

			modelview = Matrix4.LookAt (vEye, vEye + vLook, Vector3.UnitZ);
			GL.MatrixMode (MatrixMode.Modelview);
			GL.LoadIdentity ();
			GL.LoadMatrix (ref modelview);

			GL.Light(LightName.Light0, LightParameter.Position, vLight);
		}


		/// <summary>Creates a 800x600 window with the specified title.</summary>
		public Shoothemup ()
			: base(1024, 800,"test")
		{
			VSync = VSyncMode.On;
		}
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main ()
		{
			Console.WriteLine ("starting example");

			using (Shoothemup game = new Shoothemup( )) {
				game.Run (30.0);
			}
		}
	}
}