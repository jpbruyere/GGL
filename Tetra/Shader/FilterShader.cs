//
//  FilterShader.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2016 jp
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace Tetra
{
	public class FilterShader : Shader
	{
		protected static GGL.vaoMesh quad;

		protected int 	resolutionLocation;

		protected int width, height;
		protected int tex, fbo;
		protected bool clear = true;

		protected DrawBuffersEnum[] drawBuffs;

		Vector2 resolution;

		public FilterShader (string vertResPath, string fragResPath = null, int _width = -1, int _height = -1, int initTex = 0)
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

			Resolution = new Vector2 (width, height);
			MVP = OpenTK.Matrix4.CreateOrthographicOffCenter(-0.5f, 0.5f, -0.5f, 0.5f, 1, -1);

			if (quad == null)
				quad = new vaoMesh (0, 0, 0, 1, 1, 1, 1);
		}
		public override void Enable ()
		{
			base.Enable ();
		}
		public virtual int OutputTex { get { return tex; } set { tex = value; }}

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

			if (!GL.IsTexture (tex))
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
			quad.Render (PrimitiveType.TriangleStrip);

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

			if (clear)
				GL.ClearColor(clearCols[0], clearCols[1], clearCols[2], clearCols[3]);
			GL.Viewport (viewport [0], viewport [1], viewport [2], viewport [3]);
		}
		#endregion

		public Vector2 Resolution { set { resolution = value; }}

		protected override void GetUniformLocations ()
		{
			base.GetUniformLocations ();

			resolutionLocation = GL.GetUniformLocation(pgmId, "resolution");
		}
		public override void Dispose ()
		{
			base.Dispose ();

			if (GL.IsTexture(tex))
				GL.DeleteTexture (tex);
			if (GL.IsFramebuffer(fbo))
				GL.DeleteFramebuffer (fbo);
		}
	}
	}
}

