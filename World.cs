using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using OpenTK;
using GGL;
using System.Drawing;
//using GGL.Shaders;
using System.Diagnostics;
using Jitter.LinearMath;
using System.Drawing.Imaging;
using Jitter.Dynamics;

namespace GGL
{
    public sealed class Conversion
    {
        public static float[] ToFloat(JVector vector)
        {
            return new float[4] { vector.X, vector.Y, vector.Z, 0.0f };
        }

        public static float[] ToFloat(JMatrix matrix)
        {
            return new float[12] { matrix.M11, matrix.M21, matrix.M31, 0.0f,
                                   matrix.M12, matrix.M22, matrix.M32, 0.0f,
                                   matrix.M13, matrix.M23, matrix.M33, 1.0f };
        }
        public static Vector3 jToOtk(JVector v)
        {
            //return v == null ? Vector3.Zero : new Vector3(v.X, v.Y, v.Z);
            return new Vector3(v.X, v.Y, v.Z);
        }
        public static JVector OtkToJ(Vector3 v)
        {
            //return v == null ? Vector3.Zero : new Vector3(v.X, v.Y, v.Z);
            return new JVector(v.X, v.Y, v.Z);
        }
    }

    [Serializable]
    public class World
    {
        public static int lastEditedVertexIndex = -1;
        public static World CurrentWorld;
        public Jitter.World physicalWorld;
        public Jitter.Collision.CollisionSystem collisionsSystem;

        public static Vector3 vEye;


        public static bool Picking = false;

        public static bool pathEdition = false;
        public static bool roadEdition = false;
        public static bool treeAddition = false;
        public static bool houseAddition = false;

        public static bool renderWater = false;
        public static bool renderTerrain = false;

        //equivalent en mettre de 1 unitée opengl
        public const float unity = 1f;

        public static int selX = 0;
        public static int selY = 0;

        public static int selStartX = 0;
        public static int selStartY = 0;
        public static int selEndX = 0;
        public static int selEndY = 0;

        public static Texture concrete;
        public static Texture pavement;
        public static Texture brick;



        public long elapsedMiliseconds = 0;
        public float elapsedSeconds = 0f;
        [NonSerialized]
        Stopwatch timer = new Stopwatch();
        long lastElapsedMilliseconds = 0;
        public void startTimeCounters()
        {
            timer.Start();
        }
        public void stopTimeCounter()
        {
            timer.Stop();
        }
        public void updateTimeCounters()
        {
            long em = timer.ElapsedMilliseconds;
            elapsedMiliseconds = em - lastElapsedMilliseconds;
            lastElapsedMilliseconds = em;
            //Debug.WriteLine(elapsedMiliseconds + " lastEMS: " + lastElapsedMilliseconds + " timer.ms:" + timer.ElapsedMilliseconds);
            elapsedSeconds += (float)elapsedMiliseconds / 1000f;
        }
        public void save(string fileName)
        {
            BinaryFormatter serializer = new BinaryFormatter();

            using (Stream stream = File.Create(fileName))
            {
                serializer.Serialize(stream, this);
            }

        }


        public void Prepare()
        {
            concrete = new Texture(directories.rootDir + @"Images\texture\concrete.jpg", true);
            pavement = new Texture(directories.rootDir + @"Images\texture\Pavement.jpg", true);
            brick = new Texture(directories.rootDir + @"Images\texture\brick005.jpg", true);

            foreach (ModelInstance mi in objects)
                SelectableObject.registerObject(mi);

            foreach (Road r in roads)
            {
                r.world = this;
                SelectableObject.registerObject(r);

                foreach (GenericRoadSegment rs in r.segments)
                {

                    rs.Prepare();

                }
            }

            foreach (Vehicle v in vehicles)
            {
                v.Bind(this);
            }
            //Road.currentRoad = ter.roads[0];
            foreach (BOquads c in cityPlates)
            {
                c.Prepare();
            }
        }

