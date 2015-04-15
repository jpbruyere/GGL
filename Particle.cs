using System;

namespace GGL
{
	public class Particle
	{
		BOquads plane;
		public Material material;
		public int repeat;	//0 => infinite loop

		public Particle (float _width, float _height, Material _material, int _repeat=0)
		{
			plane = BOquads.createPlaneZup(_width, _height, 1f, 1f);
			plane.Prepare ();
			material = _material;
			repeat = _repeat;
		}

		public void Render()
		{			 
			material.Enable ();
			plane.Render ();
			material.Disable ();
		}
	}
}

