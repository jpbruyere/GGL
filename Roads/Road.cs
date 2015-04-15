using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Diagnostics;
using OpenTK.Input;
using GGL;

namespace GGL
{
    [Serializable]
    public class Road : SelectableObject
    {
        public const int NormalRoad = 0;
        public const int HighWay = 1;
        public const int ProvinceRoad = 2;
        public const int Railroad = 3;

        public static Road currentRoad
        {
            get { return World.CurrentWorld.roads[selectedRoadIndex]; }
            set
            {
                selectedRoadIndex = World.CurrentWorld.roads.IndexOf(value);
            }
        }
        public static GenericRoadSegment currentSegment = null;

        [NonSerialized]
        public static List<Texture> textures = new List<Texture>();

        public static bool bridgeCreation = false;
        public static bool tunnelCreation = false;
        public static bool smoothRoadLeveling = false;


        public static void loadTextures()
        {
            textures.Add(new Texture(directories.rootDir + @"Images\texture\terrain\road1.jpg", true));
            textures.Add(new Texture(directories.rootDir + @"Images\texture\terrain\roadDouble1.jpg", true));
            textures.Add(new Texture(directories.rootDir + @"Images\texture\terrain\road3.jpg", true));
            textures.Add(new Texture(directories.rootDir + @"Images\texture\terrain\rail3.jpg", true));
            textures.Add(new Texture(directories.rootDir + @"Images\texture\terrain\rail5.jpg", true));
        }

        public static void createDefaultRoutes()
        {
            World.CurrentWorld.addRoad(new Road
            {
                textureIndex = 0,
                width = 0.25f
            });

            World.CurrentWorld.addRoad(new Road
            {
                textureIndex = 1,
                width = 0.5f
            });

            World.CurrentWorld.addRoad(new Road
            {
                textureIndex = 2,
                width = 0.25f
            });

            World.CurrentWorld.addRoad(new Road
            {
                textureIndex = 3,
                width = 0.25f,
                textureTile = 1f
            });

            World.CurrentWorld.addRoad(new Road
            {
                textureIndex = 4,
                width = 0.25f,                
            });

            //Road.currentRoad = r; 
        }

        [NonSerialized]
        public int id = 0;

        public int textureIndex = 0;
        public float textureTile = 2f;
        [NonSerialized]
        static int _selectedRoadIndex = 0;
        public static int selectedRoadIndex
        {
            get { return _selectedRoadIndex; }
            set
            {
                if (value < 0)
                    _selectedRoadIndex = World.CurrentWorld.roads.Count - 1;
                else if (value > World.CurrentWorld.roads.Count - 1)
                    _selectedRoadIndex = 0;
                else
                    _selectedRoadIndex = value;
            }
        }

        public bool edit = false;

        public List<GenericRoadSegment> segments = new List<GenericRoadSegment>();

        public GenericRoadSegment newSegment = null;

        Vector2 _defaultDir = Vector2.UnitX;

        int _currentHandleInNewSegment = -1;
        public int currentHandleInNewSegment
        {
            get { return _currentHandleInNewSegment; }
            set
            {
                if (value < 0)
                    _currentHandleInNewSegment = 3;
                else if (value > 3)
                    _currentHandleInNewSegment = 0;
                else
                    _currentHandleInNewSegment = value;
            }
        }


        public Vector2 startPosition
        {
            get { return new Vector2(Mouse3d.Position); }
        }

        Vector3 _vNewSegmentDir;
        public Vector3 vNewSegmentDir
        {
            get { return _vNewSegmentDir; }
            set
            {
                _vNewSegmentDir = value;
            }
        }


        public Vector3 vDir
        {
            get
            {
                if (vNewSegmentDir == null)
                    return new Vector3(_defaultDir);
                else
                    if (vNewSegmentDir == Vector3.Zero)
                        return new Vector3(_defaultDir);
                    else
                        return vNewSegmentDir;
            }
        }


        float _width = 0.25f;
        public float width
        {
            get { return _width; }
            set { _width = value; }
        }

        public float _length = 10f;
        int _defaultPathSegmentCount = 10;

        public static bool newSegmentInit = false;

        public World world;

