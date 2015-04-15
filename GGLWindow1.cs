// Released to the public domain. Use, modify and relicense at will.
#define DEBUG

using System;

using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using OpenTK.Input;
//using System.Drawing;
//using System.Drawing.Imaging;
using System.Diagnostics;
using System.Collections.Generic;
//using System.Windows.Forms;
using System.IO;
using GLU = OpenTK.Graphics.Glu;
using GGL;
using Examples.Shapes;

using go;
using System.Threading;

using System.Drawing.Imaging;



namespace GGL
{
	class GGLWindow1 : GameWindow
    {
		/// <summary>Creates a 800x600 window with the specified title.</summary>
		public GGLWindow1(int _width, int _height)
			: base(_width, _height, new OpenTK.Graphics.GraphicsMode(32, 24, 0, 8), "GGL")
		{
			VSync = VSyncMode.On;
		}

        bool saveShadowOnDisk = false;

        public static Matrix4 modelview;
        public static Matrix4 projection;
        public static Matrix4 lightView;
        public static Matrix4 lightProjection;

        public static Vector3 vEye = new Vector3(5.0f, 5f, 7.0f);    // Camera Position
        public static Vector3 vEyeTarget = Vector3.Zero;
        public static Vector3 vLook = new Vector3(0f, 1f, -0.7f);  // Camera vLook Vector

        float zFar = 6400.0f;
        float zNear = 0.1f;
        float fovY = (float)Math.PI / 4;

        float MoveSpeed = 0.5f;

#if _WIN32 || _WIN64
        public const string rootDir = @"d:\";
#elif __linux__
        public const string rootDir = @"/mnt/data/";
#endif

		Light light0;

        void init()
        {
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

			light0 = new Light (LightName.Light0);

            initHelpersList();

			createScene();

            createShadowMapFboAndTex();            
        }
			
        float time = 0f;

        void createScene()
        {
		}

        #region shadow map
        
        const int SHADOW_MAP_SIZE = 2048;
        float bias = 0.0005f;

        int ShadowMap;
        int fboShadow;

