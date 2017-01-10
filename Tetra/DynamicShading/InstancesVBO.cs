//
//  InstancesVBO.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2017 jp
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
using System.Runtime.InteropServices;

namespace Tetra.DynamicShading
{
	public class InstancesVBO<U> : IDisposable where U : struct
	{
		//public T Datas;
		public U[] InstancedDatas;
		int instancesDataTypeLengthInBytes;
		public int VboId;


		public InstancesVBO ()
		{
			VboId = GL.GenBuffer ();
		}
		public InstancesVBO (U[] Datas) : this(){
			InstancedDatas = Datas;
			instancesDataTypeLengthInBytes = Marshal.SizeOf(typeof(U));
			UpdateVBO ();
		}

		int minDirty=0,maxDirty=0;

		public void UpdateInstance(int index, U data){
			if (index < minDirty)
				minDirty = index;
			if (index > maxDirty)
				maxDirty = index;
			InstancedDatas [index] = data;
		}
		public void AddInstance(U instData)
		{
			U[] tmp = new U[InstancedDatas.Length + 1];
			Array.Copy (InstancedDatas, tmp, InstancedDatas.Length);
			tmp [InstancedDatas.Length] = instData;
			InstancedDatas = tmp;
		}
		public int AddInstance()
		{
			U[] tmp = new U[InstancedDatas.Length + 1];
			Array.Copy (InstancedDatas, tmp, InstancedDatas.Length);
			InstancedDatas = tmp;
			return InstancedDatas.Length - 1;
		}
		public void RemoveInstance(int index)
		{
			U[] tmp = new U[InstancedDatas.Length - 1];
			if (index > 0)
				Array.Copy (InstancedDatas, tmp, index);
			if (index < InstancedDatas.Length - 1)
				Array.Copy (InstancedDatas, index + 1, tmp, index, InstancedDatas.Length - 1 - index);
			InstancedDatas = tmp;
		}

		public void UpdateVBO()
		{
			if (InstancedDatas != null) {
				GL.BindBuffer (BufferTarget.ArrayBuffer, VboId);
				GL.BufferData<U> (BufferTarget.ArrayBuffer,
					new IntPtr (InstancedDatas.Length * instancesDataTypeLengthInBytes),
					InstancedDatas, BufferUsageHint.DynamicDraw);
				GL.BindBuffer (BufferTarget.ArrayBuffer, 0);
			}
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			GL.DeleteBuffer (VboId);
		}
		#endregion
	}
}

