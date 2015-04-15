using System;
using OpenTK;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;

namespace GGL
{
	public class ParticleInstance
	{
		public static List<ParticleInstance> Particles = new List<ParticleInstance>();
		public static void UpdateParticles()
		{
			int i = 0;
			while(i<Particles.Count)
			{
				ParticleInstance p = Particles [i];

				if (p.velocity != Vector3.Zero)
					p.position += p.velocity;

				MultiSpriteMaterial mss = p.particle.material as MultiSpriteMaterial;
				if ((mss!=null) && (p.particle.repeat > 0)) {
					if (p.startFrame<0)
						p.startFrame = (Material.multiSpriteShader as MultiSpriteShader).frame;
					int curFrame = (Material.multiSpriteShader as MultiSpriteShader).frame;
					if (curFrame - p.startFrame >= (p.particle.repeat * mss.nbSprites)) {
						//destroy particle
						Particles.RemoveAt (i);
						continue;
					}
				}
				i++;
			}

		}
		public static void RenderParticles()
		{
			foreach (ParticleInstance p in Particles) {
				p.Render ();
			}
		}

		public Particle particle;
		public Vector3 velocity;
		public Vector3 acceleration;
		public Vector3 position;
		public int startFrame=-1;

		public ParticleInstance (Particle _particle, Vector3 _initialPosition, 
			Vector3 _velocity, Vector3 _acceleration)
		{
			particle = _particle;
			position = _initialPosition;
			velocity = _velocity;
			acceleration = _acceleration;
		}

		public void Render()
		{
			MultiSpriteShader mss = particle.material.shader as MultiSpriteShader;
			if (mss != null) {
				if (startFrame < 0)
					return;
				else
					mss.startFrame = startFrame;
			}

			GL.PushMatrix ();
			GL.Translate (position);

			particle.Render ();

			GL.PopMatrix ();
		}
	}
}

