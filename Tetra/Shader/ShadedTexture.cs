   using System;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace Tetra
{
	public class ShadedTexture : Shader
	{
		public static GGL.vaoMesh quad;
		public static Matrix4 orthoMat
		= OpenTK.Matrix4.CreateOrthographicOffCenter (-0.5f, 0.5f, -0.5f, 0.5f, 1, -1);

		protected int 	resolutionLocation;

		protected int width, height;
		protected Tetra.Texture tex;
		protected int fbo;
		protected bool clear = true;

		protected DrawBuffersEnum[] drawBuffs;

		Vector2 resolution;

		static ShadedTexture(){
			quad = new GGL.vaoMesh (0, 0, 0, 1, 1, 1, 1);
		}

		public ShadedTexture (string vertResPath, string fragResPath = null, int _width = 256, int _height = 256, Tetra.Texture initTex = null)
			:base(vertResPath, fragResPath)
		{
			if (_width < 0)
				return;

			width = _width;
			height = _height;

			if (height < 0)
				height = width;

			tex = initTex;

			initFbo ();

			resolution = new Vector2 (width, height);

			this.Enable ();
			GL.UniformMatrix4 (mvpLocation, false, ref orthoMat);
			GL.Uniform2 (resolutionLocation, resolution);
			this.Disable ();
		}

		public virtual Tetra.Texture OutputTex { get { return tex; } set { tex = value; }}

		public virtual void Update ()
		{
			this.Enable ();
			updateFbo ();
			this.Disable ();
		}

		#region FBO

		protected virtual void initFbo()
		{
			drawBuffs = new DrawBuffersEnum[] {	DrawBuffersEnum.ColorAttachment0 };

			if (tex != null)
				tex.Dispose();
			tex = new Texture (width, height);
			GL.GenFramebuffers(1, out fbo);

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
				TextureTarget.Texture2D, tex, 0);

			if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
			{
				throw new Exception(GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer).ToString());
			}

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

		}
		protected void updateFbo()
		{			
			int[] viewport = new int[4];
			GL.GetInteger (GetPName.Viewport, viewport);
				
			GL.Viewport(0, 0, width, height);

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
			GL.DrawBuffers(drawBuffs.Length, drawBuffs);

			float[] clearCols = new float[4];
			if (clear) {
				GL.GetFloat (GetPName.ColorClearValue, clearCols);
				GL.ClearColor (0, 0, 0, 0);
				GL.Clear (ClearBufferMask.ColorBufferBit);
			}
			quad.Render (BeginMode.TriangleStrip);

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

			if (clear)
				GL.ClearColor(clearCols[0], clearCols[1], clearCols[2], clearCols[3]);
			GL.Viewport (viewport [0], viewport [1], viewport [2], viewport [3]);
		}
		#endregion

		protected override void GetUniformLocations ()
		{
			base.GetUniformLocations ();

			resolutionLocation = GL.GetUniformLocation(pgmId, "resolution");
		}
		public override void Dispose ()
		{
			base.Dispose ();

			if (tex != null)
				tex.Dispose ();
			if (GL.IsFramebuffer(fbo))
				GL.DeleteFramebuffer (fbo);
		}
	}
}