        public void newSegmentDirectionUpdate()
        {
            for (int h = 1; h < 4; h++)
            {
                newSegment.handles[h] = newSegment.handles[h - 1] + vNewSegmentDir * _length / 3;
                newSegment.preBind();
            }
        }



        public bool parrallelRight = false;

        public float tolerance = 0.2f;

        public void checkLinkForNewSegment()
        {
            if (newSegment == null) 
                return;
            if (newSegment.handleStartIsLinked && currentHandleInNewSegment == 0 && Mouse3d.Delta.LengthFast < tolerance * 5f)
                return;
            if (newSegment.handleEndIsLinked &&
                (   (currentHandleInNewSegment == 3 && newSegment is GenericRoadSegment) || 
                    (currentHandleInNewSegment == 1 && newSegment is GenericRoadSegment)) 
                    && Mouse3d.Delta.LengthFast < tolerance * 5f)
                return;

            bool linked = false;
            //link new segment with currentsegment
            for (int i = 0; i < Road.currentSegment.nbPathPoints; i++)
            {
                Vector3 diff = Road.currentSegment.positions[i] - Mouse3d.Position;
                if (diff.Length < tolerance )
                {
                    linked = true;

                    newSegment.handles[currentHandleInNewSegment] = Road.currentSegment.positions[i];

                    if (currentHandleInNewSegment == 0)
                    {
                        newSegment.handleStartLinkedIndex = i;
                        newSegment.handleStartIsLinked = true;
                        newSegment.handleStartLinkReference = Road.currentSegment;

                        //if (newSegment.handle3IsLinked)
                        //    computeParralelDirection(0);
                    }
                    else
                    {
                        newSegment.handleEndLinkedIndex = i;
                        newSegment.handleEndIsLinked = true;
                        newSegment.handleEndLinkReference = Road.currentSegment;
                        //if (newSegment.handle0IsLinked)
                        //    computeParralelDirection(3);
                    }

                    if (newSegment is GenericRoadSegment)
                    {
                        if (newSegment.handleStartIsLinked && newSegment.handleEndIsLinked && (newSegment.handleStartLinkReference != newSegment.handleEndLinkReference) 
                            && (i!=0&&i!=Road.currentSegment.nbPathPoints-1))
                        {
                            //position handles 1-2 for junction
                            Vector3 v1 = newSegment.handleStartLinkReference.positions[newSegment.handleStartLinkedIndex];
                            Vector3 v2 = newSegment.handleEndLinkReference.positions[newSegment.handleEndLinkedIndex];
                            Vector3 junctionDir = v2 - v1;
                            junctionDir.Normalize();

                            Vector3 vPathDirPos = newSegment.handleStartLinkReference.getPositiveDirection(newSegment.handleStartLinkedIndex);
                            Vector3 vPathDirNeg = newSegment.handleStartLinkReference.getNegativeDirection(newSegment.handleStartLinkedIndex);

                            float angle = Vector3.CalculateAngle(junctionDir, vPathDirPos);
                            if (angle > MathHelper.PiOver2)
                            {
                                //select other dir
                                newSegment.handles[1] = newSegment.handles[0] + vPathDirNeg * newSegment.computeLength() / 3f;
                            }
                            else
                            {
                                newSegment.handles[1] = newSegment.handles[0] + vPathDirPos * newSegment.computeLength() / 3f;
                                //newSegment.handles[2] = newSegment.handles[3] - 
                                //    newSegment.handle3LinkReference.getNegativeDirection(newSegment.handle3LinkedIndex) 
                                //    *newSegment.computeLength() / 3f;
                            }

                            vPathDirPos = newSegment.handleEndLinkReference.getPositiveDirection(newSegment.handleEndLinkedIndex);
                            vPathDirNeg = newSegment.handleEndLinkReference.getNegativeDirection(newSegment.handleEndLinkedIndex);

                            angle = Vector3.CalculateAngle(junctionDir, vPathDirPos);
                            if (angle > MathHelper.PiOver2)
                            {
                                //select other dir
                                newSegment.handles[2] = newSegment.handles[3] - vPathDirNeg * newSegment.computeLength() / 3f;
                            }
                            else
                            {
                                newSegment.handles[2] = newSegment.handles[3] - vPathDirPos * newSegment.computeLength() / 3f;
                            }

                        }
                        else
                        {
                            if (i == 0)
                            {
                                if (currentHandleInNewSegment == 0)
                                    newSegment.handles[1] = newSegment.handles[0] + Road.currentSegment.startVector * newSegment.computeLength()/3;
                                else
                                    newSegment.handles[2] = newSegment.handles[3] + Road.currentSegment.startVector * newSegment.computeLength() / 3;
                            }
                            else if (i == Road.currentSegment.nbPathPoints - 1)
                            {
                                if (currentHandleInNewSegment == 0)
                                    newSegment.handles[1] = newSegment.handles[0] + Road.currentSegment.endVector * newSegment.computeLength() / 3;
                                else
                                    newSegment.handles[2] = newSegment.handles[3] + Road.currentSegment.endVector * newSegment.computeLength() / 3;
                            }
                            else
                            {
                                if (newSegment is GenericRoadSegment)
                                    (newSegment as GenericRoadSegment ).computeHandle1_2WhenSingleLinked(i, Road.currentSegment as GenericRoadSegment, currentHandleInNewSegment);
                            }
                        }
                    }

                    break;
                }
            }
            if (!linked)
            {
                moveHandleToMousePosition();
            }
            Debug.WriteLine("linked = " + linked);
        }
        //public void computeParralelDirection(int h)
        //{
        //    Vector3 hDir = newSegment.handles[h];
        //    hDir.Normalize();
        //    Vector3 mouseDir = Mouse3d.Position;
        //    float mhAngle = Vector3.CalculateAngle(hDir, mouseDir);
        //    Debug.WriteLine(MathHelper.RadiansToDegrees(mhAngle));

