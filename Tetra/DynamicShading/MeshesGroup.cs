//
//  Meshes.cs
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
using OpenTK;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Tetra.DynamicShading
{
	[Serializable]
	public struct MeshPointer
	{
		public int BaseVertex;
		public int IndicesCount;
		public int Offset;
		public MeshPointer(int indiceCount, int baseVertex, int offset){
			BaseVertex = baseVertex;
			IndicesCount = indiceCount;
			Offset = offset;
		}
	}
	/// <summary>
	/// Conbined meshes to use with BaseVertex drawing
	/// </summary>
	[Serializable]
	public class MeshesGroup<T> : Mesh<T> where T : struct
	{
		public MeshPointer[] meshesPointers = new MeshPointer[0];

		public MeshesGroup ()
		{
		}
		public MeshPointer Add(Mesh<T> mesh)
		{
			List<MeshPointer> MeshesPointers = new List<MeshPointer>(meshesPointers);

			MeshPointer ptrMesh = new MeshPointer (mesh.Indices.Length,0,0);

			if (MeshesPointers.Count == 0) {
				Positions = mesh.Positions;
				foreach (FieldInfo fi in mesh.DataType.GetFields()) {
					object dataTmp = Datas;
					fi.SetValue (dataTmp, fi.GetValue (mesh.Datas));
					Datas = (T)dataTmp;
				}
				Indices = mesh.Indices;
			} else {

				ptrMesh.BaseVertex = Positions.Length;
				ptrMesh.Offset = Indices.Length;

				Vector3[] tmpPositions;
				ushort[] tmpIndices;


				tmpPositions = new Vector3[Positions.Length + mesh.Positions.Length];
				Positions.CopyTo (tmpPositions, 0);
				mesh.Positions.CopyTo (tmpPositions, Positions.Length);
				Positions = tmpPositions;

				foreach (FieldInfo fi in mesh.DataType.GetFields()) {
					Array meshData = (Array)fi.GetValue (mesh.Datas);
					Array vaoData = (Array)fi.GetValue (Datas);

					object tmp = Activator.CreateInstance (fi.FieldType, new object[] { vaoData.Length + meshData.Length });
					vaoData.CopyTo (tmp as Array, 0);
					meshData.CopyTo (tmp as Array, vaoData.Length);
					object dataTmp = Datas;
					fi.SetValue (dataTmp, tmp);
					Datas = (T)dataTmp;
				}

				tmpIndices = new ushort[Indices.Length + mesh.Indices.Length];
				Indices.CopyTo (tmpIndices, 0);
				mesh.Indices.CopyTo (tmpIndices, Indices.Length);
				Indices = tmpIndices;
			}
			MeshesPointers.Add (ptrMesh);
			meshesPointers = MeshesPointers.ToArray ();
			return ptrMesh;
		}

		public static MeshesGroup<T> LoadBinary (string fileName){
			using (Stream stream = GGL.FileSystemHelpers.GetStreamFromPath (fileName)) {
				BinaryFormatter formatter = new BinaryFormatter();
				return (MeshesGroup<T>)formatter.Deserialize (stream);
			}
		}
	}
}

