using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;
using System.Drawing.Imaging;
using System.Diagnostics;

namespace GGL
{
    public class WaterErosionSimulation
    {
        public enum SimulationTexture
        {
            none,
            paintQte,
            paintV,
            paintF
        }

        public SimulationTexture currentTexToPaint = SimulationTexture.none;

        public int HeightMapTexture
        {
            get { return texQuantity[0]; }
        }

        public Shader Shader;
        int width;
        int height;

        int fbo;

        int[] texQuantity = new int[2];
        int[] texFlux = new int[2];
        int[] texVelocity = new int[2];

        int uniformTime;
        int uniformDeltaTime;

        bool EvenCycle = false;

        int indexIn
        {
            get { return EvenCycle ? 1 : 0; }
        }
        int indexOut
        {
            get { return EvenCycle ? 0 : 1; }
        }

        DrawBuffersEnum[] dbeOddCycle = new DrawBuffersEnum[3] 
            {
                DrawBuffersEnum.ColorAttachment0,
                DrawBuffersEnum.ColorAttachment1,
                DrawBuffersEnum.ColorAttachment2
            };
        DrawBuffersEnum[] dbeEvenCycle = new DrawBuffersEnum[3] 
            {
                DrawBuffersEnum.ColorAttachment3,
                DrawBuffersEnum.ColorAttachment4,
                DrawBuffersEnum.ColorAttachment5
            };

        DrawBuffersEnum[] currentDbe
        {
            get { return EvenCycle ? dbeEvenCycle : dbeOddCycle; }
        }

        public int Width
        {
            get { return width; }
        }
        public int Height
        {
            get { return height; }
        }

        float precipitationFactor;
        float precipitationQuantity;
        float sedimentationCapacity;
        float dissolvingConstant;
        float depositionConstant;
        float evaporationConstant;

        public float PrecipitationFactor
        {
            get { return precipitationFactor; }
            set 
            {
                if (value == precipitationFactor)
                    return;
                precipitationFactor = value;
                GL.Uniform1(GL.GetUniformLocation(Shader, "precipitationFactor"), precipitationFactor);
                Debug.WriteLine("Precipitation factor: {0}", precipitationFactor);
            }
        }        
        public float PrecipitationQuantity
        {
            get { return precipitationQuantity; }
            set 
            {
                if (value == precipitationQuantity)
                    return;
                precipitationQuantity = value;
                GL.Uniform1(GL.GetUniformLocation(Shader, "precipitationQte"), precipitationQuantity);
                Debug.WriteLine("Precipitation quantity: {0}", precipitationQuantity);
            }
        }
        public float SedimentationCapacity
        {
            get { return sedimentationCapacity; }
            set
            {
                if (value == sedimentationCapacity)
                    return;
                sedimentationCapacity = value;
                GL.Uniform1(GL.GetUniformLocation(Shader, "sedimentationCapacity"), sedimentationCapacity);
                Debug.WriteLine("Sedimentation Capacity: {0}", sedimentationCapacity);
            }
        }
        public float DissolvingConstant
        {
            get { return dissolvingConstant; }
            set
            {
                if (value == dissolvingConstant)
                    return;
                dissolvingConstant = value;
                GL.Uniform1(GL.GetUniformLocation(Shader, "dissolvingConstant"), dissolvingConstant);
                Debug.WriteLine("Dissolving Constant: {0}", dissolvingConstant);
            }
        }
        public float DepositionConstant
        {
            get { return depositionConstant; }
            set
            {
                if (value == depositionConstant)
                    return;
                depositionConstant = value;
                GL.Uniform1(GL.GetUniformLocation(Shader, "depositionConstant"), depositionConstant);
                Debug.WriteLine("Deposition Constant: {0}", depositionConstant);
            }
        }
        public float EvaporationConstant
        {
            get { return evaporationConstant; }
            set
            {
                if (value == evaporationConstant)
                    return;
                evaporationConstant = value;
                GL.Uniform1(GL.GetUniformLocation(Shader, "evaporationConstant"), evaporationConstant);
                Debug.WriteLine("Evaporation Constant: {0}", evaporationConstant);
            }
        }


        public WaterErosionSimulation(int size)
        {
            init(size, size);
            Shader = new ExternalShader("", "waterErosion", "");

            GL.Uniform1(GL.GetUniformLocation(Shader, "quantityTexture"), 0);
            GL.Uniform1(GL.GetUniformLocation(Shader, "velocityTexture"), 1);
            GL.Uniform1(GL.GetUniformLocation(Shader, "fluxTexture"), 2);
            GL.Uniform1(GL.GetUniformLocation(Shader, "texelSize"), 1f / size);
            PrecipitationFactor = 0.005f;
            PrecipitationQuantity = 0.002f;
            GL.Uniform1(GL.GetUniformLocation(Shader, "pipeLenght"), 1f);
            SedimentationCapacity = 0.005f;
            DissolvingConstant = 0.005f;
            DepositionConstant = 0.005f;
            EvaporationConstant = 0.002f;

            uniformTime = GL.GetUniformLocation(Shader, "time");
            uniformDeltaTime = GL.GetUniformLocation(Shader, "deltaTime");
        }

        public void Update()
        {
            DrawOnFBO();
        }

        public void UpdateTime(float time, float delta)
        {
            GL.UseProgram(Shader);
            GL.Uniform1(uniformTime, time);

            GL.Uniform1(GL.GetUniformLocation(Shader, "deltaTime"), delta);
        }

        void init(int _width = 512, int _height = 512)
        {
            width = _width;
            height = _height;

            GL.Ext.GenFramebuffers(1, out fbo);
            GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, fbo);

