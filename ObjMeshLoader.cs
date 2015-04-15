using System;
using System.IO;
using System.Collections.Generic;
using OpenTK;
using System.Threading;
//using System.Drawing;
using GGL;
using System.Diagnostics;
using go;

public class ObjMeshLoader
{
    private static List<Vector3> vertices = null;
    private static List<Vector3> normals = null;
    private static List<Vector2> texCoords = null;

    private static List<Vertex> objVertices = null;

    public static void Load(string fileName, ObjModel model)
    {
        bool nextVerticeIsAxe = false;
        bool firstLod = false;

        if (model.meshes.Count == 0)
            firstLod = true;

        vertices = new List<Vector3>();
        normals = new List<Vector3>();
        texCoords = new List<Vector2>();
        objVertices = new List<Vertex>();

        string oldDir = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(System.IO.Path.GetDirectoryName(fileName));

        using (StreamReader Reader = new StreamReader(fileName))
        {
            if (firstLod)//first lod
                model.Name = System.IO.Path.GetFileNameWithoutExtension(fileName);

            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            List<Triangle> triangles = new List<Triangle>();
            List<Quad> quads = new List<Quad>();

            List<Mesh> faces = new List<Mesh>();

            Mesh currentMesh = null;
            FaceGroup currentFaceGroup = null;


            string line;
            while ((line = Reader.ReadLine()) != null)
            {
                line = line.Trim(splitCharacters);
                line = line.Replace("  ", " ");

                string[] parameters = line.Split(splitCharacters);

                switch (parameters[0])
                {
                    case "o":

                        if (currentMesh != null)
                        {
                            if (currentFaceGroup != null)
                            {
                                currentFaceGroup.Triangles = triangles.ToArray();
                                currentFaceGroup.Quads = quads.ToArray();

                                currentMesh.Faces.Add(currentFaceGroup);
                            }

                            currentMesh.Vertices = objVertices.ToArray();
                            objVertices.Clear();

                            faces.Add(currentMesh);
                        }

                        if (parameters[1].StartsWith("axe"))
                        {
                            nextVerticeIsAxe = true;
                            currentMesh = null;
                        }
                        else
                        {
                            currentMesh = new Mesh();
                            currentMesh.name = parameters[1];
                        }
                        break;
                    case "p": // Point
                        break;
                    case "v": // Vertex
                        float x = float.Parse(parameters[1]);
                        float y = float.Parse(parameters[2]);
                        float z = float.Parse(parameters[3]);
                        if (nextVerticeIsAxe)
                        {
                            model.Axe = new Vector3(x, y, z);
                            nextVerticeIsAxe = false;
                        }                        
                            vertices.Add(new Vector3(x, y, z));
                        break;
                    case "vt": // TexCoord
                        float u = float.Parse(parameters[1]);
                        float v = float.Parse(parameters[2]);
                        texCoords.Add(new Vector2(u, v));
                        break;

                    case "vn": // Normal
                        float nx = float.Parse(parameters[1]);
                        float ny = float.Parse(parameters[2]);
                        float nz = float.Parse(parameters[3]);
                        normals.Add(new Vector3(nx, ny, nz));
                        break;

                    case "f":
                        switch (parameters.Length)
                        {
                            case 4:
                                Triangle objTriangle = new Triangle();
                                objTriangle.Index0 = ParseFaceParameter(parameters[1]);
                                objTriangle.Index1 = ParseFaceParameter(parameters[2]);
                                objTriangle.Index2 = ParseFaceParameter(parameters[3]);
                                triangles.Add(objTriangle);
                                break;

                            case 5:
                                Quad objQuad = new Quad();
                                objQuad.Index0 = ParseFaceParameter(parameters[1]);
                                objQuad.Index1 = ParseFaceParameter(parameters[2]);
                                objQuad.Index2 = ParseFaceParameter(parameters[3]);
                                objQuad.Index3 = ParseFaceParameter(parameters[4]);
                                quads.Add(objQuad);
                                break;
                        }
                        break;

                    case "usemtl":
                        if (currentFaceGroup != null)
                        {
                            currentFaceGroup.Triangles = triangles.ToArray();
                            currentFaceGroup.Quads = quads.ToArray();

                            triangles.Clear();
                            quads.Clear();

                            currentMesh.Faces.Add(currentFaceGroup);
                        }

                        currentFaceGroup = new FaceGroup();

                        string name = "";

                        if (parameters.Length > 1)
                            name = parameters[1];

                        currentFaceGroup.material = model.materials.Find(
                            delegate(Material m)
                            {
                                return m.Name == name;
                            });

                        break;
                    case "mtllib":
                        if (firstLod)
                        {
                            model.mtllib = parameters[1];
                            string mtlPath = System.IO.Path.GetDirectoryName(fileName)
                                + System.IO.Path.DirectorySeparatorChar
                                + model.mtllib;

                            if (System.IO.File.Exists(mtlPath))
                            {
                                model.materials = ObjMeshLoader.LoadMtl(mtlPath);

                                //mesh.materials[0].InitMaterial();
                            }
                        }
                        break;
                    case "#":
                        if (parameters.Length > 1)
                        {
                            if (parameters[1] == "object")
                            {
                                if (currentMesh != null)
                                {
                                    if (currentFaceGroup != null)
                                    {
                                        currentFaceGroup.Triangles = triangles.ToArray();
                                        currentFaceGroup.Quads = quads.ToArray();

                                        currentMesh.Faces.Add(currentFaceGroup);
                                    }

                                    currentMesh.Vertices = objVertices.ToArray();
                                    objVertices.Clear();

                                    faces.Add(currentMesh);
                                }
                                currentMesh = new Mesh();
                                currentMesh.name = parameters[2];
                            }
                        }
                        break;
                }
            }

            if (currentFaceGroup != null)
            {
                currentFaceGroup.Triangles = triangles.ToArray();
                currentFaceGroup.Quads = quads.ToArray();
                currentMesh.Faces.Add(currentFaceGroup);
            }
            if (currentMesh != null)
            {
                currentMesh.Vertices = objVertices.ToArray();
                faces.Add(currentMesh);
            }
            model.meshes.Add(faces.ToArray());
        }

        Directory.SetCurrentDirectory(oldDir);

    }
    public static List<Material> LoadMtl(string fileName)
    {
        using (StreamReader streamReader = new StreamReader(fileName))
        {
            return LoadMtl(streamReader);
        }
    }

