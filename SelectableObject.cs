using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GGL
{
    [Serializable]
    public class SelectableObject
    {
        [NonSerialized]
        public int id;

        static int _NextAvailableID = 100;

        public static SelectableObject selectedObject;
        public static Dictionary<int, Object> objectsDic = new Dictionary<int, Object>();
        public static void registerObject(SelectableObject o)
        {
            o.id = NextAvailableID;
            objectsDic.Add(o.id, o);        
        }
        public static void unregisterObject(SelectableObject o)
        {
            objectsDic.Remove(o.id);
        }


        public static int NextAvailableID
        {
            get
            {
                int id = _NextAvailableID;
                _NextAvailableID++;
                return id;
            }
        }



    }
}
