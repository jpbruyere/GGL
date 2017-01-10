//
//  InstancedVAO.cs
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
using OpenTK;

namespace Tetra.DynamicShading
{
	public class InstancedVAO<T,U> : MeshVAO<T>
		where T : struct
		where U : struct
	{
		public int InstancesDataTypeLengthInBytes;
		public int InstanceAttributeStartingIndex;
		public InstancedVAO (Mesh<T> _mesh, int _instanceAttributeStartingIndex = 4)
		{
			InstancesDataTypeLengthInBytes = Marshal.SizeOf(typeof(U));
			InstanceAttributeStartingIndex = _instanceAttributeStartingIndex;
			if (_mesh == null)
				return;
			CreateBuffers (_mesh);
		}

		protected override void CreateVAOs ()
		{
			base.CreateVAOs ();

			GL.BindVertexArray(vaoHandle);
			int dataStructSize = Marshal.SizeOf (typeof(U));
			int nbSubBuf = Math.Min(GL.GetInteger(GetPName.MaxVertexAttribs)-InstanceAttributeStartingIndex, dataStructSize / 4);
			GL.VertexBindingDivisor (InstanceAttributeStartingIndex, 1);
			for (int i = 0; i < nbSubBuf; i++) {
				GL.EnableVertexAttribArray (InstanceAttributeStartingIndex + i);
				GL.VertexAttribBinding (InstanceAttributeStartingIndex+i, InstanceAttributeStartingIndex);
				GL.VertexAttribFormat(InstanceAttributeStartingIndex+i, 4, VertexAttribType.Float, false, Vector4.SizeInBytes * i);
			}
			GL.BindVertexArray(0);
		}
		public void Render(BeginMode _primitiveType, MeshPointer item, InstancesVBO<U> instancesBuff, int firstInstance, int instancesCount){
			GL.BindVertexBuffer (InstanceAttributeStartingIndex, instancesBuff.VboId, (IntPtr)(firstInstance * InstancesDataTypeLengthInBytes), InstancesDataTypeLengthInBytes);
			GL.DrawElementsInstancedBaseVertex(_primitiveType, item.IndicesCount,
				DrawElementsType.UnsignedShort, new IntPtr(item.Offset*sizeof(ushort)),
				instancesCount, item.BaseVertex);
		}
		public void Render(BeginMode _primitiveType, MeshPointer item, InstancesVBO<U> instancesBuff){
			GL.BindVertexBuffer (InstanceAttributeStartingIndex, instancesBuff.VboId, IntPtr.Zero, InstancesDataTypeLengthInBytes);
			GL.DrawElementsInstancedBaseVertex(_primitiveType, item.IndicesCount,
				DrawElementsType.UnsignedShort, new IntPtr(item.Offset*sizeof(ushort)),
				instancesBuff.InstancedDatas.Length, item.BaseVertex);
		}
	}
}

