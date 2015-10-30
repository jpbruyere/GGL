using System;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using GGL;

namespace GameLib
{
	public class EffectShader : Shader
	{
		protected static vaoMesh quad;

		protected int 	timeLocation,
						resolutionLocation;

		protected int width, height;
		int tex, fbo;

		public EffectShader (string effectId, int _width = -1, int _height = -1) : 
			base(effectId + ".vert", effectId + ".frag")
		{
			if (_width < 0)
				return;

			width = _width;
			height = _height;

			if (height < 0)
				height = width;

			ProjectionMatrix = OpenTK.Matrix4.CreateOrthographicOffCenter(-0.5f, 0.5f, -0.5f, 0.5f, 1, -1);
			Resolution = new Vector2 (width, height);

			initFbo ();

			if (quad == null)
				quad = new vaoMesh (0, 0, 0, 1, 1, 1, 1);
		}

		public int Texture { get { return tex; } }

		public override void Enable ()
		{
			base.Enable ();
			GL.Uniform2 (resolutionLocation, resolution);
		}
		public void Update (float time)
		{
			this.Enable ();

			Time = time;

			GL.Viewport(0, 0, width, height);
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
		void updateFbo()
		{						
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
			GL.ClearColor (0, 0, 0, 0);
			GL.Clear (ClearBufferMask.ColorBufferBit);
			quad.Render (PrimitiveType.TriangleStrip);
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
		}
		#endregion
		Vector2 resolution;

		public float Time { set { GL.Uniform1 (timeLocation, value); }}
		public Vector2 Resolution { set { resolution = value; }}


		protected override void GetUniformLocations ()
		{
			base.GetUniformLocations ();

			timeLocation = GL.GetUniformLocation(pgmId, "time");
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