            for (int i = 0; i < 2; i++)
            {
                texQuantity[i] = createEmptyTexture();
                texVelocity[i] = createEmptyTexture();
                texFlux[i] = createEmptyTexture();

                GL.Ext.FramebufferTexture2D(FramebufferTarget.FramebufferExt, FramebufferAttachment.ColorAttachment0Ext + i * 3,
                    TextureTarget.Texture2D, texQuantity[i], 0);
                GL.Ext.FramebufferTexture2D(FramebufferTarget.FramebufferExt, FramebufferAttachment.ColorAttachment1Ext + i * 3,
                    TextureTarget.Texture2D, texVelocity[i], 0);
                GL.Ext.FramebufferTexture2D(FramebufferTarget.FramebufferExt, FramebufferAttachment.ColorAttachment2Ext + i * 3,
                    TextureTarget.Texture2D, texFlux[i], 0);
            }


            GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, 0);
        }

        int createEmptyTexture()
        {
            int tex;
            GL.GenTextures(1, out tex);

            GL.BindTexture(TextureTarget.Texture2D, tex);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0,
            OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);

            GL.BindTexture(TextureTarget.Texture2D, 0);

            return tex;
        }

        void DrawOnFBO()
        {
            int savePgm = GL.GetInteger(GetPName.CurrentProgram);

            GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, fbo);

            GL.DrawBuffers(3, currentDbe);

            GL.PushAttrib(AttribMask.ViewportBit);
            {
                GL.UseProgram(Shader);

                GL.Enable(EnableCap.Texture2D);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, texQuantity[indexIn]);
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, texVelocity[indexIn]);
                GL.ActiveTexture(TextureUnit.Texture2);
                GL.BindTexture(TextureTarget.Texture2D, texFlux[indexIn]);

                GL.Viewport(0, 0, width, height);

                GL.MatrixMode(MatrixMode.Projection);
                GL.PushMatrix();
                OpenTK.Matrix4 ortho = OpenTK.Matrix4.CreateOrthographicOffCenter(-1, 1, -1, 1, 1, -1);
                GL.LoadMatrix(ref ortho);

                GL.MatrixMode(MatrixMode.Modelview);
                GL.PushMatrix();
                GL.LoadIdentity();

                UnityQuad.Draw();

                GL.MatrixMode(MatrixMode.Projection);
                GL.PopMatrix();
                GL.MatrixMode(MatrixMode.Modelview);
                GL.PopMatrix();
            }
            GL.PopAttrib();

            GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, 0); // disable rendering into the FB            

            GL.UseProgram(savePgm);

            EvenCycle = !EvenCycle;
        }

        float[,] getHeightMapFromCurrentTexture()
        {
            GL.BindTexture(TextureTarget.Texture2D, texQuantity[indexOut]);
            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            BitmapData data = bmp.LockBits(new System.Drawing.Rectangle(0, 0, width, height),
                    ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            GL.GetTexImage(TextureTarget.Texture2D, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

            int bmpSizeInPixels = width * height;
            int bmpSizeInBytes = bmpSizeInPixels * 4;
            byte[] pixels = new byte[bmpSizeInBytes];

            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, pixels, 0, bmpSizeInBytes);
            bmp.UnlockBits(data);

            float[,] hm = new float[width + 1, height + 1];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    hm[x, y] = pixels[(x + y * width) * 4];
                }
            }
            return hm;
        }

        public void saveTexturesToDisk()
        {
            saveTextures(texQuantity[0], "qte.png");
            saveTextures(texFlux[0], "flux.png");
            saveTextures(texVelocity[0], "velocity.png");
        }

        public void Paint()
        {
            if (currentTexToPaint == SimulationTexture.none)
                return;

            int savePgm = GL.GetInteger(GetPName.CurrentProgram);

            GL.PushAttrib(AttribMask.ViewportBit);
            {
                GL.UseProgram(0);
                GL.Viewport(0, 0, width, height);

                GL.MatrixMode(MatrixMode.Projection);
                GL.PushMatrix();
                OpenTK.Matrix4 ortho = OpenTK.Matrix4.CreateOrthographicOffCenter(-1, 1, -1, 1, 1, -1);
                GL.LoadMatrix(ref ortho);

                GL.MatrixMode(MatrixMode.Modelview);
                GL.PushMatrix();
                GL.LoadIdentity();


                GL.Scale(0.8, 0.8, 1);

                GL.ActiveTexture(TextureUnit.Texture0);
                switch (currentTexToPaint)
                {
                    case SimulationTexture.paintQte:
                        GL.BindTexture(TextureTarget.Texture2D, texQuantity[indexOut]);
                        break;
                    case SimulationTexture.paintV:
                        GL.BindTexture(TextureTarget.Texture2D, texVelocity[indexOut]);
                        break;
                    case SimulationTexture.paintF:
                        GL.BindTexture(TextureTarget.Texture2D, texFlux[indexOut]);
                        break;
                }
                
                UnityQuad.Draw();

                GL.MatrixMode(MatrixMode.Projection);
                GL.PopMatrix();
                GL.MatrixMode(MatrixMode.Modelview);
                GL.PopMatrix();
            }
            GL.PopAttrib();

            GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, 0); // disable rendering into the FB            

            GL.UseProgram(savePgm);
        }

        void saveTextures(int tex, string fileName)
        {
            GL.BindTexture(TextureTarget.Texture2D, tex);
            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            BitmapData data = bmp.LockBits(new System.Drawing.Rectangle(0, 0, width, height),
                    ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            GL.GetTexImage(TextureTarget.Texture2D, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
            bmp.UnlockBits(data);
            bmp.Save(@"d:/" + fileName);
        }

        public SimpleTerrain CreateTerrainFromTexture()
        {
            float[,] hm = getHeightMapFromCurrentTexture();

            return new SimpleTerrain(hm);
        }

    }
}
