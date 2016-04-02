//
//  UniformBufferObject.cs
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
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;

namespace Tetra
{
	public class UniformBufferObject<T> : IDisposable where T : struct
	{
		static int dataLengthInBytes;

		public int UboId;
		public T Datas;
		public BufferUsageHint Usage;


		static UniformBufferObject(){
			dataLengthInBytes = Marshal.SizeOf(typeof(T));
			if (dataLengthInBytes > GL.GetInteger (GetPName.MaxUniformBlockSize))
				throw new Exception ("UBO Error: Uniform Block size limit reached for: " + typeof(T).Name);
		}

		public UniformBufferObject (BufferUsageHint usage = BufferUsageHint.StaticCopy)
		{
			Usage = usage;
			UboId = GL.GenBuffer ();
			Update ();
		}
		public UniformBufferObject(T datas, BufferUsageHint usage = BufferUsageHint.StaticCopy)
			: this(usage){

			Datas = datas;

			Update ();
		}
		public void Update(T datas){
			Datas = datas;
			Update ();
		}
		public void Update(){
			GL.BindBuffer (BufferTarget.UniformBuffer, UboId);
			GL.BufferData<T>(BufferTarget.UniformBuffer,dataLengthInBytes,
				ref Datas, Usage);
			GL.BindBuffer (BufferTarget.UniformBuffer, 0);
		}
		public void Bind(int globalBindingPoint){
			GL.BindBufferBase (BufferRangeTarget.UniformBuffer, globalBindingPoint, UboId);
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			GL.DeleteBuffer (UboId);
		}
		#endregion
	}
}

