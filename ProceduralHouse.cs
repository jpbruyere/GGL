using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;

namespace GGL
{
    public class ProceduralHouse : ProceduralBuilding
    {
        public ProceduralHouse(Vector3 dimensions, float _roofHeight = 0.5f, bool withGarage = false) :
            base(dimensions, _roofHeight)
        {

            if (withGarage)
                Structure += NewGarage(vDir, width, width, stageHeight * 0.8f, 0.2f);

            Prepare();
        }
        public ProceduralHouse(float _length, float _width, float _height, float _roofHeight, bool withGarage = false)
                    : base(_length,_width,_height,_roofHeight)
        {

            if (withGarage)
                Structure += NewGarage(vDir, width, width, stageHeight * 0.8f, 0.2f);

            Prepare();
        }
        
        public ProceduralHouse(Vector3 _position, Vector3 _vdir, Vector3 dimensions, float _roofHeight, bool withGarage = false):
            base(_position,_vdir,dimensions,_roofHeight)
        {
            if (withGarage)
                Structure += NewGarage(vDir, width, width, stageHeight * 0.8f, 0.2f);        
        }

        public static ProceduralHouse createHousesAlongPath(GGL.Path p)
        {
            Vector3 dimensions = new Vector3(0.5f, 0.4f, 0.3f);
            ProceduralHouse houses = new ProceduralHouse(p.positions[0], Vector3.UnitX, dimensions, 0.1f);

            for (int i = 1; i < p.positions.Length; i++)
            {
                

                ProceduralHouse ph = new ProceduralHouse(p.positions[i],p.getPathPerpendicularDirection(i) , dimensions, 0.07f);
                ph.Prepare();

                World.CurrentWorld.makePlanar(
                    (int)ph.bounds.x0-1,
                    (int)ph.bounds.y0-1,
                    (int)ph.bounds.x1+1,                    
                    (int)ph.bounds.y1+1);

                float heightdiff = World.CurrentWorld.getHeight(p.positions[i].X, p.positions[i].Y) - p.positions[i].Z;
                ph.Structure.ChangeHeight(heightdiff);

                houses.Structure += ph.Structure;
            }

            houses.Prepare();
            return houses;
        }


        

        public BOquads NewGarage(Vector3 gvDir, float glength, float gwidth, float gheight, float groofHeight)
        {
            Vector3 vDirPerp = new Vector3(new Vector2(vDir).PerpendicularLeft);
            Vector3 vHalfLength = vDir * length;
            Vector3 vHalfWidth = vDirPerp * width;
            Vector3 gvDirPerp = new Vector3(new Vector2(gvDir).PerpendicularLeft);
            Vector3 gvHalfLength = gvDir * glength;
            Vector3 gvHalfWidth = gvDirPerp * gwidth;

            BOquads garage = new ProceduralGarage(
                position - vHalfLength - vHalfWidth - gvHalfLength - gvHalfWidth, gvDir, new Vector3(gwidth, glength, gheight),
                0.2f).Structure;

            return garage;
        }
    }
}
