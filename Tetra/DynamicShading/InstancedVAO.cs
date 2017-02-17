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
using System.Reflection;

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
			GL.VertexBindingDivisor (InstanceAttributeStartingIndex, 1);

			int vaPtr = InstanceAttributeStartingIndex;
			int offset = 0;
			foreach (FieldInfo fi in typeof(U).GetFields()) {
				GL.EnableVertexAttribArray (vaPtr);
				GL.VertexAttribBinding (vaPtr, InstanceAttributeStartingIndex);

				VertexAttribute vAttrib = (VertexAttribute)fi.GetCustomAttribute (typeof(VertexAttribute));

				if (vAttrib != null){

					GL.VertexAttribFormat (vaPtr, vAttrib.NbComponents,getVATfromVAPT(vAttrib.PointerType), vAttrib.Normalized, offset);
					offset += Marshal.SizeOf (fi.FieldType.GetElementType ()) * vAttrib.NbComponents;
				}else {
					if (fi.FieldType == typeof(Vector2)) {
						GL.VertexAttribFormat (vaPtr, 2, VertexAttribType.Float, false, offset);
						offset += Vector2.SizeInBytes;
					} else if (fi.FieldType == typeof(Vector2h)) {
						GL.VertexAttribFormat (vaPtr, 2, VertexAttribType.HalfFloat, false, offset);
						offset += Vector2h.SizeInBytes;
					} else if (fi.FieldType == typeof(Vector3)) {
						GL.VertexAttribFormat (vaPtr, 3, VertexAttribType.Float, false, offset);
						offset += Vector3.SizeInBytes;
					} else if (fi.FieldType == typeof(Vector3h)) {
						GL.VertexAttribFormat (vaPtr, 3, VertexAttribType.HalfFloat, false, offset);
						offset += Vector3h.SizeInBytes;
					} else if (fi.FieldType == typeof(Vector4)) {
						GL.VertexAttribFormat (vaPtr, 4, VertexAttribType.Float, false, offset);
						offset += Vector4.SizeInBytes;
					}else if (fi.FieldType == typeof(Matrix4)) {
						GL.VertexAttribFormat (vaPtr, 4, VertexAttribType.Float, false, offset);

						for (int i = 1; i < 4; i++) {
							offset += Vector4.SizeInBytes;
							vaPtr++;
							GL.EnableVertexAttribArray (vaPtr);
							GL.VertexAttribBinding (vaPtr, InstanceAttributeStartingIndex);
							GL.VertexAttribFormat (vaPtr, 4, VertexAttribType.Float, false, offset);
						}

						offset += Vector4.SizeInBytes;
					}
				}

				vaPtr++;
			}

			GL.BindVertexArray(0);
		}
		static VertexAttribType getVATfromVAPT(VertexAttribPointerType vapt){
			switch (vapt) {
			case VertexAttribPointerType.Byte:
				return VertexAttribType.Byte;
			case VertexAttribPointerType.UnsignedByte:
				return VertexAttribType.UnsignedByte;
			case VertexAttribPointerType.Short:
				return VertexAttribType.Short;
			case VertexAttribPointerType.UnsignedShort:
				return VertexAttribType.UnsignedShort;
			case VertexAttribPointerType.Int:
				return VertexAttribType.Int;
			case VertexAttribPointerType.UnsignedInt:
				return VertexAttribType.UnsignedInt;
			case VertexAttribPointerType.Float:
				return VertexAttribType.Float;
			case VertexAttribPointerType.Double:
				return VertexAttribType.Double;
			case VertexAttribPointerType.HalfFloat:
				return VertexAttribType.HalfFloat;
			case VertexAttribPointerType.Fixed:
				return VertexAttribType.Fixed;
			case VertexAttribPointerType.UnsignedInt2101010Rev:
				return VertexAttribType.UnsignedInt2101010Rev;
			case VertexAttribPointerType.Int2101010Rev:
				return VertexAttribType.Int2101010Rev;
			}
			return default(VertexAttribType);
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
		public void Render(InstancedModel<U> im){
			GL.BindTexture(TextureTarget.Texture2D,im.Diffuse);
			Render (im.PrimitiveType, im.VAOPointer,im.Instances);
		}
	}
}

