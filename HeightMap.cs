using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using go;
using System.Diagnostics;

namespace GGL
{
    [Serializable]
    public class HeightMap
    {
        Random rnd = new Random();

        public float[,] Heights { get; set; }
        public int Size { get; set; }
        private PerlinGenerator Perlin { get; set; }

        public HeightMap(int size)
        {
            Size = size;
            Heights = new float[Size, Size];
            Perlin = new PerlinGenerator(0);
        }

        public void AddPerlinNoise(float f, float scale = 1f)
        {
            for (int i = 0; i < Size; i++)
            {
                for (int j = 0; j < Size; j++)
                {
                    Heights[i, j] += Perlin.Noise(f * i / (float)Size, f * j / (float)Size, 0) * scale;
                }
            }
        }

        public void Perturb(float f, float d)
        {
            int u, v;
            float[,] temp = new float[Size, Size];
            for (int i = 0; i < Size; ++i)
            {
                for (int j = 0; j < Size; ++j)
                {
                    u = i + (int)(Perlin.Noise(f * i / (float)Size, f * j / (float)Size, 0) * d);
                    v = j + (int)(Perlin.Noise(f * i / (float)Size, f * j / (float)Size, 1) * d);
                    if (u < 0) u = 0; if (u >= Size) u = Size - 1;
                    if (v < 0) v = 0; if (v >= Size) v = Size - 1;
                    temp[i, j] = Heights[u, v];
                }
            }
            Heights = temp;
        }
        public void Erode(float smoothness)
        {
            for (int i = 1; i < Size - 1; i++)
            {
                for (int j = 1; j < Size - 1; j++)
                {
                    float d_max = 0.0f;
                    int[] match = { 0, 0 };

                    for (int u = -1; u <= 1; u++)
                    {
                        for (int v = -1; v <= 1; v++)
                        {
                            if (Math.Abs(u) + Math.Abs(v) > 0)
                            {
                                float d_i = Heights[i, j] - Heights[i + u, j + v];
                                if (d_i > d_max)
                                {
                                    d_max = d_i;
                                    match[0] = u; match[1] = v;
                                }
                            }
                        }
                    }

                    if (0 < d_max && d_max <= (smoothness / (float)Size))
                    {
                        float d_h = 0.5f * d_max;
                        Heights[i, j] -= d_h;
                        Heights[i + match[0], j + match[1]] += d_h;
                    }
                }
            }
        }
        public void Smoothen()
        {
            for (int i = 1; i < Size - 1; ++i)
            {
                for (int j = 1; j < Size - 1; ++j)
                {
                    float total = 0.0f;
                    for (int u = -1; u <= 1; u++)
                    {
                        for (int v = -1; v <= 1; v++)
                        {
                            total += Heights[i + u, j + v];
                        }
                    }

                    Heights[i, j] = total / 9.0f;
                }
            }
        }

        public void gaussianConvolution(float deviation)
        {
            Heights = GaussianF.GaussianConvolution(Heights, deviation);
        }

