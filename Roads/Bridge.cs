using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using GGL;

namespace GGL
{
    public class Bridge : GenericRoadSegment
    {
        BOquads structure;

        public Bridge(Road _road) : 
            base(_road,false,1)
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

            inclinaisonMax = MathHelper.Pi / 16f;
    
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
                Vector3 pos = Vector3.Lerp(positions[0],positions[1],t);
                float h = world.getHeight(new Vector2(pos));
                if (h > pos.Z)
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
            tile = computeLength();
            structure = BOquads.createUncappedRectangle(geometry[0] + vboZadjustment, geometry[1] + vboZadjustment, geometry[2] + vboZadjustment, geometry[3] + vboZadjustment, vHeight.Z, computeLength() * 5f, 1f);
            
            int nbPilasses = (int)(computeLength() / 10f);
            if (nbPilasses < 2)
                nbPilasses = 2;

            for (int i = 1; i <= nbPilasses; i++)
			{
			    float t = 1f / (nbPilasses + 1) * i;
                Vector3 vMiddle = Vector3.Lerp(positions[0],positions[1],t);
                float height = world.getHeight(new Vector2(vMiddle));

                Vector3 vWidth = new Vector3((float)width * 0.8f,0, 0);
                Vector3 vLength = new Vector3(0, 0.2f, 0);

                structure += BOquads.createUncappedRectangle(
                    vMiddle - vWidth - vLength, vMiddle + vWidth - vLength, 
                    vMiddle - vWidth + vLength, vMiddle + vWidth + vLength,-vMiddle.Z+height, 1f, height * 5f);
                
			}

            base.bind();
        }
        public override void Prepare()
        {
            base.Prepare();
            structure.Prepare();
        }
        public override void Render()
        {
            base.Render();

            GL.BindTexture(TextureTarget.Texture2D, World.pavement);
            GL.Enable(EnableCap.Texture2D);
            structure.Render();
            GL.Disable(EnableCap.Texture2D);
        }
    }
}
