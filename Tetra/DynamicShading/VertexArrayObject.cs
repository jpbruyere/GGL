using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Tetra.DynamicShading
{
	public abstract class VertexArrayObject : IDisposable{
		public const int instanceBufferIndex = 4;

		protected int	vaoHandle,
						positionVboHandle,
						eboHandle;

		protected Vector3[] positions;
		protected ushort[] indices;

		public List<VAOItem> Meshes = new List<VAOItem>();

		#region CTOR
		public VertexArrayObject(){
			vaoHandle = GL.GenVertexArray();
		}
		#endregion

		public abstract Type VAODataType { get; }
		public abstract Type VAOInstancedDataType { get; }

		public virtual VAOItem Add(Mesh mesh){
			VAOItem vaoi = new VAOItem ();

			vaoi.IndicesCount = mesh.Indices.Length;

			if (Meshes.Count == 0) {
				positions = mesh.Positions;
				indices = mesh.Indices;
				Meshes.Add (vaoi);
				return vaoi;
			}

			vaoi.BaseVertex = positions.Length;
			vaoi.IndicesOffset = indices.Length;

			Vector3[] tmpPositions;
			ushort[] tmpIndices;

			tmpPositions = new Vector3[positions.Length + mesh.Positions.Length];
			positions.CopyTo (tmpPositions, 0);
			mesh.Positions.CopyTo (tmpPositions, positions.Length);
			positions = tmpPositions;

			tmpIndices = new ushort[indices.Length + mesh.Indices.Length];
			indices.CopyTo (tmpIndices, 0);
			mesh.Indices.CopyTo (tmpIndices, indices.Length);
			indices = tmpIndices;

			Meshes.Add (vaoi);
			return vaoi;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Bind(){
			GL.BindVertexArray(vaoHandle);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Unbind(){
			GL.BindVertexArray (0);
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			GL.DeleteVertexArray (vaoHandle);
		}
		#endregion
	}

	public class VertexArrayObject<T,U> : VertexArrayObject, IDisposable
		where T : struct
		where U : struct
	{		
		int[] vboHandles;

		T Datas;

		public VertexArrayObject() : base(){
		}

		public override VAOItem Add(Mesh _mesh)
		{
			VAOItem<U> vaoi = new VAOItem<U> ();
			Mesh<T> mesh = _mesh as Mesh<T>;

			vaoi.IndicesCount = mesh.Indices.Length;

			if (Meshes.Count == 0) {
				positions = mesh.Positions;
				foreach (FieldInfo fi in mesh.DataType.GetFields()) {					
					object dataTmp = Datas;
					fi.SetValue (dataTmp, fi.GetValue (mesh.Datas));
					Datas = (T)dataTmp;
				}
				indices = mesh.Indices;
				Meshes.Add (vaoi);
				return vaoi;
			}
				
			vaoi.BaseVertex = positions.Length;
			vaoi.IndicesOffset = indices.Length;

			Vector3[] tmpPositions;
			ushort[] tmpIndices;


			tmpPositions = new Vector3[positions.Length + mesh.Positions.Length];
			positions.CopyTo (tmpPositions, 0);
			mesh.Positions.CopyTo (tmpPositions, positions.Length);
			positions = tmpPositions;

			foreach (FieldInfo fi in mesh.DataType.GetFields()) {
				Array meshData = (Array)fi.GetValue (mesh.Datas);
				Array vaoData = (Array)fi.GetValue (Datas);

				object tmp = Activator.CreateInstance (fi.FieldType,new object[] {vaoData.Length + meshData.Length});
				vaoData.CopyTo (tmp as Array, 0);
				meshData.CopyTo (tmp as Array, vaoData.Length);
				object dataTmp = Datas;
				fi.SetValue (dataTmp, tmp);
				Datas = (T)dataTmp;
			}

			tmpIndices = new ushort[indices.Length + mesh.Indices.Length];
			indices.CopyTo (tmpIndices, 0);
			mesh.Indices.CopyTo (tmpIndices, indices.Length);
			indices = tmpIndices;

			Meshes.Add (vaoi);
			return vaoi;

//			tmpTexCoords = new Vector2[texCoords.Length + mesh.TexCoords.Length];
//			texCoords.CopyTo (tmpTexCoords, 0);
//			mesh.TexCoords.CopyTo (tmpTexCoords, texCoords.Length);
//
//			tmpNormals = new Vector3[normals.Length + mesh.Normals.Length];
//			normals.CopyTo (tmpNormals, 0);
//			mesh.Normals.CopyTo (tmpNormals, normals.Length);

//			texCoords = tmpTexCoords;
//			normals = tmpNormals;
		}

		protected void CreateVBOs()
		{
			positionVboHandle = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
			GL.BufferData<Vector3>(BufferTarget.ArrayBuffer,
				new IntPtr(positions.Length * Vector3.SizeInBytes),
				positions, BufferUsageHint.StaticDraw);

			FieldInfo[] DataFields = VAODataType.GetFields ();
			vboHandles = new int[DataFields.Length];

			ValueType Test;

			for (int i = 0; i < DataFields.Length; i++) {
				FieldInfo fi = DataFields [i];
				Array vaoData = (Array)fi.GetValue (Datas);
				vboHandles[i] = GL.GenBuffer ();
				GL.BindBuffer (BufferTarget.ArrayBuffer, vboHandles[i]);

				if (fi.FieldType.GetElementType () == typeof(Vector2)) {
					Vector2[] tmp = (Vector2[])(vaoData as object);
					GL.BufferData<Vector2> (BufferTarget.ArrayBuffer,
						new IntPtr (vaoData.Length * Vector2.SizeInBytes),
						tmp, BufferUsageHint.StaticDraw);
				} else if (fi.FieldType.GetElementType () == typeof(Vector3)) {
					Vector3[] tmp = (Vector3[])(vaoData as object);
					GL.BufferData<Vector3> (BufferTarget.ArrayBuffer,
						new IntPtr (vaoData.Length * Vector3.SizeInBytes),
						tmp, BufferUsageHint.StaticDraw);
				}
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
			GL.BindVertexArray(vaoHandle);

			GL.EnableVertexAttribArray(0);
			GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
			GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, true, Vector3.SizeInBytes, 0);

			int vaPtr = 1;
			foreach (FieldInfo fi in VAODataType.GetFields()) {
				GL.EnableVertexAttribArray (vaPtr);
				GL.BindBuffer (BufferTarget.ArrayBuffer, vboHandles[vaPtr-1]);
				if (fi.FieldType.GetElementType() == typeof(Vector2))
					GL.VertexAttribPointer (vaPtr, 2, VertexAttribPointerType.Float, true, Vector2.SizeInBytes, 0);
				else if (fi.FieldType.GetElementType() == typeof(Vector3))
					GL.VertexAttribPointer (vaPtr, 3, VertexAttribPointerType.Float, true, Vector3.SizeInBytes, 0);

				vaPtr++;
			}

			int dataStructSize = Marshal.SizeOf (typeof(U));
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
			CreateVBOs ();
			CreateVAOs ();
		}



		public void Render(PrimitiveType _primitiveType){
			foreach (VAOItem<U> item in Meshes)
				Render (_primitiveType, item, 0, item.InstancedDatas.Length);
		}
		public void Render(PrimitiveType _primitiveType, int[] vaoItemIndexes){
			foreach (int i in vaoItemIndexes)
				Render (_primitiveType, Meshes [i] as VAOItem<U>, 0, (Meshes[i] as VAOItem<U>).InstancedDatas.Length);
		}
		public void Render(PrimitiveType _primitiveType, VAOItem<U> item){
			Render (_primitiveType, item, 0, item.InstancedDatas.Length);
		}
		public void Render(PrimitiveType _primitiveType, VAOItem<U> item, int firstInstance, int instancesCount){
			GL.ActiveTexture (TextureUnit.Texture1);
			GL.BindTexture (TextureTarget.Texture2D, item.NormalMapTexture);
			GL.ActiveTexture (TextureUnit.Texture0);
			GL.BindTexture (TextureTarget.Texture2D, item.DiffuseTexture);
			GL.BindVertexBuffer (instanceBufferIndex, item.instancesVboId, (IntPtr)(firstInstance * item.InstanceDataLengthInBytes), item.InstanceDataLengthInBytes);
			GL.DrawElementsInstancedBaseVertex(_primitiveType, item.IndicesCount,
				DrawElementsType.UnsignedShort, new IntPtr(item.IndicesOffset*sizeof(ushort)),
				instancesCount, item.BaseVertex);
		}


//		public void ComputeTangents(){
//			tangents = new Vector3[indices.Length];
//
//			for (int i = 0 ; i < indices.Length; i += 3) {
//
//
//				Vector3 Edge1 = positions[indices[i+1]] - positions[indices[i]];
//				Vector3 Edge2 = positions[indices[i+2]] - positions[indices[i]];
//
//				float DeltaU1 = texCoords[indices[i+1]].X - texCoords[indices[i]].X;
//				float DeltaV1 = texCoords[indices[i+1]].Y - texCoords[indices[i]].Y;
//				float DeltaU2 = texCoords[indices[i+2]].X - texCoords[indices[i]].X;
//				float DeltaV2 = texCoords[indices[i+2]].Y - texCoords[indices[i]].Y;
//
//				float f = 1.0f / (DeltaU1 * DeltaV2 - DeltaU2 * DeltaV1);
//
//				Vector3 Tangent, Bitangent;
//
//				Tangent.X = f * (DeltaV2 * Edge1.X - DeltaV1 * Edge2.X);
//				Tangent.Y = f * (DeltaV2 * Edge1.Y - DeltaV1 * Edge2.Y);
//				Tangent.Z = f * (DeltaV2 * Edge1.Z - DeltaV1 * Edge2.Z);
//
//				Bitangent.X = f * (-DeltaU2 * Edge1.X - DeltaU1 * Edge2.X);
//				Bitangent.Y = f * (-DeltaU2 * Edge1.Y - DeltaU1 * Edge2.Y);
//				Bitangent.Z = f * (-DeltaU2 * Edge1.Z - DeltaU1 * Edge2.Z);
//
//				tangents[indices[i]] += Tangent;
//				tangents[indices[i+1]] += Tangent;
//				tangents[indices[i+2]] += Tangent;
//			}
//
//			for (int i = 0 ; i < tangents.Length ; i++)
//				tangents[i].Normalize();
//		}

		#region implemented abstract members of VertexArrayObject

		public override Type VAODataType {
			get {
				return typeof(T);
			}
		}
		public override Type VAOInstancedDataType {
			get {
				return typeof(U);
			}
		}

		#endregion

		#region IDisposable implementation
		public void Dispose ()
		{
			GL.DeleteBuffer (positionVboHandle);
			foreach (int vboH in vboHandles)
				GL.DeleteBuffer (vboH);	
			GL.DeleteBuffer (eboHandle);

			base.Dispose ();
		}
		#endregion
	}
}

