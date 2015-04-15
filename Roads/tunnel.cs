using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using GGL;
using System.Drawing;
using System.Diagnostics;

namespace GGL
{
    [Serializable]
    public class tunnel : GenericRoadSegment
    {
        BOquads structure;
        BOquads roofAndGround;

        public float height = 1f;
        public float width = 1f;

        public float pilasseWidth = 0.2f;

        Vector3[] inQuad = new Vector3[4];
        Vector3[] outQuad = new Vector3[4];

        public tunnel(Road _road)
            : base(_road,false,1)
        {
            handles = new Vector3[2];

            handles[0] = new Vector3(Mouse3d.Position);
            handles[1] = new Vector3(Mouse3d.Position);

            Road.newSegmentInit = true;

            road.currentHandleInNewSegment = 1;
            Mouse3d.setPosition(handles[0]);

            preBind();

            if (Road.currentSegment != null)
            {
                road.currentHandleInNewSegment = 0;
                Road.currentRoad.checkLinkForNewSegment();
                road.currentHandleInNewSegment = 1;
                preBind();
            }

            inclinaisonMax = MathHelper.Pi / 32f;

        }
        public override void validatePath()
        {
            isValid = true;
            int i;
            for (i = 0; i < pathSegments; i++)
            {
                if (segVerticalAngles[i] > inclinaisonMax)
                {
                    isValid = false;
                    invalidIndex = i;
                    return;
                }
            }
            //check if ground is lower everywhere
            for (i = 1; i < computeLength(); i++)
            {
                float t = 1f / computeLength() * i;
                Vector3 pos = Vector3.Lerp(positions[0], positions[1], t);
                float h = world.getHeight(new Vector2(pos));
                if (h < pos.Z)
                {
                    isValid = false;
                    invalidIndex = 0;
                    return;
                }
            }
        }
        public override void bind()
        {
            Vector3 vHeight = new Vector3(0, 0, -0.2f);
            //tile = computeLength();


            //structure = BOquads.createUncappedRectangle(geometry[0] + vboZadjustment, geometry[1] + vboZadjustment, geometry[2] + vboZadjustment, geometry[3] + vboZadjustment, vHeight.Z, computeLength() * 5f, 1f);

            //int nbPilasses = (int)(computeLength() / 10f);
            //if (nbPilasses < 2)
            //    nbPilasses = 2;

            //for (int i = 1; i <= nbPilasses; i++)
            //{
            //    float t = 1f / (nbPilasses + 1) * i;
            //    Vector3 vMiddle = Vector3.Lerp(positions[0], positions[1], t);
            //    float h = world.getHeight(new Vector2(vMiddle));

            //    Vector3 vWidth = new Vector3((float)width * 0.8f, 0, 0);
            //    Vector3 vLength = new Vector3(0, 0.2f, 0);

            //    structure += BOquads.createUncappedRectangle(
            //        vMiddle - vWidth - vLength, vMiddle + vWidth - vLength,
            //        vMiddle - vWidth + vLength, vMiddle + vWidth + vLength, -vMiddle.Z + h, 1f, h * 5f);

            //}

            world.levelGroundAlongPath(positions, 0.5f, width * 1.5f, height);
            this.inQuad = detectTunelEntrance(positions, 0.5f, width, height);
            Vector3[] reversePath = positions.Reverse().ToArray();
            this.outQuad = detectTunelEntrance(reversePath, 0.5f, width, height);

            Vector3 vDir = Vector3.Normalize(inQuad[3] - inQuad[0]);
            vDir.Z = 0;

            Vector3 vTunnel = Vector3.Normalize(positions[1] - positions[0]);

            //inner walls
            structure = BOquads.createPlane(
                inQuad[0] - vTunnel * pilasseWidth,
                inQuad[1] - vTunnel * pilasseWidth,
                outQuad[0] + vTunnel * pilasseWidth,
                outQuad[1] + vTunnel * pilasseWidth, 
                2f, computeLength() * 2f);
            structure += BOquads.createPlane(
                outQuad[2] + vTunnel * pilasseWidth,
                outQuad[3] + vTunnel * pilasseWidth,
                inQuad[2] - vTunnel * pilasseWidth,
                inQuad[3] - vTunnel * pilasseWidth,
                2f, computeLength() * 2f);
            
            //outer walls
            //structure += BOquads.createPlane(inQuad[0], inQuad[1], inQuad[1] + vDir * pilasseWidth, inQuad[0] + vDir * pilasseWidth, 2f, pilasseWidth);
            //structure += BOquads.createPlane(inQuad[3] - vDir * pilasseWidth, inQuad[2] - vDir * pilasseWidth,inQuad[2], inQuad[3], 2f, pilasseWidth);
            //structure += BOquads.createPlane(
            //    inQuad[1] + (vDir - Vector3.UnitZ) * pilasseWidth,
            //    inQuad[1] + (vDir) * pilasseWidth,
            //    inQuad[2] + (-vDir) * pilasseWidth,
            //    inQuad[2] + (-vDir - Vector3.UnitZ) * pilasseWidth,                
            //    pilasseWidth*2f, width - 2 * pilasseWidth );

            //outer walls
            structure += BOquads.createPlane(
                inQuad[0] - vTunnel * pilasseWidth, 
                inQuad[1] - vTunnel * pilasseWidth,
                inQuad[1] - vTunnel * pilasseWidth + vDir * pilasseWidth,
                inQuad[0] - vTunnel * pilasseWidth + vDir * pilasseWidth,
                2f, pilasseWidth);
            structure += BOquads.createPlane(
                inQuad[3] - vTunnel * pilasseWidth - vDir * pilasseWidth,
                inQuad[2] - vTunnel * pilasseWidth - vDir * pilasseWidth,
                inQuad[2] - vTunnel * pilasseWidth, 
                inQuad[3] - vTunnel * pilasseWidth, 
                2f, pilasseWidth);
            structure += BOquads.createPlane(
                inQuad[1] - vTunnel * pilasseWidth + (vDir - Vector3.UnitZ) * pilasseWidth,
                inQuad[1] - vTunnel * pilasseWidth + (vDir) * pilasseWidth,
                inQuad[2] - vTunnel * pilasseWidth + (-vDir) * pilasseWidth,
                inQuad[2] - vTunnel * pilasseWidth + (-vDir - Vector3.UnitZ) * pilasseWidth,
                pilasseWidth * 2f, width - 2 * pilasseWidth);
            structure += BOquads.createPlane(
                inQuad[1] ,
                inQuad[2] ,
                inQuad[2] - vTunnel * pilasseWidth ,
                inQuad[1] - vTunnel * pilasseWidth ,
                2f, pilasseWidth);

            roofAndGround = BOquads.createPlane(inQuad[1], inQuad[2], outQuad[3], outQuad[0], 2f, computeLength() * 2f);
            roofAndGround += BOquads.createPlane(
                outQuad[1] + vTunnel * pilasseWidth + Vector3.UnitZ * 0.001f,
                outQuad[2] + vTunnel * pilasseWidth + Vector3.UnitZ * 0.001f,
                inQuad[3] - vTunnel * pilasseWidth + Vector3.UnitZ * 0.001f,
                inQuad[0] - vTunnel * pilasseWidth + Vector3.UnitZ * 0.001f,
                2f, computeLength() * 2f);
            //world.setVertexPosition(
            //world.deleteQuad((int)inQuad[0].X, (int)inQuad[1].X, (int)inQuad[0].Y, (int)inQuad[2].Y);

            world.reshapeTerrainsAlongPath(positions);
            

            positions[0] = (inQuad[0] + inQuad[3]) / 2;
            positions[1] = (outQuad[1] + outQuad[2]) / 2;
            ComputeGeometry();
            base.bind();
        }
        //return vertex index from entrance
        public Vector3[] detectTunelEntrance(Vector3[] positions, float resolution = 0.5f, float width = 1f, float height = 1f)
        {
            Vector3[] vertices = new Vector3[4];//terrain vertices
            Vector3[] res = new Vector3[4];     //result positions

            for (int i = 0; i < positions.Length - 1; i++)
            {
                Vector3 vDir = positions[i + 1] - positions[i];

                float pente = vDir.Y / vDir.X;
                float segLength = vDir.Length;
                vDir.Normalize();

                Vector2 vPerp = new Vector2(vDir).PerpendicularLeft;
                vPerp.Normalize();
                Vector3 vPerp3 = new Vector3(vPerp);

                Vector3 v = positions[i] + vDir * resolution;


                do
                {
                    Vector2 v2 = new Vector2(v);
                    float h = world.getHeight(v2);
                    if (Math.Abs(h - v.Z) > height * 0.1f)
                    {
                        float xa = Vector3.CalculateAngle(v, Vector3.UnitX);
                        float ya = Vector3.CalculateAngle(v, Vector3.UnitY);
                        float za = Vector3.CalculateAngle(v, Vector3.UnitZ);
                        int x = (int)v.X;
                        int y = (int)v.Y;

                        if (xa < MathHelper.PiOver4)
                        {
                            //dans l'orientation x (north)
                            if (vDir.X < 0)
                            {
                                vertices[0] = new Vector3(x, y + 1, world.getHeight(x, y + 1));
                                vertices[1] = new Vector3(x + 1, y + 1, world.getHeight(x + 1, y + 1));
                                vertices[2] = new Vector3(x + 1, y, world.getHeight(x + 1, y));
                                vertices[3] = new Vector3(x, y, world.getHeight(x, y));

                                res[0] = v - vPerp3 * width * 0.5f + Vector3.UnitZ * height;
                                res[1] = v - vPerp3 * width * 0.5f;
                                res[2] = v + vPerp3 * width * 0.5f;
                                res[3] = v + vPerp3 * width * 0.5f + Vector3.UnitZ * height;

                                world.deleteQuad((int)vertices[0].X, (int)vertices[1].X, (int)vertices[2].Y, (int)vertices[0].Y);
                                //orientation x négative
                            }
                            else
                            {
                                //direction positive
                                vertices[0] = new Vector3(x, y + 1, world.getHeight(x, y + 1));
                                vertices[1] = new Vector3(x + 1, y + 1, world.getHeight(x + 1, y + 1));
                                vertices[2] = new Vector3(x + 1, y, world.getHeight(x + 1, y));
                                vertices[3] = new Vector3(x, y, world.getHeight(x, y));

                                res[0] = v + vPerp3 * width * 0.5f;
                                res[1] = v + vPerp3 * width * 0.5f + Vector3.UnitZ * height;
                                res[2] = v - vPerp3 * width * 0.5f + Vector3.UnitZ * height;
                                res[3] = v - vPerp3 * width * 0.5f;

                                world.deleteQuad((int)vertices[0].X, (int)vertices[1].X, (int)vertices[2].Y, (int)vertices[0].Y);
                            }
                            
                            for (int j = 0; j < 4; j++)
                            {
                                world.setVertexPositionInWorldCoordonate(vertices[j], res[j]);
                            }
                            //if (za > MathHelper.PiOver2)
                            //{

                            //}
                            //else
                            //{
                            //    res[0] = new Vector3(x, y + 1, getHeight(x, y + 1));
                            //    res[1] = new Vector3(x + 1, y + 1, getHeight(x + 1, y + 1));
                            //    res[2] = new Vector3(x + 1, y, getHeight(x + 1, y));
                            //    res[3] = new Vector3(x, y, getHeight(x, y));
                            //}
                            //Game.selectedPos = new Vector3(x, y, getHeight(x, y));
                            Debug.WriteLine("tunel creation: aX:{0} aY:{1} aZ:{2}", xa, ya, za);    
                            return res;
                        }
                    }

                    v += vDir * resolution;

                } while ((v - positions[i]).Length < segLength);
            }
            return res;
        }
        public override void preBind()
        {
            base.preBind();
        }
        public override void Prepare()
        {
            base.Prepare();
            structure.Prepare();
            roofAndGround.Prepare();
        }
        public override void Render()
        {
            base.Render();            
            //GL.Enable(EnableCap.Lighting);
            GL.Color3(Color.Gray);
            GL.BindTexture(TextureTarget.Texture2D, World.brick);
            GL.Enable(EnableCap.Texture2D);
            structure.Render();
            GL.BindTexture(TextureTarget.Texture2D, World.concrete);
            GL.Enable(EnableCap.Texture2D);
            roofAndGround.Render();
            GL.Disable(EnableCap.Texture2D);
        }
        public void RenderInAndOutQuads()
        {
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.ProgramPointSize);
            GL.PointSize(5f);
            GL.Begin(BeginMode.Points);
            GL.Color3(Color.Red);
            GL.Vertex3(inQuad[0]);
            GL.Vertex3(outQuad[0]);
            GL.Color3(Color.Blue);
            GL.Vertex3(inQuad[1]);
            GL.Vertex3(outQuad[1]);
            GL.Color3(Color.Yellow);
            GL.Vertex3(inQuad[2]);
            GL.Vertex3(outQuad[2]);
            GL.Color3(Color.Green);
            GL.Vertex3(inQuad[3]);
            GL.Vertex3(outQuad[3]);
            GL.End();
            GL.Enable(EnableCap.DepthTest);        
        }
    }
}
