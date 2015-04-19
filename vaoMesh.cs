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
using System.Linq;

namespace GGL
{
	public class vaoMesh : IDisposable
	{
		public int vaoHandle,
		positionVboHandle,
		normalsVboHandle,
		texVboHandle,
		eboHandle;

		public Vector3[] positions;
		public Vector3[] normals;
		public Vector2[] texCoords;
		public int[] indices;

		public string Name = "Unamed";

		public vaoMesh()
		{
		}

		public vaoMesh (Vector3[] _positions, Vector2[] _texCoord, int[] _indices)
		{
			positions = _positions;
			texCoords = _texCoord;
			indices = _indices;

			CreateVBOs ();
			CreateVAOs ();
		}

		public vaoMesh (Vector3[] _positions, Vector2[] _texCoord, Vector3[] _normales, int[] _indices)
		{
			positions = _positions;
			texCoords = _texCoord;
			normals = _normales;
			indices = _indices;

			CreateVBOs ();
			CreateVAOs ();
		}

		public vaoMesh (float x, float y, float z, float width, float height, float TileX = 1f, float TileY = 1f)
		{
			positions =
				new Vector3[] {
				new Vector3 (x - width / 2, y + height / 2, z),
				new Vector3 (x - width / 2, y - height / 2, z),
				new Vector3 (x + width / 2, y + height / 2, z),
				new Vector3 (x + width / 2, y - height / 2, z)
			};
			texCoords =	new Vector2[] {
				new Vector2 (0, TileY),
				new Vector2 (0, 0),
				new Vector2 (TileX, TileY),
				new Vector2 (TileX, 0)
			};
			normals = new Vector3[] {
				Vector3.UnitZ,
				Vector3.UnitZ,
				Vector3.UnitZ,
				Vector3.UnitZ
			};
			indices = new int[] { 0, 1, 2, 3 };

			CreateVBOs ();
			CreateVAOs ();
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
			eboHandle = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, eboHandle);
			GL.BufferData(BufferTarget.ElementArrayBuffer,
				new IntPtr(sizeof(uint) * indices.Length),
				indices, BufferUsageHint.StaticDraw);

			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
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
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, eboHandle);

