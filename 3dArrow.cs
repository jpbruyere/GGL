using System;
using OpenTK;
using System.Diagnostics;

namespace GGL
{
	public class Arrow3d : vaoMesh
	{
		public Arrow3d (Vector3 startPos, Vector3 endPos, Vector3 vUp) 
		{
			Vector3 vArrow = endPos - startPos;
			Vector3 h1 = startPos + vUp;
			Vector3 h2 = endPos + vUp;
			int resolution = 40;

			positions = new Vector3[resolution*2];
			indices = new ushort[resolution * 2];

			Vector3 vPerp = new Vector3 
				((new Vector2 (vArrow.X, vArrow.Y)).PerpendicularLeft);
			vPerp.NormalizeFast ();
			vPerp *= 0.1f;

			Debug.Print ("{0} {1} {2} {3}", vArrow.ToString (), h1, h2, vPerp);

			for (int i = 0; i < resolution; i++) {
				float t = i / (float)(resolution-1);

				//Vector3 p = Vector3.Lerp(h1,h2,t);
				Vector3 vd = vPerp;
				if (i == resolution - 2) {
					vd *= 2.5f;
					t = (i - 1) / (float)(resolution - 1);
				} else if (i == resolution - 1)
					vd *= 0.001f;
				else
					vd *= 1.0f-t;

				Vector3 p = Path.CalculateBezierPoint (t, startPos, h1, h2, endPos);

				positions [i*2] = p - vd;
				positions [i*2+1] = p + vd;
				indices [i * 2] = (ushort)(i * 2);
				indices [i * 2 + 1] = (ushort)(i * 2 + 1);
			}

			CreateVBOs ();
			CreateVAOs ();
		}
	}
}

