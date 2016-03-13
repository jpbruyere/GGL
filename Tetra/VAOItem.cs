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
using System.Runtime.InteropServices;

namespace Tetra
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct VAOInstancedData
	{
		public Matrix4 modelMats;
	}
	public class VAOItem 
	{
		public int instancesVboId;

		public int BaseVertex;
		public int IndicesCount;
		public int IndicesOffset;

		public int DiffuseTexture;
		public int NormalMapTexture;

		public VAOItem(){
			
		}
	}
	public class VAOItem<T> : VAOItem, IDisposable where T : struct
	{

		public T[] Datas;
		public int InstanceDataLengthInBytes;

		public VAOItem () : base()
		{
			InstanceDataLengthInBytes = Marshal.SizeOf(typeof(T));
			instancesVboId = GL.GenBuffer ();
		}

		public void AddInstance(T modelMat)
		{
			T[] tmp = new T[Datas.Length + 1];
			Array.Copy (Datas, tmp, Datas.Length);
			tmp [Datas.Length] = modelMat;
			Datas = tmp;
			UpdateInstancesData ();
		}
		public int AddInstance()
		{
			T[] tmp = new T[Datas.Length + 1];
			Array.Copy (Datas, tmp, Datas.Length);
			Datas = tmp;
			UpdateInstancesData ();
			return Datas.Length - 1;
		}
		public void RemoveInstance(int index)
		{
			T[] tmp = new T[Datas.Length - 1];
			if (index > 0)
				Array.Copy (Datas, tmp, index);
			if (index < Datas.Length - 1)
				Array.Copy (Datas, index + 1, tmp, index, Datas.Length - 1 - index);
			Datas = tmp;
			UpdateInstancesData ();
		}

		public void UpdateInstancesData()
		{
			if (Datas != null) {				
				GL.BindBuffer (BufferTarget.ArrayBuffer, instancesVboId);
				GL.BufferData<T> (BufferTarget.ArrayBuffer,
					new IntPtr (Datas.Length * InstanceDataLengthInBytes),
					Datas, BufferUsageHint.DynamicDraw);
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

