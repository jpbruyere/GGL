using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Reflection;

namespace Tetra
{
	public class IndexedVAO : IndexedVAO<VAOInstancedData>{}

	public class IndexedVAO<V> : IDisposable where V : struct
	{
		public const int instanceBufferIndex = 4;

		int	vaoHandle,
			positionVboHandle,
			texVboHandle,
			normalsVboHandle,
			tangentsVboHandle,
			eboHandle;

		Vector3[] positions;
		Vector3[] normals;
		Vector3[] tangents;
		Vector2[] texCoords;
		ushort[] indices;

		public List<VAOItem<V>> Meshes = new List<VAOItem<V>>();

		public IndexedVAO(){
		}

		public VAOItem<V> Add(Mesh mesh)
		{
			VAOItem<V> vaoi = new VAOItem<V> ();

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

			Vector3[] tmpPositions;
			Vector3[] tmpNormals;
			Vector2[] tmpTexCoords;
			ushort[] tmpIndices;


			tmpPositions = new Vector3[positions.Length + mesh.Positions.Length];
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
			GL.BufferData<Vector3>(BufferTarget.ArrayBuffer,
				new IntPtr(positions.Length * Vector3.SizeInBytes),
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

			if (tangents != null) {
				tangentsVboHandle = GL.GenBuffer ();
				GL.BindBuffer (BufferTarget.ArrayBuffer, tangentsVboHandle);
				GL.BufferData<Vector3> (BufferTarget.ArrayBuffer,
					new IntPtr (tangents.Length * Vector3.SizeInBytes),
					tangents, BufferUsageHint.StaticDraw);
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
			if (tangents != null) {
				GL.EnableVertexAttribArray (3);
				GL.BindBuffer (BufferTarget.ArrayBuffer, tangentsVboHandle);
				GL.VertexAttribPointer (3, 3, VertexAttribPointerType.Float, true, Vector3.SizeInBytes, 0);
			}

			int dataStructSize = Marshal.SizeOf (typeof(V));
			int nbSubBuf = dataStructSize / 4;
			GL.VertexBindingDivisor (instanceBufferIndex, 1);
			for (int i = 0; i < nbSubBuf; i++) {
				GL.EnableVertexAttribArray (instanceBufferIndex + i);
				GL.VertexAttribBinding (instanceBufferIndex+i, instanceBufferIndex);
				GL.VertexAttribFormat(instanceBufferIndex+i, 4, VertexAttribType.Float, false, Vector4.SizeInBytes * i);
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
			foreach (VAOItem<V> item in Meshes)
				Render (_primitiveType, item, 0, item.Datas.Length);
		}
		public void Render(PrimitiveType _primitiveType, int[] vaoItemIndexes){
			foreach (int i in vaoItemIndexes)
				Render (_primitiveType, Meshes [i], 0, Meshes[i].Datas.Length);
		}
		public void Render(PrimitiveType _primitiveType, VAOItem<V> item){
			Render (_primitiveType, item, 0, item.Datas.Length);
		}
		public void Render(PrimitiveType _primitiveType, VAOItem<V> item, int firstInstance, int instancesCount){
			GL.ActiveTexture (TextureUnit.Texture1);
			GL.BindTexture (TextureTarget.Texture2D, item.NormalMapTexture);
			GL.ActiveTexture (TextureUnit.Texture0);
			GL.BindTexture (TextureTarget.Texture2D, item.DiffuseTexture);
			GL.BindVertexBuffer (instanceBufferIndex, item.instancesVboId, (IntPtr)(firstInstance * item.InstanceDataLengthInBytes), item.InstanceDataLengthInBytes);
			GL.DrawElementsInstancedBaseVertex(_primitiveType, item.IndicesCount,
				DrawElementsType.UnsignedShort, new IntPtr(item.IndicesOffset*sizeof(ushort)),
				instancesCount, item.BaseVertex);
		}
		public void Unbind(){
			GL.BindVertexArray (0);
		}


		public void ComputeTangents(){
			tangents = new Vector3[indices.Length];

			for (int i = 0 ; i < indices.Length; i += 3) {


				Vector3 Edge1 = positions[indices[i+1]] - positions[indices[i]];
				Vector3 Edge2 = positions[indices[i+2]] - positions[indices[i]];

				float DeltaU1 = texCoords[indices[i+1]].X - texCoords[indices[i]].X;
				float DeltaV1 = texCoords[indices[i+1]].Y - texCoords[indices[i]].Y;
				float DeltaU2 = texCoords[indices[i+2]].X - texCoords[indices[i]].X;
				float DeltaV2 = texCoords[indices[i+2]].Y - texCoords[indices[i]].Y;

				float f = 1.0f / (DeltaU1 * DeltaV2 - DeltaU2 * DeltaV1);

				Vector3 Tangent, Bitangent;

				Tangent.X = f * (DeltaV2 * Edge1.X - DeltaV1 * Edge2.X);
				Tangent.Y = f * (DeltaV2 * Edge1.Y - DeltaV1 * Edge2.Y);
				Tangent.Z = f * (DeltaV2 * Edge1.Z - DeltaV1 * Edge2.Z);

				Bitangent.X = f * (-DeltaU2 * Edge1.X - DeltaU1 * Edge2.X);
				Bitangent.Y = f * (-DeltaU2 * Edge1.Y - DeltaU1 * Edge2.Y);
				Bitangent.Z = f * (-DeltaU2 * Edge1.Z - DeltaU1 * Edge2.Z);

				tangents[indices[i]] += Tangent;
				tangents[indices[i+1]] += Tangent;
				tangents[indices[i+2]] += Tangent;
			}

			for (int i = 0 ; i < tangents.Length ; i++)
				tangents[i].Normalize();
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			GL.DeleteBuffer (positionVboHandle);
			GL.DeleteBuffer (normalsVboHandle);
			GL.DeleteBuffer (tangentsVboHandle);
			GL.DeleteBuffer (texVboHandle);
			GL.DeleteBuffer (eboHandle);
			GL.DeleteVertexArray (vaoHandle);
		}
		#endregion
	}
}

