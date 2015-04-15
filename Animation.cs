using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using System.Diagnostics;

namespace GGL
{
    [Serializable]
    public class Animation
    {        
        private Stopwatch watch = new Stopwatch();
        //private Random random = new Random();

        public double speed = 0.1;

        public ModelInstance mi;

        public int meshIndex;
        public Matrix4 transformation;

        private bool _IsEnable;
        public bool IsEnable
        {
            get { return _IsEnable; }
            set
            {
                if (value == _IsEnable)
                    return;

                _IsEnable = value;

                if (_IsEnable)
                    watch.Start();
                else
                    watch.Stop();
            }
        }

        private float _angle;
        public float angle
        {
            get { return _angle; }
            set
            {
                _angle = value;
                if (_angle > (float)Math.PI * 2)
                    _angle = 0f;                

                transformation =
                    Matrix4.CreateTranslation(-mi.model.Axe) *
                    Matrix4.Rotate(Vector3.UnitX, angle) *
                    Matrix4.CreateTranslation(mi.model.Axe);
            }
        }

        public void process()
        {
            if (!IsEnable || mi == null)
                return;
            //if (discardRandom)
            //    if (random.Next(10) < 1)
            //        return;


            long elapsed = watch.ElapsedMilliseconds;

            if (elapsed > speed)
            {
                watch.Restart();
                //animation
                angle += (float)((World.CurrentWorld.elapsedMiliseconds / 1000) * Math.PI * 2 * speed);
            }
        }
    }
}
