//
//  VAOItem.cs
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
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Tetra
{
	public class VAOItem : IDisposable
	{
		public int instancesVboId;

		public int BaseVertex;
		public int IndicesCount;
		public int IndicesOffset;

		public Matrix4[] modelMats;


		public VAOItem ()
		{
			instancesVboId = GL.GenBuffer ();
		}

		public void UpdateInstancesData()
		{
			if (modelMats != null) {				
				GL.BindBuffer (BufferTarget.ArrayBuffer, instancesVboId);
				GL.BufferData<Matrix4> (BufferTarget.ArrayBuffer,
					new IntPtr (modelMats.Length * Vector4.SizeInBytes * 4),
					modelMats, BufferUsageHint.DynamicDraw);

//				for (int i = 0; i < 4; i++) {
//					GL.BindVertexBuffer(0, instancesVboId, Vector4.SizeInBytes * i, Vector4.SizeInBytes * 4);
//					GL.VertexAttribBinding(3+i, i);

//					GL.EnableVertexAttribArray (3 + i);	
//					GL.VertexAttribPointer (3+i, 4, VertexAttribPointerType.Float, false, Vector4.SizeInBytes * 4, Vector4.SizeInBytes * i);
//					GL.VertexAttribDivisor (3+i, 1);
//				}
				GL.BindBuffer (BufferTarget.ArrayBuffer, 0);
			}			
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			GL.DeleteBuffer (instancesVboId);
		}
		#endregion
	}
}

