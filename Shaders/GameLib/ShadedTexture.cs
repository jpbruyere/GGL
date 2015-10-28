   using System;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using GGL;

namespace GameLib
{
	public class ShadedTexture : Shader
	{
		protected static vaoMesh quad;

		protected int 	resolutionLocation;

		protected int width, height;
		int tex, fbo;

		public ShadedTexture (string effectId, int _width = -1, int _height = -1) : 
			base(effectId + ".vert", effectId + ".frag")
		{
			if (_width < 0)
				return;

			width = _width;
			height = _height;

			if (height < 0)
				height = width;

			initFbo ();

			Enable ();

			Resolution = new Vector2 (width, height);
			ProjectionMatrix = OpenTK.Matrix4.CreateOrthographicOffCenter(-0.5f, 0.5f, -0.5f, 0.5f, 1, -1);
			ModelViewMatrix = Matrix4.Identity;
			ModelMatrix = Matrix4.Identity;

			Disable ();

			if (quad == null)
				quad = new vaoMesh (0, 0, 0, 1, 1, 1, 1);
		}

		public override void Reload ()
		{
			base.Reload ();

			initFbo ();
			Enable ();
			Resolution = new Vector2 (width, height);
			Disable ();
		}

		public int Texture { get { return tex; } }

		public void Update ()
		{
			this.Enable ();
			updateFbo ();
			this.Disable ();
		}

		#region FBO

		void initFbo()
		{
			tex = new Texture (width, height);
			GL.GenFramebuffers(1, out fbo);

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
				TextureTarget.Texture2D, tex, 0);

			if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
			{
				throw new Exception(GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer).ToString());
			}

			GL.BindFramebuffer(FramebufferTarget.FramebufferExt, 0);

		}
		protected void updateFbo()
		{				
			GL.Viewport(0, 0, width, height);
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
			GL.ClearColor (0, 0, 0, 0);
			GL.Clear (ClearBufferMask.ColorBufferBit);
			quad.Render (PrimitiveType.TriangleStrip);
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
		}
		#endregion

		public Vector2 Resolution { set { GL.Uniform2 (resolutionLocation, value.X, value.Y); }}

		protected override void GetUniformLocations ()
		{
			base.GetUniformLocations ();

			resolutionLocation = GL.GetUniformLocation(pgmId, "resolution");
		}
		public override void Dispose ()
		{
			base.Dispose ();
			GL.DeleteTexture (tex);
			GL.Ext.DeleteFramebuffer (fbo);
		}
	}
}

