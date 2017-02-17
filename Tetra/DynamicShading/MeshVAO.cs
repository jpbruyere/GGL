using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL;
//using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Diagnostics;
//using System.Linq;
using System.Reflection;

namespace Tetra.DynamicShading
{
	/// <summary>
	/// Vertex Array Object
	/// </summary>
	public class MeshVAO<T> : IDisposable where T : struct
	{
		public int vaoHandle,
		positionVboHandle,
		eboHandle;
		int[] vboHandles;

		int verticesCount, indicesCount;

		public int VerticesCount { get { return verticesCount;	}}
		public int IndicesCount { get { return indicesCount;	}}

		#region CTOR
		public MeshVAO()
		{
		}

		public MeshVAO(Mesh<T> _mesh){
			if (_mesh == null)
				return;
			CreateBuffers (_mesh);
		}
		#endregion

		public void CreateBuffers(Mesh<T> _mesh){
			verticesCount = _mesh.Positions.Length;
			if (_mesh.Indices != null)
				indicesCount = _mesh.Indices.Length;
			CreateVBOs (_mesh);
			CreateVAOs ();
		}
		protected void CreateVBOs(Mesh<T> _mesh)
		{
			positionVboHandle = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
			GL.BufferData<Vector3>(BufferTarget.ArrayBuffer,
				new IntPtr(_mesh.Positions.Length * Vector3.SizeInBytes),
				_mesh.Positions, BufferUsageHint.StaticDraw);

			FieldInfo[] DataFields = typeof(T).GetFields ();
			vboHandles = new int[DataFields.Length];

			for (int i = 0; i < DataFields.Length; i++) {
				FieldInfo fi = DataFields [i];

				Array vaoData = (Array)fi.GetValue (_mesh.Datas);
				vboHandles [i] = GL.GenBuffer ();
				GL.BindBuffer (BufferTarget.ArrayBuffer, vboHandles [i]);

				GCHandle pinnedArray = GCHandle.Alloc(vaoData, GCHandleType.Pinned);
				IntPtr pointer = pinnedArray.AddrOfPinnedObject();
				GL.BufferData (BufferTarget.ArrayBuffer,vaoData.Length * System.Runtime.InteropServices.Marshal.SizeOf (fi.FieldType.GetElementType ()),
					pointer, BufferUsageHint.StaticDraw);
				pinnedArray.Free();
			}

			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

			if (_mesh.Indices != null) {
				eboHandle = GL.GenBuffer ();
				GL.BindBuffer (BufferTarget.ElementArrayBuffer, eboHandle);
				GL.BufferData (BufferTarget.ElementArrayBuffer,
					new IntPtr (sizeof(ushort) * _mesh.Indices.Length),
					_mesh.Indices, BufferUsageHint.StaticDraw);
				GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
			}
		}
		protected virtual void CreateVAOs()
		{
			vaoHandle = GL.GenVertexArray();
			GL.BindVertexArray(vaoHandle);

			GL.EnableVertexAttribArray(0);
			GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
			GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, true, Vector3.SizeInBytes, 0);

			int vaPtr = 1;
			foreach (FieldInfo fi in typeof(T).GetFields()) {
				GL.EnableVertexAttribArray (vaPtr);
				GL.BindBuffer (BufferTarget.ArrayBuffer, vboHandles[vaPtr-1]);

				VertexAttribute vAttrib = (VertexAttribute)fi.GetCustomAttribute (typeof(VertexAttribute));

				if (vAttrib != null)
					GL.VertexAttribPointer (vaPtr, vAttrib.NbComponents, vAttrib.PointerType, vAttrib.Normalized, 0, 0);
				else {
					if (fi.FieldType.GetElementType () == typeof(Vector2))
						GL.VertexAttribPointer (vaPtr, 2, VertexAttribPointerType.Float, true, 0, 0);
					else if (fi.FieldType.GetElementType () == typeof(Vector2h))
						GL.VertexAttribPointer (vaPtr, 2, VertexAttribPointerType.HalfFloat, true, 0, 0);
					else if (fi.FieldType.GetElementType () == typeof(Vector3))
						GL.VertexAttribPointer (vaPtr, 3, VertexAttribPointerType.Float, true, 0, 0);
					else if (fi.FieldType.GetElementType () == typeof(Vector3h))
						GL.VertexAttribPointer (vaPtr, 3, VertexAttribPointerType.HalfFloat, true, 0, 0);
					else if (fi.FieldType.GetElementType () == typeof(Vector4))
						GL.VertexAttribPointer (vaPtr, 4, VertexAttribPointerType.Float, true, 0, 0);
				}

				vaPtr++;
			}

			if (indicesCount > 0)
				GL.BindBuffer(BufferTarget.ElementArrayBuffer, eboHandle);

			GL.BindVertexArray(0);
		}

		public void Bind(){
			GL.BindVertexArray (vaoHandle);
		}
		public void Unbind(){
			GL.BindVertexArray (0);
		}
		public void Render(BeginMode _primitiveType){
			if (indicesCount == 0)
				GL.DrawArrays (_primitiveType, 0, verticesCount);
			else
				GL.DrawElements(_primitiveType, indicesCount,
					DrawElementsType.UnsignedShort, IntPtr.Zero);
		}
		public void Render(BeginMode _primitiveType, int[] _customIndices){
			GL.DrawElements(_primitiveType, _customIndices.Length,
				DrawElementsType.UnsignedInt, _customIndices);
		}
		public void Render(BeginMode _primitiveType, int instances){
			GL.DrawElementsInstanced(_primitiveType, indicesCount,
				DrawElementsType.UnsignedInt, IntPtr.Zero, instances);
		}
		public void Render(BeginMode _primitiveType, MeshPointer item){
			GL.DrawElementsBaseVertex (_primitiveType, item.IndicesCount,
				DrawElementsType.UnsignedShort, new IntPtr (item.Offset * sizeof(ushort)), item.BaseVertex);
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			GL.DeleteBuffer (positionVboHandle);
			foreach (int vboH in vboHandles)
				GL.DeleteBuffer (vboH);
			GL.DeleteBuffer (eboHandle);
			GL.DeleteVertexArray (vaoHandle);
		}
		#endregion
	}

}