    static char[] splitCharacters = new char[] { ' ' };

    

    static int ParseFaceParameter(string faceParameter)
    {
        Vector3 vertex = new Vector3();
        Vector2 texCoord = new Vector2();
        Vector3 normal = new Vector3();

        string[] parameters = faceParameter.Split(faceParamaterSplitter);

        int vertexIndex = int.Parse(parameters[0]);
        if (vertexIndex < 0) vertexIndex = vertices.Count + vertexIndex;
        else vertexIndex = vertexIndex - 1;
        vertex = vertices[vertexIndex];

        if (parameters.Length > 1)
        {
            int texCoordIndex;
            if (int.TryParse(parameters[1], out texCoordIndex))
            {
                if (texCoordIndex < 0) texCoordIndex = texCoords.Count + texCoordIndex;
                else texCoordIndex = texCoordIndex - 1;
                texCoord = texCoords[texCoordIndex];
            }
        }

        if (parameters.Length > 2)
        {
            int normalIndex;
            if (int.TryParse(parameters[2], out normalIndex))
            {
                if (normalIndex < 0) normalIndex = normals.Count + normalIndex;
                else normalIndex = normalIndex - 1;
                normal = normals[normalIndex];
            }
        }

        Vertex newObjVertex = new Vertex();
        newObjVertex.position = vertex;
        newObjVertex.TexCoord = texCoord;
        newObjVertex.Normal = normal;


        int index = objVertices.Count;
        objVertices.Add(newObjVertex);
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
    static List<Material> LoadMtl(TextReader textReader)
    {
        Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        List<Material> Materials = new List<Material>();
        Material currentMat = null;

        string line;
        while ((line = textReader.ReadLine()) != null)
        {
            line = line.Trim(splitCharacters);
            line = line.Replace("  ", " ");

            string[] parameters = line.Split(splitCharacters);

            switch (parameters[0])
            {
                case "newmtl":
                    if (currentMat != null)
                        Materials.Add(currentMat);
                    currentMat = new Material();
                    if (parameters.Length > 1)
                        currentMat.Name = parameters[1];
                    break;
                case "Ka":
					currentMat.Ambient = new Color (
							float.Parse(parameters[1]),
	                        float.Parse(parameters[2]),
							float.Parse(parameters[3]),1.0f
	                        );
	                    break;
                case "Kd":
					currentMat.Diffuse = new Color (
						float.Parse(parameters[1]),
						float.Parse(parameters[2]),
						float.Parse(parameters[3]),1.0f
					);
                    break;
                case "Ks":
					currentMat.Specular = new Color (
						float.Parse(parameters[1]),
						float.Parse(parameters[2]),
						float.Parse(parameters[3]),1.0f
					);
                    break;
                case "d":
                case "Tr":
                    currentMat.Transparency = float.Parse(parameters[1]);
                    break;
                case "map_Ka":
                    currentMat.AmbientMap = new Texture(parameters[parameters.Length - 1]);
                    break;
                case "map_Kd":
                    currentMat.DiffuseMap = new Texture(parameters[parameters.Length - 1]);
                    break;
                case "map_Ks":
                    currentMat.SpecularMap = new Texture(parameters[parameters.Length - 1]);
                    break;
                case "map_Ns":
                    currentMat.SpecularHighlightMap = new Texture(parameters[parameters.Length - 1]);
                    break;
                case "map_d":
                    currentMat.AlphaMap = new Texture(parameters[parameters.Length - 1]);
                    break;
                case "map_bump":
                case "bump":
                    currentMat.BumpMap = new Texture(parameters[parameters.Length - 1]);
                    break;
                case "disp":
                    currentMat.DisplacementMap = new Texture(parameters[parameters.Length - 1]);
                    break;
                case "decal":
                    currentMat.StencilDecalMap = new Texture(parameters[parameters.Length - 1]);
                    break;
            }

        }

        if (currentMat != null)
            Materials.Add(currentMat);

        return Materials;
    }


    static char[] faceParamaterSplitter = new char[] { '/' };


}