        public static World load(string filename)
        {
            BinaryFormatter serializer = new BinaryFormatter();

            World tmp = null;

            using (Stream stream = File.OpenRead(filename))
            {
                tmp = (World)serializer.Deserialize(stream);
            }

            World.CurrentWorld = tmp;

            for (int x = 0; x < worldSize; x++)
            {
                for (int y = 0; y < worldSize; y++)
                {
                    tmp.terrains[x, y].OriginalModel.Prepare();
                    tmp.terrains[x, y].model = tmp.terrains[x, y].OriginalModel;
                }
            }

            tmp.Prepare();

            return tmp;

        }

        [NonSerialized]
        HeightMap heightMap;

        public static int worldSize = 1;
        public static Vector3 wind = new Vector3(1, 1, 0);

        public Terrain[,] terrains;
        public List<GGL.Path> pathes = new List<GGL.Path>();
        public List<ModelInstance> objects = new List<ModelInstance>();
        public List<Forest> forests = new List<Forest>();
        public List<Road> roads = new List<Road>();
        public List<Vehicle> vehicles = new List<Vehicle>();



        public void addObject(ModelInstance m)
        {
            //try
            //{
            //    m.z = getHeight((int)m.x, (int)m.y);
            //}
            //catch
            //{ }

            objects.Add(m);

            if (m.body != null)
                physicalWorld.AddBody(m.body);

        }
        public void removeObject(ModelInstance m)
        {
            if (m.body != null)
                physicalWorld.RemoveBody(m.body);
            SelectableObject.unregisterObject(m);
            objects.Remove(m);
        }
        public void addRoad(Road r)
        {
            r.bind(this);
        }
        public void addForest(Forest f)
        {
            forests.Add(f);
            f.bind(this);
        }
        public void addVehicle(Vehicle v, int roadIndex)
        {
            v.Bind(roadIndex, this);
        }
        public Triangle getTriangle(int x, int y)
        {
            int wx = (int)x / (Terrain.size);
            int wy = (int)y / (Terrain.size);
            int tx = x % (Terrain.size);
            int ty = y % (Terrain.size);

            if (wx > worldSize - 1 || wy > worldSize - 1 || wx < 0 || wy < 0)
                return new Triangle();
            return terrains[wx, wy].getTriangle(tx, ty);
        }
        public Terrain getTerrainContainingPoint(int x, int y)
        {
            int wx = (int)x / (Terrain.size);
            int wy = (int)y / (Terrain.size);
            return terrains[wx, wy];
        }
        public Terrain getTerrainContainingPoint(Vector3 v)
        {
            int wx = (int)v.X / (Terrain.size);
            int wy = (int)v.Y / (Terrain.size);
            return terrains[wx, wy];
        }

        public void deleteTriangle(int x, int y)
        {
            int wx = (int)x / (Terrain.size);
            int wy = (int)y / (Terrain.size);
            int tx = x % (Terrain.size);
            int ty = y % (Terrain.size);

            if (wx > worldSize - 1 || wy > worldSize - 1 || wx < 0 || wy < 0)
                return;

            terrains[wx, wy].deleteTriangle(tx, ty);
        }
        public void deleteQuad(int x, int y)
        {
            int wx = (int)x / (Terrain.size);
            int wy = (int)y / (Terrain.size);
            int tx = x % (Terrain.size);
            int ty = y % (Terrain.size);

            if (wx > worldSize - 1 || wy > worldSize - 1 || wx < 0 || wy < 0)
                return;

            terrains[wx, wy].deleteQuad(tx, ty);
        }
        public void deleteQuad(int startX, int endX, int startY, int endY)
        {
            int sX = Math.Min(startX, endX) + 1;
            int eX = Math.Max(startX, endX) + 1;
            int sY = Math.Min(startY, endY) + 1;
            int eY = Math.Max(startY, endY) + 1;

            for (int y = sY; y < eY; y++)
            {
                for (int x = sX; x < eX; x++)
                {
                    deleteQuad(x, y);
                }
            }
            getTerrainContainingPoint(startX, startY).reshape();
        }

        public Vertex getVertex(int x, int y)
        {
            int wx = (int)x / (Terrain.size + 1);
            int wy = (int)y / (Terrain.size + 1);
            int tx = x % (Terrain.size + 1);
            int ty = y % (Terrain.size + 1);

            if (wx > worldSize - 1 || wy > worldSize - 1 || wx < 0 || wy < 0)
                return new Vertex();
            return terrains[wx, wy].getVertex(tx, ty);
        }


