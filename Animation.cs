//
//  Animation.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2015 jp
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using OpenTK;

namespace GGL
{
	public delegate void AnimationEventHandler(Animation a);

    public delegate float GetterDelegate();
    public delegate void SetterDelegate(float value);

//	public class AnimationList : List<AnimationList> ,IAnimatable
//	{
//		#region IAnimatable implementation
//
//		public event EventHandler<EventArgs> AnimationFinished;
//
//		public void Animate (float ellapseTime = 0f)
//		{
//			throw new NotImplementedException ();
//		}
//
//		#endregion
//
//
//	}

    public class Animation
    {
		public event AnimationEventHandler AnimationFinished;

		public static Random random = new Random ();
		public static int DelayMs = 0;

        protected GetterDelegate getValue;
        protected SetterDelegate setValue;

        public string propertyName;

        protected Stopwatch timer = new Stopwatch();
        protected int delayStartMs = 0;
		/// <summary>
		/// Delay before firing ZnimationFinished event.
		/// </summary>
		protected int delayFinishMs = 0;
        public static List<Animation> AnimationList = new List<Animation>();

        //public FieldInfo member;
        public Object AnimatedInstance;

		public Animation(Object instance, string _propertyName)
		{
			propertyName = _propertyName;
			AnimatedInstance = instance;
			PropertyInfo pi = instance.GetType().GetProperty(propertyName);
			getValue = (GetterDelegate)Delegate.CreateDelegate(typeof(GetterDelegate), instance, pi.GetGetMethod());
			setValue = (SetterDelegate)Delegate.CreateDelegate(typeof(SetterDelegate), instance, pi.GetSetMethod());
		}

		public static void StartAnimation(Animation a, int delayMs = 0, AnimationEventHandler OnEnd = null)
        {

			Animation aa = null;
			if (Animation.GetAnimation (a.AnimatedInstance, a.propertyName, ref aa)) {
				aa.CancelAnimation ();
			}
//			if (a.AnimatedInstance is CardInstance) {
//				if ((a.AnimatedInstance as CardInstance).Model.Name.StartsWith ("Spider"))
// 					Debugger.Break ();
//			}
			//a.AnimationFinished += onAnimationFinished;

			a.AnimationFinished += OnEnd;
			a.delayStartMs = delayMs + DelayMs;


            if (a.delayStartMs > 0)
                a.timer.Start();
            
			AnimationList.Add (a);

        }

        static Stack<Animation> anims = new Stack<Animation>();
		static int frame = 0;
        public static void ProcessAnimations()
        {
			frame++;

//			#region FLYING anim
//			if (frame % 20 == 0){
//				foreach (Player p in MagicEngine.CurrentEngine.Players) {
//					foreach (CardInstance c in p.InPlay.Cards.Where(ci => ci.HasAbility(AbilityEnum.Flying) && ci.z < 0.4f)) {
//						
//					}
//				}
//			}
//			#endregion
            //Stopwatch animationTime = new Stopwatch();
            //animationTime.Start();
			 
			const int maxAnim = 200000;
			int count = 0;


			lock (AnimationList) {
				if (anims.Count == 0)
					anims = new Stack<Animation> (AnimationList);
			}
        
			while (anims.Count > 0 && count < maxAnim) {
				Animation a = anims.Pop ();	
				if (a == null)
					continue;
				if (a.timer.IsRunning) {
					if (a.timer.ElapsedMilliseconds > a.delayStartMs)
						a.timer.Stop ();
					else
						continue;
				}

				a.Process ();
				count++;
			}
				
            //animationTime.Stop();
            //Debug.WriteLine("animation: {0} ticks \t {1} ms ", animationTime.ElapsedTicks,animationTime.ElapsedMilliseconds);
        }
        public static bool GetAnimation(object instance, string PropertyName, ref Animation a)
        {
			for (int i = 0; i < AnimationList.Count; i++) {
				Animation anim = AnimationList [i];
				if (anim == null) {					
					continue;
				}
				if (anim.AnimatedInstance == instance && anim.propertyName == PropertyName) {
					a = anim;
					return true;
				}
			}

            return false;
        }
		public virtual void Process () {}
        public void CancelAnimation()
        {
			//Debug.WriteLine("Cancel anim: " + this.ToString()); 
            AnimationList.Remove(this);
        }
		public void RaiseAnimationFinishedEvent()
		{
			if (AnimationFinished != null)
				AnimationFinished (this);
		}

		public static void onAnimationFinished(Animation a)
		{
			Debug.WriteLine ("\t\tAnimation finished: " + a.ToString ());
		}


    }
	public class ShakeAnimation : Animation
	{
		public float LowBound;
		public float HighBound;

		bool rising = true;

		public ShakeAnimation(
			Object instance, 
			string _propertyName, 
			float lowBound, float highBound)
			: base(instance, _propertyName)
		{
			
			LowBound = Math.Min (lowBound, highBound);
			HighBound = Math.Max (lowBound, highBound);

			float value = getValue ();

			if (value > HighBound)
				rising = false;
		}
		const float stepMin = 0.001f, stepMax = 0.005f;
		public override void Process ()
		{
			float value = getValue ();	
			float step = stepMin + (float)random.NextDouble () * stepMax;

			if (rising) {				
				value += step;
				if (value > HighBound) {
					value = HighBound;
					rising = false;
				}
			} else {
				value -= step;
				if (value < LowBound) {
					value = LowBound;
					rising = true;
				} else if (value > HighBound)
					value -= step * 10f;
			}
			setValue (value);
		}

	}
    public class FloatAnimation : Animation
    {

        public float TargetValue;
		float initialValue;
        public float Step;
		public bool Cycle;


        public FloatAnimation(Object instance, string _propertyName, float Target, float step = 0.2f)
			: base(instance, _propertyName)
        {
            
            TargetValue = Target;

            float value = getValue();
			initialValue = value;

            Step = step;

            if (value < TargetValue)
            {
                if (Step < 0)
                    Step = -Step;
            }
            else if (Step > 0)
                Step = -Step;            
        }

        /// <summary>
        /// process one frame
        /// </summary>
        public override void Process()
        {
            float value = getValue();

			//Debug.WriteLine ("Anim: {0} <= {1}", value, this.ToString ());

            if (Step > 0f)
            {
                value += Step;
                setValue(value);
                //Debug.WriteLine(value);
                if (TargetValue > value)
                    return;
            }
            else
            {
                value += Step;
                setValue(value);

                if (TargetValue < value)
                    return;
            }

			if (Cycle) {
				Step = -Step;
				TargetValue = initialValue;
				Cycle = false;
				return;
			}

            setValue(TargetValue);
            AnimationList.Remove(this);

			RaiseAnimationFinishedEvent ();
        }

		public override string ToString ()
		{
			return string.Format ("{0}:->{1}:{2}",base.ToString(),TargetValue,Step);
		}
    }

    public class AngleAnimation : FloatAnimation
    {
        public AngleAnimation(Object instance, string PropertyName, float Target, float step = 0.1f) : 
            base(instance,PropertyName,Target,step)
        {
        }
        public override void Process()
        {
            base.Process();

            float value = getValue();
            if (value < -MathHelper.TwoPi)
                setValue(value + MathHelper.TwoPi);
            else if (value >= MathHelper.TwoPi)
                setValue(value - MathHelper.TwoPi);
        }
    }
}