        //}
        
        //
        public void moveHandleToMousePosition()
        {
            Vector3 hDiff;
            switch (currentHandleInNewSegment)
            {
                case 0:
                    hDiff = newSegment.handles[0] - newSegment.handles[1];
                    newSegment.handles[0] = Mouse3d.Position;                    
                    newSegment.handleStartLinkedIndex = 0;
                    newSegment.handleStartIsLinked = false;
                    if (newSegment is GenericRoadSegment)
                    {
                        newSegment.handles[1] = newSegment.handles[0] - hDiff;
                        if (newSegment.handleEndIsLinked && newSegment.handleEndLinkReference is GenericRoadSegment)
                            (newSegment as GenericRoadSegment).computeHandle1_2WhenSingleLinked(newSegment.handleEndLinkedIndex, newSegment.handleEndLinkReference as GenericRoadSegment, 3);
                    }
                    break;
                case 1:
                    if (newSegment is GenericRoadSegment)
                    {
                        if (!newSegment.handleStartIsLinked)
                            newSegment.handles[1] = Mouse3d.Position;
                    }
                    else 
                    {
                        newSegment.handles[1] = Mouse3d.Position;
                        newSegment.handleEndLinkedIndex = 0;
                        newSegment.handleEndIsLinked = false;
                    }
                    break;
                case 2:
                    if (!newSegment.handleEndIsLinked)
                        newSegment.handles[2] = Mouse3d.Position;
                    break;
                case 3:
                    hDiff = newSegment.handles[3] - newSegment.handles[2];
                    newSegment.handles[3] = Mouse3d.Position;
                    newSegment.handles[2] = newSegment.handles[3] - hDiff;
                    newSegment.handleEndLinkedIndex = 0;
                    newSegment.handleEndIsLinked = false;
                    if (newSegment.handleStartIsLinked && newSegment.handleEndLinkReference is GenericRoadSegment)
                        (newSegment as GenericRoadSegment).computeHandle1_2WhenSingleLinked(newSegment.handleStartLinkedIndex, newSegment.handleStartLinkReference as GenericRoadSegment, 0);

                    break;
            }
        }

 
        public void render()
        {
            GL.UseProgram(0);
            if (World.roadEdition)
            {
                if (currentSegment != null)                
                    currentSegment.RenderGeom();

                if (newSegment != null)
                    newSegment.Render();
            }

            foreach (GenericRoadSegment rs in segments)            
                rs.Render();
            


        }

        public void bind(World _world)
        {
            world = _world;
            id = ModelInstance.NextAvailableID;
            SelectableObject.objectsDic.Add(id, this);
            world.roads.Add(this);
            Road.currentRoad = this;
        }
    }




}
