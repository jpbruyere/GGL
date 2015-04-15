using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GGL;
using OpenTK;

namespace GGL
{
    [Serializable]
    public class Station : Model
    {

        public int length = 5;
        public int nbLane = 1;
        public enum Orientation
        {
            NS,
            EO
        };

        public Station(int x1, int y1, int x2, int y2, Orientation orientation)
        {
            int height = Math.Abs(x1 - x2);
            int width = Math.Abs(y1 - y2);

            World w = World.CurrentWorld;
            w.makePlanar(x1, y1, x2, y2);

            float z = w.getHeight(x1, y1) + 0.01f;
            Vector3 vDir;

            if (orientation == Orientation.NS)
            {
                length = width;
                nbLane = height * 2;
                

                for (int y = y1; y < y2; y += 2)
                {
                    BOquads c = BOquads.createCappedCube(x1, y, x2, y + 1, z, 0.1f, width*2, 2f);
                    c.Prepare();

                    w.cityPlates.Add(c);
                }
                z = w.getHeight(x1, y1);
                for (int y = y1 + 1; y < y2; y += 2)
                {
                    GenericRoadSegment rs = new GenericRoadSegment(w.roads[3]);
                    rs.IsStation = true;

                    rs.positions = new Vector3[2];
                    rs.positions[0] = new Vector3(x1, (float)y + rs.width, z);
                    rs.positions[1] = new Vector3(x2, (float)y + rs.width, z);
                    
                    vDir = rs.positions[0] - rs.positions[1];
                    vDir.Normalize();
                    rs.handles[0] = rs.positions[0];
                    rs.handles[1] = rs.positions[0] - vDir*5;
                    rs.handles[2] = rs.positions[1] + vDir*5;
                    rs.handles[3] = rs.positions[1];

                    rs.tile = length/2f;
                    rs.ComputeGeometry();
                    rs.bind();

                    rs = new GenericRoadSegment(w.roads[3]);
                    rs.positions = new Vector3[2];
                    rs.positions[0] = new Vector3(x1, (float)y + rs.width*3, z);
                    rs.positions[1] = new Vector3(x2, (float)y + rs.width*3, z);

                    vDir = rs.positions[0] - rs.positions[1];
                    vDir.Normalize();

                    rs.handles[0] = rs.positions[0];
                    rs.handles[1] = rs.positions[0] - vDir*2;
                    rs.handles[2] = rs.positions[1] + vDir*2;
                    rs.handles[3] = rs.positions[1];
                    
                    rs.tile = length/2f;
                    rs.ComputeGeometry();
                    rs.bind();
                }
            }
            else 
            {
                length = height;
                nbLane = width * 2;
            }





        }

        public override void Render()
        {
            throw new NotImplementedException();
        }
        public override void Prepare()
        {
            throw new NotImplementedException();
        }
    }
}
