using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using OpenTK;

namespace OTKGL
{

    [Serializable]
    public class Text
    {
        readonly Font TextFont = new Font(FontFamily.GenericSansSerif, 72);
        Bitmap TextBitmap;
        PointF[] positions;
        string[] lignes;
        Brush[] couleurs;
        int texture, nb_lignes;
        Vector3 position;

        public Text(Vector3 _position, string _text = "", int taille = 1)
        {
            
            position = _position;
            
            lignes = new string[taille];
            


            if (!string.IsNullOrEmpty(_text))
                AddText(_text);

        }

        public void AddText(string s)
        {
            lignes[nb_lignes] = s;
            nb_lignes++;
            UpdateText();
        }

        public void UpdateText()
        {
            if (nb_lignes != 0)
            {
                TextBitmap = new Bitmap(100, 100); // match window size

                texture = GL.GenTexture();
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, texture);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, TextBitmap.Width, TextBitmap.Height, 0,
                    PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);

                using (Graphics g = Graphics.FromImage(TextBitmap))
                {
                    float y = 0;

                    g.Clear(Color.Transparent);
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                    for (int i = 0; i < nb_lignes; i++)
                    {
                        SizeF size = g.MeasureString(lignes[i], TextFont);
                        
                        Bitmap bmp = new Bitmap((int)Math.Ceiling(size.Width),(int)Math.Ceiling(size.Height));
                        using (Graphics g2 = Graphics.FromImage(bmp)) 
                        {
                            g2.Clear(Color.Transparent);
                            g2.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                            g2.DrawString(lignes[i], TextFont, Brushes.WhiteSmoke, new PointF(0, 0));
                        }

                        Rectangle r = new Rectangle(Point.Empty, TextBitmap.Size);
                        float ratio = r.Width / size.Width;
                        r.Height =(int) (size.Height * ratio);

                        g.DrawImage(bmp, r);

                        y += size.Height;
                    }
                }

                System.Drawing.Imaging.BitmapData data = TextBitmap.LockBits(new Rectangle(0, 0, TextBitmap.Width, TextBitmap.Height),
                    System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, TextBitmap.Width, TextBitmap.Height, PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
                TextBitmap.UnlockBits(data);
            }
            //TextBitmap.Save(directories.rootDir + @"test.bmp");
        }

        public void render()
        {
            GL.PushMatrix();
            GL.PushAttrib(AttribMask.EnableBit);
            GL.PushClientAttrib(ClientAttribMask.ClientAllAttribBits);

            GL.PointSize(100f);


            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.Enable(EnableCap.Texture2D);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);

            GL.Enable(EnableCap.PointSprite);
            GL.TexEnv(TextureEnvTarget.PointSprite,TextureEnvParameter.CoordReplace,Color.White);

            
            GL.Begin(BeginMode.Points);
             GL.Vertex3(position);
            GL.End();

            GL.Disable(EnableCap.Texture2D);

            GL.PopClientAttrib();
            GL.PopAttrib();
            GL.PopMatrix();
        }
    }
}
