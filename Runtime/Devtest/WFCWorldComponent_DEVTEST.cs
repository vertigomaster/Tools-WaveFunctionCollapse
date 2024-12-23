using System.Collections.Generic;
using UnityEngine;
using IDEK.WFC.Basic;

namespace IDEK.DevTest.WFC
{
    public class WFCWorldComponent_DEVTEST : WFCWorldComponent
    {
        protected override void SetScoreWeights(WFCTile tile, float[] tileWeights)
        {
            for(int i = 0; i < tileWeights.Length; i++) 
            {
                tileWeights[i] = 0;
            }

            // Example rule: if air directly below, we must be air
            if(tile.Below != null && tile.Below.elementID == 0)
            {
                tileWeights[0] = float.PositiveInfinity;
                return;
            }
            else if(tile.Above != null && tile.Above.elementID > 0)
            {
                tileWeights[0] = float.NegativeInfinity;
            }

            // Example rule: if next to water, can't be grass
            if(tile.IsAdjacentToElementID(3))
            {
                tileWeights[1] = -1f;
            }

            // Example rule: if next to grass, can't be water
            if(tile.IsAdjacentToElementID(1))
            {
                tileWeights[3] = -1f;
            }

            // Example rule: solids can't be on top of water
            if(tile.Above != null && (tile.Above.elementID > 0 && tile.Above.elementID != 3))
            {
                tileWeights[3] = -1f;
            }
        }
    }
}
