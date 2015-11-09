using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Tetra
{
	public class IndexedVAO<T> : IDisposable 
		where T : struct
	{
		int	vaoHandle,
			positionVboHandle,
			texVboHandle,
			normalsVboHandle,
			eboHandle;

		T[] positions;
		Vector3[] normals;
		Vector2[] texCoords;
		ushort[] indices;

		public List<VAOItem> Meshes = new List<VAOItem>();

		public IndexedVAO(){
		}

		public VAOItem Add(Mesh<T> mesh)
		{
			VAOItem vaoi = new VAOItem ();

			vaoi.IndicesCount = mesh.Indices.Length;

			if (positions == null) {
				positions = mesh.Positions;
				texCoords = mesh.TexCoords;
				normals = mesh.Normals;
				indices = mesh.Indices;
				Meshes.Add (vaoi);
				return vaoi;
			}

			vaoi.BaseVertex = positions.Length;
			vaoi.IndicesOffset = indices.Length;
			vaoi.IndicesCount = mesh.Indices.Length;

			T[] tmpPositions;
			Vector3[] tmpNormals;
			Vector2[] tmpTexCoords;
			ushort[] tmpIndices;


			tmpPositions = new T[positions.Length + mesh.Positions.Length];
			positions.CopyTo (tmpPositions, 0);
			mesh.Positions.CopyTo (tmpPositions, positions.Length);

			tmpTexCoords = new Vector2[texCoords.Length + mesh.TexCoords.Length];
			texCoords.CopyTo (tmpTexCoords, 0);
			mesh.TexCoords.CopyTo (tmpTexCoords, texCoords.Length);

			tmpNormals = new Vector3[normals.Length + mesh.Normals.Length];
			normals.CopyTo (tmpNormals, 0);
			mesh.Normals.CopyTo (tmpNormals, normals.Length);

			tmpIndices = new ushort[indices.Length + mesh.Indices.Length];
			indices.CopyTo (tmpIndices, 0);
			mesh.Indices.CopyTo (tmpIndices, indices.Length);

			positions = tmpPositions;
			texCoords = tmpTexCoords;
			normals = tmpNormals;
			indices = tmpIndices;

			Meshes.Add (vaoi);
			return vaoi;
		}

		protected void CreateVBOs()
		{
			positionVboHandle = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
			GL.BufferData<T>(BufferTarget.ArrayBuffer,
				new IntPtr(positions.Length * Marshal.SizeOf(typeof(T))),
				positions, BufferUsageHint.StaticDraw);

			if (normals != null) {
				normalsVboHandle = GL.GenBuffer ();
				GL.BindBuffer (BufferTarget.ArrayBuffer, normalsVboHandle);
				GL.BufferData<Vector3> (BufferTarget.ArrayBuffer,
					new IntPtr (normals.Length * Vector3.SizeInBytes),
					normals, BufferUsageHint.StaticDraw);
			}

			if (texCoords != null) {
				texVboHandle = GL.GenBuffer ();
				GL.BindBuffer (BufferTarget.ArrayBuffer, texVboHandle);
				GL.BufferData<Vector2> (BufferTarget.ArrayBuffer,
					new IntPtr (texCoords.Length * Vector2.SizeInBytes),
					texCoords, BufferUsageHint.StaticDraw);
			}

			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

			if (indices != null) {
				eboHandle = GL.GenBuffer ();
				GL.BindBuffer (BufferTarget.ElementArrayBuffer, eboHandle);
				GL.BufferData (BufferTarget.ElementArrayBuffer,
					new IntPtr (sizeof(ushort) * indices.Length),
					indices, BufferUsageHint.StaticDraw);
				GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
			}
		}
		protected void CreateVAOs()
		{
			vaoHandle = GL.GenVertexArray();
			GL.BindVertexArray(vaoHandle);

			GL.EnableVertexAttribArray(0);
			GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
			if (typeof(T) == typeof(Vector2))
				GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, true, Vector2.SizeInBytes, 0);
			else if (typeof(T) == typeof(Vector3))
				GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, true, Vector3.SizeInBytes, 0);			

			if (texCoords != null) {
				GL.EnableVertexAttribArray (1);
				GL.BindBuffer (BufferTarget.ArrayBuffer, texVboHandle);
				GL.VertexAttribPointer (1, 2, VertexAttribPointerType.Float, true, Vector2.SizeInBytes, 0);
			}
			if (normals != null) {
				GL.EnableVertexAttribArray (2);
				GL.BindBuffer (BufferTarget.ArrayBuffer, normalsVboHandle);
				GL.VertexAttribPointer (2, 3, VertexAttribPointerType.Float, true, Vector3.SizeInBytes, 0);
			}

			GL.VertexBindingDivisor (3, 1);
			for (int i = 0; i < 4; i++) {					
				GL.EnableVertexAttribArray (3 + i);	
				GL.VertexAttribBinding (3+i, 3);
				GL.VertexAttribFormat(3+i, 4, VertexAttribType.Float, false, Vector4.SizeInBytes * i);
			}

			if (indices != null)
				GL.BindBuffer(BufferTarget.ElementArrayBuffer, eboHandle);

			GL.BindVertexArray(0);
		}			
			
		public void BuildBuffers(){
			Dispose ();
			CreateVBOs ();
			CreateVAOs ();
		}

		public void Bind(){
			GL.BindVertexArray(vaoHandle);
		}

		public void Render(PrimitiveType _primitiveType){
			foreach (VAOItem item in Meshes) {
				GL.ActiveTexture (TextureUnit.Texture0);
				GL.BindTexture (TextureTarget.Texture2D, item.DiffuseTexture);
				GL.BindVertexBuffer (3, item.instancesVboId, IntPtr.Zero,Vector4.SizeInBytes * 4);
				GL.DrawElementsInstancedBaseVertex(_primitiveType, item.IndicesCount, 
					DrawElementsType.UnsignedShort, new IntPtr(item.IndicesOffset*sizeof(ushort)),
					item.modelMats.Length, item.BaseVertex);
			}
		}
			
		public void Unbind(){
			GL.BindVertexArray (0);
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			GL.DeleteBuffer (positionVboHandle);
			GL.DeleteBuffer (normalsVboHandle);
			GL.DeleteBuffer (texVboHandle);
			GL.DeleteBuffer (eboHandle);
			GL.DeleteVertexArray (vaoHandle);
		}
		#endregion
	}
}