        public void SelectFirstQuadWithHeightDifferent(Vector3[] path, float resolution = 0.5f, float width = 1f, float height = 1f)
        {
            for (int i = 0; i < path.Length - 1; i++)
            {
                Vector3 vDir = path[i + 1] - path[i];
                float segLength = vDir.Length;
                vDir.Normalize();

                Vector2 vPerp = new Vector2(vDir).PerpendicularLeft;
                vPerp.Normalize();
                Vector3 vPerp3 = new Vector3(vPerp);

                Vector3 v = path[i] + vDir * resolution;


                do
                {
                    Vector3 neareastV = getNeareastVertex(new Vector2(v)).position;
                    if (neareastV.Z - v.Z > height)
                    {
                        Vector3 vPos = getVertex((int)v.X, (int)v.Y).position;
                        selStartX = (int)vPos.X;
                        selStartY = (int)vPos.Y;
                        selEndX = (int)vPos.X + 1;
                        selEndY = (int)vPos.Y + 1;

                        return;
                    }

                    v += vDir * resolution;

                } while ((v - path[i]).Length < segLength);
            }
        }

        public void RemoveQuadsAlongPath(Vector3[] path, float resolution = 0.5f, float width = 1f, float height = 1f)
        {
            //je sais plus si elle marche
            for (int i = 0; i < path.Length - 1; i++)
            {
                Vector3 vDir = path[i + 1] - path[i];
                float segLength = vDir.Length;
                vDir.Normalize();

                Vector2 vPerp = new Vector2(vDir).PerpendicularLeft;
                vPerp.Normalize();
                Vector3 vPerp3 = new Vector3(vPerp);

                Vector3 v = path[i];


                do
                {
                    float diff = getNeareastVertex(new Vector2(v)).position.Z - v.Z;
                    if (diff > 0.1 && diff < height)
                    {
                        deleteTriangle((int)v.X + 1, (int)v.Y + 1);
                        break;
                    }

                    v += vDir * resolution;

                } while ((v - path[i]).Length < segLength);
            }
        }
        public void levelGroundAlongPath(Vector3[] path, float resolution = 0.5f, float width = 1f, float maxDiff = float.MaxValue)
        {
            for (int i = 0; i < path.Length - 1; i++)
            {
                Vector3 vDir = path[i + 1] - path[i];
                float segLength = vDir.Length;
                vDir.Normalize();

                Vector2 vPerp = new Vector2(vDir).PerpendicularLeft;
                vPerp.Normalize();
                Vector3 vPerp3 = new Vector3(vPerp);

                Vector3 v = path[i];


                do
                {
                    Vector3 v2 = v;

                    do
                    {
                        if (getNeareastVertex(new Vector2(v2)).position.Z - v2.Z > maxDiff)
                            break;
                        setHeightToNeareastVertex(new Vector2(v2), v2.Z, true);
                        v2 += vPerp3 * resolution;
                    } while ((v2 - v).Length < width);

                    v2 = v;
                    do
                    {
                        if (getNeareastVertex(new Vector2(v2)).position.Z - v2.Z > maxDiff)
                            break;
                        setHeightToNeareastVertex(new Vector2(v2), v2.Z, true);
                        v2 -= vPerp3 * resolution;
                    } while ((v2 - v).Length < width);

                    v += vDir * resolution;

                } while ((v - path[i]).Length < segLength);
            }
        }