        void createShadowMapFboAndTex()
        {
            GL.GenTextures(1, out ShadowMap);
            GL.Enable(EnableCap.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, ShadowMap);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, (int)TextureCompareMode.CompareRToTexture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareFunc, (int)All.Lequal);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.DepthTextureMode, (int)All.Intensity);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent32, SHADOW_MAP_SIZE, SHADOW_MAP_SIZE, 0, OpenTK.Graphics.OpenGL.PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);

            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.ActiveTexture(TextureUnit.Texture0);

            GL.Ext.GenFramebuffers(1, out fboShadow);
            GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, fboShadow);
            DrawBuffersEnum dbe = DrawBuffersEnum.None;
            GL.DrawBuffers(1, ref dbe);
            GL.Ext.FramebufferTexture2D(FramebufferTarget.FramebufferExt, FramebufferAttachment.DepthAttachmentExt, TextureTarget.Texture2D, ShadowMap, 0);
            GL.DrawBuffer(DrawBufferMode.None);
            GL.ReadBuffer(ReadBufferMode.None);
            FramebufferErrorCode status = GL.Ext.CheckFramebufferStatus(FramebufferTarget.FramebufferExt);
            #region Test for Error

            switch (GL.Ext.CheckFramebufferStatus(FramebufferTarget.FramebufferExt))
            {
                case FramebufferErrorCode.FramebufferCompleteExt:
                    {
                        Debug.WriteLine("FBO: The framebuffer is complete and valid for rendering.");
                        break;
                    }
                case FramebufferErrorCode.FramebufferIncompleteAttachmentExt:
                    {
                        Debug.WriteLine("FBO: One or more attachment points are not framebuffer attachment complete. This could mean there’s no textureID attached or the format isn’t renderable. For color textures this means the base format must be RGB or RGBA and for depth textures it must be a DEPTH_COMPONENT format. Other causes of this error are that the width or height is zero or the z-offset is out of range in case of render to volume.");
                        break;
                    }
                case FramebufferErrorCode.FramebufferIncompleteMissingAttachmentExt:
                    {
                        Debug.WriteLine("FBO: There are no attachments.");
                        break;
                    }
                /* case  FramebufferErrorCode.GL_FRAMEBUFFER_INCOMPLETE_DUPLICATE_ATTACHMENT_EXT: 
                     {
                         Console.WriteLine("FBO: An object has been attached to more than one attachment point.");
                         break;
                     }*/
                case FramebufferErrorCode.FramebufferIncompleteDimensionsExt:
                    {
                        Debug.WriteLine("FBO: Attachments are of different size. All attachments must have the same width and height.");
                        break;
                    }
                case FramebufferErrorCode.FramebufferIncompleteFormatsExt:
                    {
                        Debug.WriteLine("FBO: The color attachments have different format. All color attachments must have the same format.");
                        break;
                    }
                case FramebufferErrorCode.FramebufferIncompleteDrawBufferExt:
                    {
                        Debug.WriteLine("FBO: An attachment point referenced by GL.DrawBuffers() doesn’t have an attachment.");
                        break;
                    }
                case FramebufferErrorCode.FramebufferIncompleteReadBufferExt:
                    {
                        Debug.WriteLine("FBO: The attachment point referenced by GL.ReadBuffers() doesn’t have an attachment.");
                        break;
                    }
                case FramebufferErrorCode.FramebufferUnsupportedExt:
                    {
                        Debug.WriteLine("FBO: This particular FBO configuration is not supported by the implementation.");
                        break;
                    }
                default:
                    {
                        Debug.WriteLine("FBO: Status unknown. (yes, this is really bad.)");
                        break;
                    }
            }

            // using FBO might have changed states, e.g. the FBO might not support stereoscopic views or double buffering
            int[] queryinfo = new int[6];
            GL.GetInteger(GetPName.MaxColorAttachmentsExt, out queryinfo[0]);
            GL.GetInteger(GetPName.AuxBuffers, out queryinfo[1]);
            GL.GetInteger(GetPName.MaxDrawBuffers, out queryinfo[2]);
            GL.GetInteger(GetPName.Stereo, out queryinfo[3]);
            GL.GetInteger(GetPName.Samples, out queryinfo[4]);
            GL.GetInteger(GetPName.Doublebuffer, out queryinfo[5]);
            //Console.WriteLine("max. ColorBuffers: " + queryinfo[0] + " max. AuxBuffers: " + queryinfo[1] + " max. DrawBuffers: " + queryinfo[2] + "\nStereo: " + queryinfo[3] + " Samples: " + queryinfo[4] + " DoubleBuffer: " + queryinfo[5]);

            Console.WriteLine("Last GL Error: " + GL.GetError());

            #endregion Test for Error
            GL.Ext.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        }
        void setupShadowTextureMatrix()
        {

            GL.ActiveTexture(TextureUnit.Texture7);
			GL.BindTexture(TextureTarget.Texture2D, ShadowMap);
            GL.Enable(EnableCap.Texture2D);

            GL.MatrixMode(MatrixMode.Texture);

            GL.LoadIdentity();
            GL.Translate(0.5f, 0.5f, 0.5f);
            GL.Scale(0.5f, 0.5f, 0.5f);
            GL.MultMatrix(ref lightProjection);
            GL.MultMatrix(ref lightView);

            // Go back to normal matrix mode
            GL.MatrixMode(MatrixMode.Modelview);
            GL.ActiveTexture(TextureUnit.Texture0);
        }
        void resetShadowTextureMatrix()
        {
            GL.MatrixMode(MatrixMode.Texture);
            GL.ActiveTexture(TextureUnit.Texture7);

            GL.LoadIdentity();

            // Go back to normal matrix mode
            GL.MatrixMode(MatrixMode.Modelview);
            GL.ActiveTexture(TextureUnit.Texture0);
        }
        void updateShadowMap()
        {
            GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, fboShadow);


            GL.MatrixMode(MatrixMode.Projection);
            GL.PushMatrix();
            {
                GL.LoadIdentity();
                GL.LoadMatrix(ref lightProjection);

                GL.MatrixMode(MatrixMode.Modelview);
                GL.PushMatrix();
                {
                    GL.LoadIdentity();
                    GL.LoadMatrix(ref lightView);


                    GL.PushAttrib(AttribMask.EnableBit | AttribMask.ViewportBit);
                    {
                        GL.Viewport(0, 0, SHADOW_MAP_SIZE, SHADOW_MAP_SIZE);

                        GL.Clear(ClearBufferMask.DepthBufferBit);

                        resetShadowTextureMatrix();

                        GL.UseProgram(0);

                        //GL.Disable(EnableCap.Lighting);
                        GL.Disable(EnableCap.Normalize);
                        GL.Disable(EnableCap.Texture2D);
                        //GL.Disable(EnableCap.Blend);
                        //GL.ColorMask(false, false, false, false);
                        GL.Enable(EnableCap.CullFace);
                        //GL.PolygonOffset(1, 1);
                        //GL.Enable(EnableCap.PolygonOffsetFill);
                        GL.ShadeModel(ShadingModel.Flat);
                        GL.CullFace(CullFaceMode.Front);

                        drawScene();

                        //GL.ColorMask(true, true, true, true);
                        GL.ShadeModel(ShadingModel.Smooth);
                        //GL.PolygonOffset(0, 0);
                        
                        GL.CullFace(CullFaceMode.Back);
                    }
                    GL.PopAttrib();

                    GL.MatrixMode(MatrixMode.Projection);
                }
                GL.PopMatrix();
                GL.MatrixMode(MatrixMode.Modelview);
            }
            GL.PopMatrix();

            #region save shadow on disk => d:/test.bmp
            if (saveShadowOnDisk)
            {
                int bmpSizeInPixels = SHADOW_MAP_SIZE * SHADOW_MAP_SIZE;
                int bmpSizeInBytes = bmpSizeInPixels * 4;
                float[] depthValues = new float[bmpSizeInBytes];

                GL.BindTexture(TextureTarget.Texture2D, ShadowMap);
                GL.GetTexImage(TextureTarget.Texture2D, 0, OpenTK.Graphics.OpenGL.PixelFormat.DepthComponent, PixelType.Float, depthValues);
                GL.BindTexture(TextureTarget.Texture2D, 0);



                byte[] pixels = new byte[bmpSizeInBytes];
                for (int x = 0; x < SHADOW_MAP_SIZE; x++)
                {
                    for (int y = 0; y < SHADOW_MAP_SIZE; y++)
                    {
                        float d = depthValues[x + y * SHADOW_MAP_SIZE];
                        byte z = (byte)(256 * d);

                        pixels[(x + y * SHADOW_MAP_SIZE) * 4] = z;
                        pixels[(x + y * SHADOW_MAP_SIZE) * 4 + 1] = z;
                        pixels[(x + y * SHADOW_MAP_SIZE) * 4 + 2] = z;
                        pixels[(x + y * SHADOW_MAP_SIZE) * 4 + 3] = 255;
                    }
                }
                System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(SHADOW_MAP_SIZE, SHADOW_MAP_SIZE, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                BitmapData data = bmp.LockBits(new System.Drawing.Rectangle(0, 0, SHADOW_MAP_SIZE, SHADOW_MAP_SIZE),
                        ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                System.Runtime.InteropServices.Marshal.Copy(pixels, 0, data.Scan0, bmpSizeInBytes);
                bmp.UnlockBits(data);
				bmp.Save(@"/home/jp/test.bmp");
            }
            #endregion

            GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, 0);
        }
        #endregion


        bool updateShadow = true;
        void Draw()
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            if (updateShadow)
            {
                updateShadowMap();
                updateShadow = false;
            }
            
            setupShadowTextureMatrix();

            drawScene();
            
            drawHelpers();

            SwapBuffers();
        }

        void drawScene()
        {

        }

        #region Helpers
        int DLhelpers = 0;
        void initHelpersList()
        {
            if (DLhelpers != 0)
                GL.DeleteLists(DLhelpers, 1);
            DLhelpers = GL.GenLists(1);
            GL.NewList(DLhelpers, ListMode.Compile);

            GL.PushAttrib(AttribMask.EnableBit);
            GL.Disable(EnableCap.Lighting);
            GL.Disable(EnableCap.Texture2D);
            GL.UseProgram(0);
            GL.PointSize(2.0f);
            GL.Begin(PrimitiveType.Points);
            GL.Color3(go.Color.Yellow);
			GL.Vertex3(light0.Position);
            GL.End();

            GL.Begin(PrimitiveType.Lines);
            {
                GL.Color3(go.Color.Red);
                GL.Vertex3(Vector3.Zero);
                GL.Vertex3(Vector3.UnitX);

                GL.Color3(go.Color.Green);
                GL.Vertex3(Vector3.Zero);
                GL.Vertex3(Vector3.UnitY);

                GL.Color3(go.Color.Blue);
                GL.Vertex3(Vector3.Zero);
                GL.Vertex3(Vector3.UnitZ);
            }
            GL.End();            
            GL.PopAttrib();
            GL.EndList();
        }
        void drawHelpers()
        {
            GL.CallList(DLhelpers);
        }
        #endregion


        #region Game windows events
        /// <summary>Load resources here.</summary>
        /// <param name="e">Not used.</param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            init();
        }
        protected override void OnUnload(EventArgs e)
        {
            //GL.DeleteTextures(1, ref objMesh.texture);
        }
        /// <summary>
        /// Called when your window is resized. Set your viewport here. It is also
        /// a good place to set up your projection matrix (which probably changes
        /// along when the aspect ratio of your window).
        /// </summary>
        /// <param name="e">Not used.</param>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            Interface.renderBounds = this.ClientRectangle;
            Debug.WriteLine(this.ClientRectangle.ToString());

            UpdateViewMatrix();
        }


        float accTime = 0.0f;



        /// <summary>
        /// Called when it is time to setup the next frame. Add you game logic here.
        /// </summary>
        /// <param name="e">Contains timing information for framerate independent logic.</param>
        ///         
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            time += (float)e.Time;
            GL.Uniform1(GL.GetUniformLocation(shader0, "time"), 1, ref time);

            accTime += 1.0f / (float)RenderFrequency;

            if (accTime > 1.0f)
            {
                this.Title = RenderFrequency.ToString("##.#") + " fps";
                accTime = 0.0f;
            }


            base.OnUpdateFrame(e);


            #region Keyboard handling
            bool processMoveKey = true;

            if (processMoveKey)
            {
                if (Keyboard[Key.ShiftLeft])
                {
                    //light movment
                    if (Keyboard[Key.Up])
                        LightPos[0] -= MoveSpeed * 0.5f;
                    else if (Keyboard[Key.Down])
                        LightPos[0] += MoveSpeed * .5f;
                    else if (Keyboard[Key.Left])
                        LightPos[1] -= MoveSpeed * .5f;
                    else if (Keyboard[Key.Right])
                        LightPos[1] += MoveSpeed * .5f;
                    else if (Keyboard[Key.PageUp])
                        LightPos[2] += MoveSpeed * .5f;
                    else if (Keyboard[Key.PageDown])
                        LightPos[2] -= MoveSpeed * .5f;
                    UpdateLight();
                }
                else
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
            }
            #endregion

        }
        /// <summary>
        /// Called when it is time to render the next frame. Add your rendering code here.
        /// </summary>
        /// <param name="e">Contains timing information.</param>
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            Draw();
        }
        #endregion

        #region Mouse Handling
        private void mousePicking()
        {
            const int buffSize = 24;

            World.Picking = true;

            int x = Mouse.X;
            int y = Mouse.Y;
            //Debug.WriteLine("X:" + x + " Y:" + y);

            int[] selectBuffer = new int[buffSize];
            GL.SelectBuffer(buffSize, selectBuffer);

            GL.MatrixMode(MatrixMode.Projection);
            GL.PushMatrix();
            GL.RenderMode(RenderingMode.Select);

            int[] viewport = new int[4];
            GL.GetInteger(GetPName.Viewport, viewport);

            /*
                restrict the draw to an area around the cursor
            */
            GL.LoadIdentity();
            GLU.PickMatrix(x - 1, viewport[3] - y + viewport[1] - 1, 2.0, 2.0, viewport);

            float fAspect = (float)viewport[2] / (float)viewport[3];

            GLU.Perspective(45f, fAspect, 1.0, 4250);


            GL.InitNames();
            GL.PushName(-1);


            Draw();

            GL.MatrixMode(MatrixMode.Projection);

            GL.PopMatrix();

            int hits = GL.RenderMode(RenderingMode.Render);


            for (int i = 3; i < selectBuffer.Length; i += 4)
            {
                int selectedIndex = selectBuffer[i];
                if (SelectableObject.objectsDic.ContainsKey(selectedIndex))
                {
                    object o = SelectableObject.objectsDic[selectBuffer[i]];

                    SelectableObject.selectedObject = o as SelectableObject;
                    break;
                }

            }
            UpdateViewMatrix();


            GL.PushName(-1);

            World.Picking = false;
        }

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
            if (Interface.MouseIsInInterface)
                Interface.ProcessMouseButtonDown();
            else
            {
                switch (e.Button)
                {
                    case MouseButton.Left:


                        break;
                    case MouseButton.Middle:
                        break;
                    case MouseButton.Right:
                        break;
                    default:
                        break;
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

            vLook = Vector3.NormalizeFast(vEye - vEyeTarget);
            vEye -= vLook * e.Delta * speed;
            UpdateViewMatrix();
        }
        #endregion

        #region keyboard Handling
        void Keyboard_KeyDown(object sender, KeyboardKeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.S:
                    saveShadowOnDisk = !saveShadowOnDisk;
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
			//Matrix4 invMV = Matrix4.Invert(modelview);
			GL.UniformMatrix4(GL.GetUniformLocation(shader0, "modelView"), false, ref modelview);

            UpdateLight();
        }

        int lightZFar = 50;
        int lightZNear = -10;
        float lightProjSize = 10f;
        public void UpdateLight()
        {

            GL.Light(LightName.Light0, LightParameter.Position, LightPos);
            initHelpersList();

            //lightProjection = Matrix4.CreatePerspectiveFieldOfView(fovY, 1.0f, 0.5f, zFar + 3000);
            lightProjection = Matrix4.CreateOrthographicOffCenter
                (-lightProjSize, lightProjSize, -lightProjSize, lightProjSize, lightZNear, lightZFar);

            lightView = Matrix4.LookAt(new Vector3(LightPos[0],LightPos[1],LightPos[2]), Vector3.Zero, Vector3.UnitZ);

            setupShadowTextureMatrix();

            updateShadow = true;
        }
    }
}