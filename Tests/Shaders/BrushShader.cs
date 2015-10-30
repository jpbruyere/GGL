//
//  CircleShader.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2015 jp
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
using GameLib;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using GGL;

namespace Ottd3D
{
	public class BrushShader : CircleShader
	{
		#region CTOR
		public BrushShader (string effectId, int _width = -1, int _height = -1):
		base(effectId,_width,_height)
		{
			clear = false;
		}
		#endregion

		int evenTex;
		int centerLoc;

		Vector2 center = new Vector2(0.5f,0.5f);
		bool evenCycle = false;

		public Vector2 Center {
			get { return center; }
			set { center = value; }
		}
		public override int OutputTex {
			get { return evenCycle ? evenTex : tex; }
		}
		public int InputTex {
			get { return evenCycle ? tex : evenTex;}
		}
		protected override void GetUniformLocations ()
		{
			base.GetUniformLocations ();

			centerLoc = GL.GetUniformLocation(pgmId, "center");
		}
		public override void Enable ()
		{
			base.Enable ();
			GL.Uniform2(centerLoc, center);
		}

		protected override void initFbo ()
		{
			tex = new Texture (width, height);
			evenTex = new Texture (width, height);

			GL.GenFramebuffers(1, out fbo);

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
				TextureTarget.Texture2D, tex, 0);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1,
				TextureTarget.Texture2D, evenTex, 0);
			
			if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
			{
				throw new Exception(GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer).ToString());
			}

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
		}
		public override void Update ()
		{				
			evenCycle = !evenCycle;

			GL.ActiveTexture (TextureUnit.Texture0);
			if (evenCycle) {
				drawBuffs = new DrawBuffersEnum[] {	DrawBuffersEnum.ColorAttachment1 };
				GL.BindTexture (TextureTarget.Texture2D, tex);
			} else {
				drawBuffs = new DrawBuffersEnum[] {	DrawBuffersEnum.ColorAttachment0 };
				GL.BindTexture (TextureTarget.Texture2D, evenTex);
			}
				
			base.Update ();
		}

		public void Clear()
		{
			int[] viewport = new int[4];
			float[] clearCols = new float[4];

			GL.GetInteger (GetPName.Viewport, viewport);
			GL.GetFloat (GetPName.ColorClearValue, clearCols);

			GL.ClearColor (0, 0, 0, 0);
			GL.Viewport(0, 0, width, height);

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

			drawBuffs = new DrawBuffersEnum[] {	DrawBuffersEnum.ColorAttachment0 };
			GL.DrawBuffers(drawBuffs.Length, drawBuffs);
			GL.Clear (ClearBufferMask.ColorBufferBit);

			drawBuffs = new DrawBuffersEnum[] {	DrawBuffersEnum.ColorAttachment1 };
			GL.DrawBuffers(drawBuffs.Length, drawBuffs);
			GL.Clear (ClearBufferMask.ColorBufferBit);

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
			GL.DrawBuffer(DrawBufferMode.Back);

			GL.ClearColor(clearCols[0], clearCols[1], clearCols[2], clearCols[3]);
			GL.Viewport (viewport [0], viewport [1], viewport [2], viewport [3]);
		}
	}
}