        public void levelGroundAlongPath(Vector3[] path, float resolution = 0.5f, float width = 1f)
        {
            for (int i = 0; i < path.Length - 1; i++)
            {
                Vector3 vDir = path[i + 1] - path[i];
                float segLength = vDir.Length;
                vDir.Normalize();

                Vector2 vPerp = new Vector2(vDir).PerpendicularLeft;
                vPerp.Normalize();
                Vector3 vPerp3 = new Vector3(vPerp);

                Vector3 v = path[i];


                do
                {
                    Vector3 v2 = v;

                    do
                    {
                        setHeightToNeareastVertex(new Vector2(v2), v2.Z, true);
                        v2 += vPerp3 * resolution;
                    } while ((v2 - v).Length < width);

                    v2 = v;
                    do
                    {
                        setHeightToNeareastVertex(new Vector2(v2), v2.Z, true);
                        v2 -= vPerp3 * resolution;
                    } while ((v2 - v).Length < width);

                    v += vDir * resolution;

                } while ((v - path[i]).Length < segLength);
            }
            reshapeTerrainsAlongPath(path);
        }
        public void reshapeTerrainsAlongPath(Vector3[] path)
        {
            List<Terrain> terToUpdate = new List<Terrain>();
            for (int i = 0; i < path.Length; i++)
            {
                Terrain t = getTerrainContainingPoint(path[i]);
                if (!terToUpdate.Contains(t))
                    terToUpdate.Add(t);
            }
            foreach (Terrain t in terToUpdate)
            {
                t.reshape();
            }
        }

        public float getHeight(int x, int y)
        {
            int wx = (int)x / (Terrain.size);
            int wy = (int)y / (Terrain.size);
            int tx = x % (Terrain.size);
            int ty = y % (Terrain.size);

            if (wx > worldSize - 1 || wy > worldSize - 1 || wx < 0 || wy < 0)
                return 0f;
            return terrains[wx, wy].getHeight(tx, ty);
        }
        public float getHeight(float x, float y)
        {
            int wx = (int)x / (Terrain.size);
            int wy = (int)y / (Terrain.size);
            float tx = x % (float)(Terrain.size);
            float ty = y % (float)(Terrain.size);

            if (wx > worldSize - 1 || wy > worldSize - 1 || wx < 0 || wy < 0)
                return 0f;
            return terrains[wx, wy].getHeight(tx, ty);
        }
        public Vertex getNeareastVertex(Vector2 v)
        {
            int wx = (int)v.X / (Terrain.size);
            int wy = (int)v.Y / (Terrain.size);
            float tx = v.X % (float)(Terrain.size);
            float ty = v.Y % (float)(Terrain.size);

            if (wx > worldSize - 1 || wy > worldSize - 1 || wx < 0 || wy < 0)
                return new Vertex();
            return terrains[wx, wy].getNeareastVertex(tx, ty);
        }
        public float getAverageHeight(float x, float y, int nbCase = 2)
        {
            int wx = (int)x / (Terrain.size + 1);
            int wy = (int)y / (Terrain.size + 1);
            float tx = x % (float)(Terrain.size + 1);
            float ty = y % (float)(Terrain.size + 1);

            if (wx > worldSize - 1 || wy > worldSize - 1 || wx < 0 || wy < 0)
                return 0f;
            return terrains[wx, wy].getAverageHeightVector(new Vector2(tx, ty), 1).Z;
        }
        public float getHeight(Vector2 v)
        {
            return getHeight(v.X, v.Y);
        }
        public Vector3 getHeightVector(int x, int y)
        {
            return new Vector3(x, y, getHeight(x, y));
        }
        public Vector3 getHeightVector(Vector2 v)
        {
            //int x = (int)v.X;
            //int y = (int)v.Y;

            //Vector3 v1 = new Vector3(x,y,getHeight(x, y));            
            //Vector3 v2 = new Vector3(x+1,y,getHeight(x+1, y));
            //Vector3 v3 = new Vector3(x,y+1,getHeight(x, y+1));
            //Vector3 v4 =  new Vector3(x+1,y+1,getHeight(x+1, y+1));

            //return Vector3.BaryCentric(v1, v2, v4, v.X % 1, v.Y % 1);
            //Vector3 res = Vector3.Lerp(v1, v2, (float)Math.Sqrt(Math.Pow(v.X % 1, 2.0) + Math.Pow(v.Y % 1, 2.0)));
            ////Debug.WriteLine("v1:{0} v2:{1} res:{2}", v1, v2, res);
            //return res;

            //int x = (int)v.X;
            //int y = (int)v.Y;

            //Vector3 v1 = new Vector3(x, y, getHeight(x, y));
            //Vector3 v2 = new Vector3(x+1, y+1, getHeight(x, y));

            //Vector3 res = Vector3.Lerp(v1, v2, (float)Math.Sqrt(Math.Pow(v.X % 1, 2.0) + Math.Pow(v.Y % 1, 2.0)));
            ////Vector3 res = v1;

            return new Vector3(v.X, v.Y, getHeight(v));
        }

