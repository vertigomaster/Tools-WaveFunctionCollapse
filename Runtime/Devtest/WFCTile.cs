using IDEK.Tools.WorldManagement.Unity;
using System.Collections.Generic;
using System;

namespace IDEK.DevTest.WFC
{
    public class WFCTile : WFCTile<WFCTile> { }

    public class WFCTile<TRuntimeType> : Vector3GridTile<TRuntimeType>
        where TRuntimeType : WFCTile<TRuntimeType>
    {
        public int elementID = -1;

        //public new World<Vector3, Vector3Int, WFCTile> World { get; set; }

        [Obsolete("This one is for quick testing only. Should remove this as soon as possible.")]
        public string devtestCollapsedPrefabName;

        /// <summary>
        /// Used to Answer the question "Are you next to an X?"
        /// </summary>
        protected HashSet<int> neighboringElementIDs = new();

        public bool IsAdjacentToElementID(int elementID)
        {
            foreach(TRuntimeType n in Neighbors)
            {
                if(n.elementID == elementID) return true;
            }

            return false;

            //return neighboringElementIDs.Contains(elementID);
        }

        public override void RefreshNeighbors()
        {
            base.RefreshNeighbors();

            neighboringElementIDs.Clear();
            foreach(TRuntimeType neighbor in Neighbors)
            {
                neighboringElementIDs.Add(neighbor.elementID);
            }
        }
    }
}