        public void voronoi(int nbPoints, float scale = 1f)
        {
            List<Point> rndPoints = new List<Point>();

            double hMax = 0;

            //il me faut une liste de point aléatoires
            for (int i = 0; i < nbPoints; i++)
            {
                rndPoints.Add(new Point(rnd.Next(Size), rnd.Next(Size)));
            }

            //pour chaque pixel, on cherche le point le plus proche
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    double minLength = double.MaxValue;
                    Point minPoint = new Point(0, 0);

                    foreach (Point p in rndPoints)
                    {
                        Point vDist = p - new Point(x, y);
                        double length = Math.Sqrt(vDist.X * vDist.X + vDist.Y * vDist.Y);

                        if (length < minLength)
                        {
                            minLength = length;
                            minPoint = p;
                        }
                    }

                    Heights[x, y] = (float)Math.Pow(minLength, 2)*scale;
                }
            }
        }

        #region plasma fractalize
        public void plasmaFractal(double roughness, double scale)
        {
            double[,] plasma = Generate(Size, Size, roughness);
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    Heights[x, y] += (float)(plasma[x, y] * scale);
                }
            }
        }

        public double gRoughness;
        public double gBigSize;
        public double[,] Generate(int iWidth, int iHeight, double iRoughness)
        {
            double c1, c2, c3, c4;
            double[,] points = new double[iWidth + 1, iHeight + 1];

            //Assign the four corners of the intial grid random color values
            //These will end up being the colors of the four corners of the applet.		
            c1 = rnd.NextDouble();
            c2 = rnd.NextDouble();
            c3 = rnd.NextDouble();
            c4 = rnd.NextDouble();
            gRoughness = iRoughness;
            gBigSize = iWidth + iHeight;
            DivideGrid(ref points, 0, 0, iWidth, iHeight, c1, c2, c3, c4);
            return points;
        }
        private double Displace(double SmallSize)
        {

            double Max = SmallSize / gBigSize * gRoughness;
            return (rnd.NextDouble() - 0.5) * Max;
        }
        public void DivideGrid(ref double[,] points, double x, double y, double width, double height, double c1, double c2, double c3, double c4)
        {
            double Edge1, Edge2, Edge3, Edge4, Middle;

            double newWidth = Math.Floor(width / 2);
            double newHeight = Math.Floor(height / 2);

            if (width > 1 || height > 1)
            {
                Middle = ((c1 + c2 + c3 + c4) / 4) + Displace(newWidth + newHeight);	//Randomly displace the midpoint!
                Edge1 = ((c1 + c2) / 2);	//Calculate the edges by averaging the two corners of each edge.
                Edge2 = ((c2 + c3) / 2);
                Edge3 = ((c3 + c4) / 2);
                Edge4 = ((c4 + c1) / 2);//
                //Make sure that the midpoint doesn't accidentally "randomly displaced" past the boundaries!
                Middle = Rectify(Middle);
                Edge1 = Rectify(Edge1);
                Edge2 = Rectify(Edge2);
                Edge3 = Rectify(Edge3);
                Edge4 = Rectify(Edge4);
                //Do the operation over again for each of the four new grids.			
                DivideGrid(ref points, x, y, newWidth, newHeight, c1, Edge1, Middle, Edge4);
                DivideGrid(ref points, x + newWidth, y, width - newWidth, newHeight, Edge1, c2, Edge2, Middle);
                DivideGrid(ref points, x + newWidth, y + newHeight, width - newWidth, height - newHeight, Middle, Edge2, c3, Edge3);
                DivideGrid(ref points, x, y + newHeight, newWidth, height - newHeight, Edge4, Middle, Edge3, c4);
            }
            else	//This is the "base case," where each grid piece is less than the size of a pixel.
            {
                //The four corners of the grid piece will be averaged and drawn as a single pixel.
                double c = (c1 + c2 + c3 + c4) / 4;

                points[(int)(x), (int)(y)] = c;
                if (width == 2)
                {
                    points[(int)(x + 1), (int)(y)] = c;
                }
                if (height == 2)
                {
                    points[(int)(x), (int)(y + 1)] = c;
                }
                if ((width == 2) && (height == 2))
                {
                    points[(int)(x + 1), (int)(y + 1)] = c;
                }
            }
        }

        private double Rectify(double iNum)
        {
            if (iNum < 0)
            {
                iNum = 0;
            }
            else if (iNum > 1.0)
            {
                iNum = 1.0;
            }
            return iNum;
        }
        #endregion

        #region river creation

        List<Point> lowP = new List<Point>();
        float lowestH = float.MaxValue;
        const int nbLowestPoints = 10;

        public void createRiver()
        {


            lowP = new List<Point>();
            lowestH = float.MaxValue;
            double lastPente = 0;

            //find n lowest points on borders
            for (int y = 0; y < Size; y += Size - 1)
            {
                for (int x = 0; x < Size-1; x++)
                {
                    double pente = Heights[x + 1, y] / Heights[x, y];
                    if (pente >= 1)//pente montante
                    {
                        if (lastPente < 1)
                        {
                            if (Heights[x, y] < lowestH)
                            {
                                processPoint(x, y);
                            }
                        }
                    }
                    lastPente = pente;
                }
            }
            //for (int x = 0; x < Size; x += Size - 1)
            //{
            //    for (int y = 1; y < Size-1; y++)
            //    {
            //        if (Heights[x, y] < lowestH)
            //        {
            //            processPoint(x, y);
            //        }
            //    }
            //}


            foreach (Point p in lowP)
            {
                Heights[p.X, p.Y] = 10000;
            }
        }

        void processPoint(int x, int y)
        {
            bool added = false;

            for (int i = 0; i < lowP.Count; i++)
            {
                if (Heights[x, y] < Heights[lowP[i].X, lowP[i].Y])
                {
                    lowP.Add(new Point(x, y));
                    added = true;
                    break;
                }
            }

            //if (!added)
            //    lowP.Insert(0, new Point(x, y));

            //if (lowP.Count > nbLowestPoints)
            //    lowP.RemoveAt(lowP.Count - 1);

            //Point last = lowP.Last();
            //lowestH = Heights[last.X, last.Y];

            //Debug.WriteLine("{0} {1}", last, lowestH);
        }
        #endregion

        #region hydraulicErosion
        const float initWater = 0.1f;
        //const float 

        public class rainDrop
        {
            int x;
            int y;
            float sediment;
            float water;
            float speed;
        }
        public void hydraulicErosion()
        { 
        
        }

        #endregion
        public System.Drawing.Bitmap heightMapBMP
        {
            get
            {
                float hMax = 0,
                    hMin = float.MaxValue;
                foreach (float h in Heights)
                {
                    if (hMax < h)
                        hMax = h;
                    if (hMin > h)
                        hMin = h;
                }
                double hScale = 255.0 / (hMax - hMin);

                byte[] pixels = new byte[Size * Size * 4];

                for (int x = 0; x < Size; x++)
                {
                    for (int y = 0; y < Size; y++)
                    {
                        byte v = (byte)((Heights[x, y] - hMin) * hScale);

                        pixels[(x + y * Size) * 4] = v;
                        pixels[(x + y * Size) * 4 + 1] = v;
                        pixels[(x + y * Size) * 4 + 2] = v;
                        pixels[(x + y * Size) * 4 + 3] = 255;
                    }
                }
                System.Drawing.Bitmap bmp;
                unsafe
                {
                    bmp = new System.Drawing.Bitmap(Size, Size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);//, ptr);
                    System.Drawing.Imaging.BitmapData data = bmp.LockBits(new System.Drawing.Rectangle(0, 0, Size, Size),
                        System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                    System.Runtime.InteropServices.Marshal.Copy(pixels, 0, data.Scan0, Size * Size * 4);

                    bmp.UnlockBits(data);
                }

                return bmp;
            }
        }
    }
}