        public int getVertexIndex(Vector2 v)
        {
            int wx = (int)v.X / Terrain.size;
            int wy = (int)v.Y / Terrain.size;
            float tx = v.X % Terrain.size;
            float ty = v.Y % Terrain.size;

            return terrains[wx, wy].getVertexIndex(v) + wx * Terrain.VertexPerLine + wy * Terrain.VertexPerLine;
        }
        public void setHeightToNeareastVertex(Vector2 v, float height, bool singleVertex = false)
        {
            int wx = (int)v.X / Terrain.size;
            int wy = (int)v.Y / Terrain.size;
            float tx = v.X % Terrain.size;
            float ty = v.Y % Terrain.size;

            lastEditedVertexIndex = terrains[wx, wy].getVertexIndex(v) + wx * Terrain.VertexPerLine + wy * Terrain.VertexPerLine;

            terrains[wx, wy].setHeight((int)Math.Round(tx, 0, MidpointRounding.AwayFromZero), (int)Math.Round(ty, 0, MidpointRounding.AwayFromZero), height, singleVertex);

        }
        public void setHeight(Vector2 v, float height, bool singleVertex = false)
        {
            int wx = (int)v.X / Terrain.size;
            int wy = (int)v.Y / Terrain.size;
            float tx = v.X % Terrain.size;
            float ty = v.Y % Terrain.size;

            lastEditedVertexIndex = terrains[wx, wy].getVertexIndex(v) + wx * Terrain.VertexPerLine + wy * Terrain.VertexPerLine;

            terrains[wx, wy].setHeight((int)tx, (int)ty, height, singleVertex);
        }
        public void setVertexPositionInWorldCoordonate(Vector3 v, Vector3 pos)
        {
            int wx = (int)v.X / Terrain.size;
            int wy = (int)v.Y / Terrain.size;
            float tx = v.X % Terrain.size;
            float ty = v.Y % Terrain.size;

            Vector3 vWorld = new Vector3(wx * Terrain.size, wy * Terrain.size, 0f);
            terrains[wx, wy].setVertexPosition(terrains[wx, wy].getVertexIndex((int)tx, (int)ty), pos - vWorld);
        }
        public void setVertexPositionInTerrainCoordonate(Vector3 v, Vector3 pos)
        {
            int wx = (int)v.X / Terrain.size;
            int wy = (int)v.Y / Terrain.size;
            float tx = v.X % Terrain.size;
            float ty = v.Y % Terrain.size;

            terrains[wx, wy].setVertexPosition(terrains[wx, wy].getVertexIndex((int)tx, (int)ty), pos);
        }
        
        public BOquads water;
        [NonSerialized]
        public Shader waterShader;

        public List<BOquads> cityPlates = new List<BOquads>();

        public void addCityPlate(int x1, int y1, int x2, int y2)
        {
            makePlanar(x1, y1, x2, y2);
            float height = Math.Abs(x1 - x2);
            float width = Math.Abs(y1 - y2);
            BOquads c = BOquads.createPlaneZup(x1, y1, x2, y2, getHeight(x1, y1) + 0.01f, width, height);

            c.Prepare();
            cityPlates.Add(c);

        }
        public void makePlanar(int x1, int y1, int x2, int y2)
        {
            int tmp;
            if (x1 > x2)
            {
                tmp = x1;
                x1 = x2;
                x2 = tmp;
            }
            if (y1 > y2)
            {
                tmp = y1;
                y1 = y2;
                y2 = tmp;
            }

            int wx1 = (int)x1 / (Terrain.size + 1);
            int wy1 = (int)y1 / (Terrain.size + 1);
            int tx1 = x1 % (Terrain.size + 1);
            int ty1 = y1 % (Terrain.size + 1);
            int wx2 = (int)x2 / (Terrain.size + 1);
            int wy2 = (int)y2 / (Terrain.size + 1);
            int tx2 = x2 % (Terrain.size + 1);
            int ty2 = y2 % (Terrain.size + 1);


            terrains[wx1, wy1].makePlanar(tx1, ty1, tx2, ty2);
        }
        public void createWaterPlane()
        {
            water = BOquads.createMultiPlaneZup(0, 0, worldSize * Terrain.size, worldSize * Terrain.size, 100, 100, 0f, 5f, 5f);
            water.Prepare();
            waterShader = new WaterShader();
            renderWater = true;
        }

