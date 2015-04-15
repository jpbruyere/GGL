using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;

namespace GGL
{
    public class ProceduralGarage : ProceduralBuilding
    {
        public ProceduralGarage(Vector3 _position, Vector3 _vdir, Vector3 dimensions, float _roofHeight) :
            base(_position,_vdir,dimensions,_roofHeight)
        {
        }

        public override void build()
        {
            Vector3 vDirPerp = new Vector3(new Vector2(vDir).PerpendicularLeft);

            Vector3 vHalfLength = vDir * length;
            Vector3 vHalfWidth = vDirPerp * width;
            Vector3 vHeightStage = new Vector3(0, 0, stageHeight);
            Vector3 vRoofHeight = new Vector3(0, 0, roofHeight);


            BOquads front = BOquads.createPlane
                (
                    position + vHalfLength + vHalfWidth,
                    position + vHeightStage + vHalfLength + vHalfWidth,
                    position + vHeightStage - vHalfLength + vHalfWidth,
                    position - vHalfLength + vHalfWidth,
                    0.5f, 1f, 0.0f, 0.25f
                );
            BOquads back = BOquads.createPlane
                (
                    position - vHalfLength - vHalfWidth,
                    position + vHeightStage - vHalfLength - vHalfWidth,
                    position + vHeightStage + vHalfLength - vHalfWidth,
                    position + vHalfLength - vHalfWidth,
                    0.5f, 1f, 0.0f, 0.25f
                );

            BOquads left1 = BOquads.createPlane
                (
                    position + vHalfLength - vHalfWidth,
                    position + vHeightStage + vHalfLength - vHalfWidth,
                    position + vHeightStage + vRoofHeight + vHalfLength,
                    position + vHalfLength,
                    0.815f, 1f, 0.47f, 0.68f, true
                );
            BOquads left2 = BOquads.createPlane
                (
                    position + vHalfLength,
                    position + vHeightStage + vRoofHeight + vHalfLength,
                    position + vHeightStage + vHalfLength + vHalfWidth,
                    position + vHalfLength + vHalfWidth,
                    1f, 0.815f, 0.47f, 0.68f, true
                );

            BOquads right1 = BOquads.createPlane
                (
                    position - vHalfLength + vHalfWidth,
                    position + vHeightStage - vHalfLength + vHalfWidth,
                    position + vHeightStage + vRoofHeight - vHalfLength,
                    position - vHalfLength,
                    0.30f, 0.5f, 0.4f, 0.65f, true
                );
            BOquads right2 = BOquads.createPlane
                (
                    position - vHalfLength,
                    position + vHeightStage + vRoofHeight - vHalfLength,
                    position + vHeightStage - vHalfLength - vHalfWidth,
                    position - vHalfLength - vHalfWidth,
                    0.30f, 0.5f, 0.4f, 0.65f, true
                );

            Vector3 roofDir = Vector3.Normalize(vRoofHeight - vHalfWidth);
            BOquads roofFront = BOquads.createPlane
            (
                position + vHeightStage + vHalfLength + vHalfWidth - Vector3.Normalize(vRoofHeight - vHalfWidth - vHalfLength) * roofGap,
                position + vHeightStage + vRoofHeight + vHalfLength + vDir * roofTopGap,
                position + vHeightStage + vRoofHeight - vHalfLength - vDir * roofTopGap,
                position + vHeightStage - vHalfLength + vHalfWidth - Vector3.Normalize(vRoofHeight - vHalfWidth + vHalfLength) * roofGap,
                0.0f, 0.7f, 0.68f, 1.0f
            );

            BOquads roofBack = BOquads.createPlane
            (
                position + vHeightStage - vHalfLength - vHalfWidth - Vector3.Normalize(vRoofHeight + vHalfWidth + vHalfLength) * roofGap,
                position + vHeightStage + vRoofHeight - vHalfLength - vDir * roofTopGap,
                position + vHeightStage + vRoofHeight + vHalfLength + vDir * roofTopGap,
                position + vHeightStage + vHalfLength - vHalfWidth - Vector3.Normalize(vRoofHeight + vHalfWidth - vHalfLength) * roofGap,
                stageHeight * tileFactor, width * tileFactor
            );

            Structure = front + back + left1 + left2 + right1 + right2 + roofFront + roofBack;



        }

    }
}
