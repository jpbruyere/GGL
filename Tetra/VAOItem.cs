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
	public class VAOItem : IDisposable
	{
		
		public int instancesVboId;

		public int BaseVertex;
		public int IndicesCount;
		public int IndicesOffset;

		public int DiffuseTexture;
		public int NormalMapTexture;

		public VAOItem(){
			
		}

		//public abstract Type DataType { get; }
		//public abstract Type InstancedDataType { get; }

		#region IDisposable implementation
		public void Dispose ()
		{
		}
		#endregion
	}
	public class VAOItem<U> : VAOItem, IDisposable
		//where T : struct
		where U : struct
	{
		public int InstanceDataLengthInBytes;

		//public T Datas;
		public U[] InstancedDatas;

		public VAOItem () : base()
		{
			instancesVboId = GL.GenBuffer ();

			InstanceDataLengthInBytes = Marshal.SizeOf(typeof(U));
		}

		public void AddInstance(U instData)
		{
			U[] tmp = new U[InstancedDatas.Length + 1];
			Array.Copy (InstancedDatas, tmp, InstancedDatas.Length);
			tmp [InstancedDatas.Length] = instData;
			InstancedDatas = tmp;
			UpdateInstancesData ();
		}
		public int AddInstance()
		{
			U[] tmp = new U[InstancedDatas.Length + 1];
			Array.Copy (InstancedDatas, tmp, InstancedDatas.Length);
			InstancedDatas = tmp;
			UpdateInstancesData ();
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
			UpdateInstancesData ();
		}

		public void UpdateInstancesData()
		{
			if (InstancedDatas != null) {				
				GL.BindBuffer (BufferTarget.ArrayBuffer, instancesVboId);
				GL.BufferData<U> (BufferTarget.ArrayBuffer,
					new IntPtr (InstancedDatas.Length * InstanceDataLengthInBytes),
					InstancedDatas, BufferUsageHint.DynamicDraw);
				GL.BindBuffer (BufferTarget.ArrayBuffer, 0);
			}			
		}

//		#region implemented abstract members of VAOItem
//		public override Type InstancedDataType {
//			get {
//				return typeof(U);
//			}
//		}
//		#endregion

		#region IDisposable implementation
		public void Dispose ()
		{
			GL.DeleteBuffer (instancesVboId);

			base.Dispose ();
		}
		#endregion
	}
}