        public Bitmap heightMapBMP
        {
            get
            {
                if (heightMap == null)
                    return null;
                int hmSize = worldSize * (Terrain.size + 1);
                byte[] heights = new byte[hmSize * hmSize * 4];

                for (int x = 0; x < hmSize; x++)
                {
                    for (int y = 0; y < hmSize; y++)
                    {
                        byte v = (byte)(int)(heightMap.Heights[x, y] * 128f + 128f);

                        heights[(x + y * hmSize) * 4] = v;
                        heights[(x + y * hmSize) * 4 + 1] = v;
                        heights[(x + y * hmSize) * 4 + 2] = v;
                        heights[(x + y * hmSize) * 4 + 3] = 255;
                    }
                }
                Bitmap bmp;
                unsafe
                {
                    fixed (byte* b = heights)
                    {
                        IntPtr ptr = new IntPtr(b);
                        bmp = new Bitmap(hmSize, hmSize, System.Drawing.Imaging.PixelFormat.Format32bppArgb);//, ptr);
                    }
                    BitmapData data = bmp.LockBits(new System.Drawing.Rectangle(0, 0, hmSize, hmSize),
                        ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                    System.Runtime.InteropServices.Marshal.Copy(heights, 0, data.Scan0, hmSize * hmSize * 4);

                    bmp.UnlockBits(data);
                }
                //Bitmap bmp = new Bitmap(hmSize, hmSize);

                return bmp;
            }
        }

        public void createTerrain()
        {
            Terrain.terrainShader = new TerrainShader();

            terrains = new Terrain[worldSize, worldSize];
            heightMap = new HeightMap(worldSize * (Terrain.size + 1));

            
            heightMap.voronoi(100,0.01f);
            //heightMap.Perturb(15.0f, 15.0f);
            //heightMap.plasmaFractal(50.0, 1.0);
            heightMap.AddPerlinNoise(6.0f, 10f);
            //heightMap.AddPerlinNoise(30.0f,100f);

            heightMap.gaussianConvolution(1.0f);
            //heightMap.createRiver();

            //heightMap.heightMapBMP.Save(@"d:\test.png");
            
            //for (int i = 0; i < 10; i++)
            //    heightMap.Erode(16.0f);
            //heightMap.Smoothen();


            

            int x = 0;
            int y = 0;

            while (x < worldSize)
            {
                y = 0;
                while (y < worldSize)
                {
                    float[,] Heights = new float[Terrain.size + 1, Terrain.size + 1];


                    int xh = x * (Terrain.size + 1);
                    if (x > 0)
                        xh--;

                    int xt = 0;
                    while (xt < Terrain.size + 1)
                    {
                        int yh = y * (Terrain.size + 1);
                        if (y > 0)
                            yh--;

                        int yt = 0;
                        while (yt < Terrain.size + 1)
                        {
                            Heights[xt, yt] = heightMap.Heights[xh, yh];
                            yh++;
                            yt++;
                        }
                        xh++;
                        xt++;
                    }


                    terrains[x, y] = new Terrain(Heights, x, y);

                    y++;
                }
                x++;

            }

            renderTerrain = true;
        }

        public void deleteTerrain()
        {
            if (terrains != null)
            {
                foreach (Terrain t in terrains)
                {
					if (t.body != null)
                    	physicalWorld.RemoveBody(t.body);
                }
            }
            terrains = null;
        }
        public void createPhysicalWorld()
        {
            collisionsSystem = new Jitter.Collision.CollisionSystemSAP();
            physicalWorld = new Jitter.World(collisionsSystem);
            physicalWorld.Gravity = new JVector(0, 0, -9.81f);
            //physicalWorld.ContactSettings.MaterialCoefficientMixing = ContactSettings.MaterialCoefficientMixingType.TakeMinimum;
            //physicalWorld.ContactSettings.AllowedPenetration = 0.00f;
            //physicalWorld.ContactSettings.BiasFactor = 0.25f;
            //physicalWorld.ContactSettings.BreakThreshold = 0.01f;
            //physicalWorld.ContactSettings.MaximumBias = 10f;
            //physicalWorld.ContactSettings.MinimumVelocity = 0.001f;
            //physicalWorld.CollisionSystem.EnableSpeculativeContacts = true;
            
        }

        public void createPhysicalTerrains()
        {
            foreach (Terrain t in terrains)
            {
                t.model.createShape();
                
                t.body = new RigidBody(t.model.shape);
                t.body.Position = new Jitter.LinearMath.JVector(t.xWorld * Terrain.size, t.yWorld * Terrain.size, 0);
                t.body.Material.Restitution = 0.0f;
                
                t.body.EnableSpeculativeContacts = true;
                t.body.LinearVelocity = Jitter.LinearMath.JVector.Zero;
                
                t.body.IsStatic = true;
                
                t.body.Mass = 1000000;

                physicalWorld.AddBody(t.body);
            }
        }
        public void deletePhysicalTerrains()
        {
            foreach (Terrain t in terrains)
            {
                physicalWorld.RemoveBody(t.body);
                t.model.shape = null;
                t.body = null;
            }
        }
        public static float timeScale = 500f;
        public void physics()
        {
            if (physicalWorld == null)
                return;

            if (elapsedMiliseconds != 0)
            {
                float step = (float)elapsedMiliseconds / timeScale;
                //Debug.WriteLine(step + " " + elapsedTime);
                physicalWorld.Step(10f/timeScale, true);
                //physicalWorld.Step(step, true);
            }
            //physicalWorld.Step(0.01f, true);
        }
        public World()
        {
            World.CurrentWorld = this;


        }
        public void render()
        {

            //GL.Enable(EnableCap.Light0);

            if (World.Picking)
            {
                //if (Game.objectEdition)
                foreach (ModelInstance o in objects)
                    o.Render();
                //if (Game.roadEdition)
                foreach (Road r in roads)
                    r.render();
                if (World.pathEdition)
                {
                    if (GGL.Path.newPath != null)
                        GGL.Path.newPath.Render();
                }
                GL.LoadName(-1);
                return;
            }
            //GL.Enable(EnableCap.Lighting);


            if (renderTerrain)
            {
                for (int x = 0; x < worldSize; x++)
                {
                    for (int y = 0; y < worldSize; y++)
                    {
                        terrains[x, y].render();                        
                    }
                }
            }

            if (renderWater)
                drawWater();

            foreach (ModelInstance o in objects)
            {
                o.Render();
            }

            foreach (Forest f in forests)
                f.render();

            foreach (Road r in roads)
                r.render();

            foreach (Vehicle v in vehicles)
                v.Render();

            if (World.pathEdition)
            {
                if (GGL.Path.newPath != null)
                    GGL.Path.newPath.Render();

                foreach (GGL.Path p in pathes)
                {
                    p.Render();
                }
            }

            GL.BindTexture(TextureTarget.Texture2D, World.pavement);
            GL.Enable(EnableCap.Texture2D);

            foreach (BOquads p in cityPlates)
            {
                p.Render();
            }
            GL.Disable(EnableCap.Texture2D);


            //GL.Disable(EnableCap.ColorMaterial);

            GL.LoadName(-1);
        }

        void drawWater()
        {
            //water
            //GL.Enable(EnableCap.ColorMaterial);

            GL.Color3(Color.FromArgb(50, Color.White));

            GL.Enable(EnableCap.AlphaTest);
            GL.AlphaFunc(AlphaFunction.Greater, 0.0f);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);


            GL.Uniform1(GL.GetUniformLocation(waterShader, "time"), World.CurrentWorld.elapsedSeconds);

            //GL.BindTexture(TextureTarget.Texture2D, Game.waterTex);
            //GL.Enable(EnableCap.Texture2D);

            //

            water.Render();
            GL.Disable(EnableCap.Texture2D);

            GL.Color3(Color.White);
        }
    }
}