			GL.BindVertexArray(0);
		}

		public void Render(PrimitiveType _primitiveType){
			GL.BindVertexArray(vaoHandle);
			GL.DrawElements(_primitiveType, indices.Length,
				DrawElementsType.UnsignedInt, IntPtr.Zero);	
			GL.BindVertexArray (0);
		}

		public static vaoMesh operator +(vaoMesh m1, vaoMesh m2){
			if (m1 == null)
				return m2;
			if (m2 == null)
				return m1;
			
			vaoMesh res = new vaoMesh ();

			m1.Dispose ();
			m2.Dispose ();

			int offset = m1.positions.Length;

			res.positions = new Vector3[m1.positions.Length + m2.positions.Length];
			m1.positions.CopyTo (res.positions, 0);
			m1.positions.CopyTo (res.positions, m1.positions.Length);

			res.indices = new int[m1.indices.Length + m2.indices.Length];
			m1.indices.CopyTo (res.indices, 0);
			for (int i = 0; i < m2.indices.Length; i++)				
				res.indices [i + offset] = m2.indices [i] + offset;

			//TODO: implement texCoord and normals addition

			res.CreateVBOs ();
			res.CreateVAOs ();

			return res;
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

		static List<Vector3> objPositions;
		static List<Vector3> objNormals;
		static List<Vector2> objTexCoords;
		static List<Vector3> lPositions;
		static List<Vector3> lNormals;
		static List<Vector2> lTexCoords;
		static List<int> lIndices;

		public static vaoMesh Load(string fileName)
		{
			objPositions = new List<Vector3>();
			objNormals = new List<Vector3>();
			objTexCoords = new List<Vector2>();
			lPositions = new List<Vector3>();
			lNormals = new List<Vector3>();
			lTexCoords = new List<Vector2>();
			lIndices = new List<int> ();

			string name = "unamed";
			using (StreamReader Reader = new StreamReader(fileName))
			{
				Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

				string line;
				while ((line = Reader.ReadLine()) != null)
				{
					line = line.Trim(splitCharacters);
					line = line.Replace("  ", " ");

					string[] parameters = line.Split(splitCharacters);

					switch (parameters[0])
					{
					case "o":					
						name = parameters[1];
						break;
					case "p": // Point
						break;
					case "v": // Vertex
						float x = float.Parse(parameters[1]);
						float y = float.Parse(parameters[2]);
						float z = float.Parse(parameters[3]);
                    
						objPositions.Add(new Vector3(x, y, z));
						break;
					case "vt": // TexCoord
						float u = float.Parse(parameters[1]);
						float v = float.Parse(parameters[2]);
						objTexCoords.Add(new Vector2(u, v));
						break;

					case "vn": // Normal
						float nx = float.Parse(parameters[1]);
						float ny = float.Parse(parameters[2]);
						float nz = float.Parse(parameters[3]);
						objNormals.Add(new Vector3(nx, ny, nz));
						break;

					case "f":
						switch (parameters.Length)
						{
						case 4:

							lIndices.Add(ParseFaceParameter(parameters[1]));
							lIndices.Add(ParseFaceParameter(parameters[2]));
							lIndices.Add(ParseFaceParameter(parameters[3]));
							break;

						case 5:
							lIndices.Add(ParseFaceParameter(parameters[1]));
							lIndices.Add(ParseFaceParameter(parameters[2]));
							lIndices.Add(ParseFaceParameter(parameters[3]));
							lIndices.Add(ParseFaceParameter(parameters[4]));
							break;
						}
						break;

					case "usemtl":
						Debug.WriteLine ("usemtl: {0}", parameters [1]); 
//						if (parameters.Length > 1)
//							name = parameters[1];
//
//						currentFaceGroup.material = model.materials.Find(
//							delegate(Material m)
//							{
//								return m.Name == name;
//							});

						break;
					case "mtllib":
						Debug.WriteLine ("usemtl: {0}", parameters [1]); 
//							model.mtllib = parameters[1];
//							string mtlPath = System.IO.Path.GetDirectoryName(fileName)
//								+ System.IO.Path.DirectorySeparatorChar
//								+ model.mtllib;
//
//							if (System.IO.File.Exists(mtlPath))
//							{
//								model.materials = ObjMeshLoader.LoadMtl(mtlPath);
//
//								//mesh.materials[0].InitMaterial();
//							}
						break;
					case "#":
//						if (parameters.Length > 1)
//						{
//							if (parameters[1] == "object")
//							{
//								if (currentMesh != null)
//								{
//									if (currentFaceGroup != null)
//									{
//										currentFaceGroup.Triangles = triangles.ToArray();
//										currentFaceGroup.Quads = quads.ToArray();
//
//										currentMesh.Faces.Add(currentFaceGroup);
//									}
//
//									currentMesh.Vertices = objVertices.ToArray();
//									objVertices.Clear();
//
//									faces.Add(currentMesh);
//								}
//								currentMesh = new Mesh();
//								currentMesh.name = parameters[2];
//							}
//						}
						break;
					}
				}

//				if (currentFaceGroup != null)
//				{
//					currentFaceGroup.Triangles = triangles.ToArray();
//					currentFaceGroup.Quads = quads.ToArray();
//					currentMesh.Faces.Add(currentFaceGroup);
//				}
//				if (currentMesh != null)
//				{
//					currentMesh.Vertices = objVertices.ToArray();
//					faces.Add(currentMesh);
//				}
//				model.meshes.Add(faces.ToArray());
			}

			vaoMesh tmp = new vaoMesh(lPositions.ToArray (),lTexCoords.ToArray (),
				lNormals.ToArray (),lIndices.ToArray ());

			tmp.Name = name;

			objPositions.Clear();
			objNormals.Clear();
			objTexCoords.Clear();
			lPositions.Clear();
			lNormals.Clear();
			lTexCoords.Clear();
			lIndices.Clear();

			return tmp;
		}

//		public static List<Material> LoadMtl(string fileName)
//		{
//			using (StreamReader streamReader = new StreamReader(fileName))
//			{
//				return LoadMtl(streamReader);
//			}
//		}

		static char[] splitCharacters = new char[] { ' ' };



		static int ParseFaceParameter(string faceParameter)
		{
			Vector3 vertex = new Vector3();
			Vector2 texCoord = new Vector2();
			Vector3 normal = new Vector3();

			string[] parameters = faceParameter.Split(faceParamaterSplitter);

			int vertexIndex = int.Parse(parameters[0]);
			if (vertexIndex < 0) vertexIndex = objPositions.Count + vertexIndex;
			else vertexIndex = vertexIndex - 1;
			vertex = objPositions[vertexIndex];

			if (parameters.Length > 1)
			{
				int texCoordIndex;
				if (int.TryParse(parameters[1], out texCoordIndex))
				{
					if (texCoordIndex < 0) texCoordIndex = objTexCoords.Count + texCoordIndex;
					else texCoordIndex = texCoordIndex - 1;
					texCoord = objTexCoords[texCoordIndex];
				}
			}

			if (parameters.Length > 2)
			{
				int normalIndex;
				if (int.TryParse(parameters[2], out normalIndex))
				{
					if (normalIndex < 0) normalIndex = objNormals.Count + normalIndex;
					else normalIndex = normalIndex - 1;
					normal = objNormals[normalIndex];
				}
			}


			lPositions.Add(vertex);
			lTexCoords.Add(texCoord);
			lNormals.Add(normal);


			int index = lPositions.Count-1;
			return index;

			//if (objVerticesIndexDictionary.TryGetValue(newObjVertex, out index))
			//{
			//    return index;
			//}
			//else
			//{
			//    objVertices.Add(newObjVertex);
			//    objVerticesIndexDictionary[newObjVertex] = objVertices.Count - 1;
			//    return objVertices.Count - 1;
			//}
		}
//		static List<Material> LoadMtl(TextReader textReader)
//		{
//			Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
//			List<Material> Materials = new List<Material>();
//			Material currentMat = null;
//
//			string line;
//			while ((line = textReader.ReadLine()) != null)
//			{
//				line = line.Trim(splitCharacters);
//				line = line.Replace("  ", " ");
//
//				string[] parameters = line.Split(splitCharacters);
//
//				switch (parameters[0])
//				{
//				case "newmtl":
//					if (currentMat != null)
//						Materials.Add(currentMat);
//					currentMat = new Material();
//					if (parameters.Length > 1)
//						currentMat.Name = parameters[1];
//					break;
//				case "Ka":
//					currentMat.Ambient = new Color (
//						float.Parse(parameters[1]),
//						float.Parse(parameters[2]),
//						float.Parse(parameters[3]),1.0f
//					);
//					break;
//				case "Kd":
//					currentMat.Diffuse = new Color (
//						float.Parse(parameters[1]),
//						float.Parse(parameters[2]),
//						float.Parse(parameters[3]),1.0f
//					);
//					break;
//				case "Ks":
//					currentMat.Specular = new Color (
//						float.Parse(parameters[1]),
//						float.Parse(parameters[2]),
//						float.Parse(parameters[3]),1.0f
//					);
//					break;
//				case "d":
//				case "Tr":
//					currentMat.Transparency = float.Parse(parameters[1]);
//					break;
//				case "map_Ka":
//					currentMat.AmbientMap = new Texture(parameters[parameters.Length - 1]);
//					break;
//				case "map_Kd":
//					currentMat.DiffuseMap = new Texture(parameters[parameters.Length - 1]);
//					break;
//				case "map_Ks":
//					currentMat.SpecularMap = new Texture(parameters[parameters.Length - 1]);
//					break;
//				case "map_Ns":
//					currentMat.SpecularHighlightMap = new Texture(parameters[parameters.Length - 1]);
//					break;
//				case "map_d":
//					currentMat.AlphaMap = new Texture(parameters[parameters.Length - 1]);
//					break;
//				case "map_bump":
//				case "bump":
//					currentMat.BumpMap = new Texture(parameters[parameters.Length - 1]);
//					break;
//				case "disp":
//					currentMat.DisplacementMap = new Texture(parameters[parameters.Length - 1]);
//					break;
//				case "decal":
//					currentMat.StencilDecalMap = new Texture(parameters[parameters.Length - 1]);
//					break;
//				}
//
//			}
//
//			if (currentMat != null)
//				Materials.Add(currentMat);
//
//			return Materials;
//		}
//

		static char[] faceParamaterSplitter = new char[] { '/' };
    
	}

}
