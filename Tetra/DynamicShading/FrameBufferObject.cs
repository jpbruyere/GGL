//
//  FrameBufferObject.cs
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

namespace Tetra
{
	public class FrameBufferObject : IDisposable
	{
		public int Width, Height, NbColorAttachments;
		public DrawBuffersEnum[] DrawBuffers;
		int[] texIds;
		int fboId;

		public FramebufferErrorCode Status {
			get {
				FramebufferErrorCode status;
				GL.BindFramebuffer (FramebufferTarget.Framebuffer, fboId);
				status = GL.CheckFramebufferStatus (FramebufferTarget.Framebuffer);
				GL.BindFramebuffer (FramebufferTarget.Framebuffer, 0);

				return status;
			}
		}
		public bool IsComplete{
			get{ return Status == FramebufferErrorCode.FramebufferComplete; }
		}

		#region CTOR
		public FrameBufferObject (int _width, int _height)
		{
			Width = _width;
			Height = _height;

			GL.GenFramebuffers(1, out fboId);
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboId);
			if (DrawBuffers == null)
				GL.DrawBuffer (DrawBufferMode.None);
			else
				GL.DrawBuffers(DrawBuffers.Length, DrawBuffers);
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
		}
		#endregion

		public void Bind(){
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboId);
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			GL.DeleteFramebuffer (fboId);
		}
		#endregion
	}
}

