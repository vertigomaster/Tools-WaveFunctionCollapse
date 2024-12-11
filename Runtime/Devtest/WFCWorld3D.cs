using IDEK.Tools.Logging;
using IDEK.Tools.ShocktroopExtensions;
using IDEK.Tools.WorldManagement.CoreLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace IDEK.DevTest.WFC
{
    [Serializable]
    public class WFCWorld3D : World<Vector3, Vector3Int, WFCTile>
    {
        public WFCTile[,,] tileMatrix;
        public Vector3 tileSize = Vector3.one;
        internal Vector3 scaleFactor = Vector3.one;

        public override bool TryGetTileAtIndex(Vector3Int newTileIndex, out WFCTile tile)
        {
            tile = default;
            if(newTileIndex.x < 0 || newTileIndex.x >= tileMatrix.GetLength(0)) return false;
            if(newTileIndex.y < 0 || newTileIndex.y >= tileMatrix.GetLength(1)) return false;
            if(newTileIndex.z < 0 || newTileIndex.z >= tileMatrix.GetLength(2)) return false;

            tile = tileMatrix[newTileIndex.x, newTileIndex.y, newTileIndex.z];

            if(tile.TileIndex != newTileIndex)
            {
                //honestly, the need for this error message is a sign that tile.TileIndex should be DERIVED from the tile matrix.
                //though that would still require a reverse LUT, so not really any considerable memory savings...
                //ether that or re-running a search every time
                ConsoleLog.LogError("Data Integrity Error: " +
                    "tile's registered index does not match the index they were found at!\n" +
                    $"Tile says they are at {tile.TileIndex}\n" +
                    $"But its Tile Matrix index = {newTileIndex}");
            }

            return true;
        }

        public bool TryGetTileAtIndex(int x, int y, int z, out WFCTile tile) => TryGetTileAtIndex(new(x, y, z), out tile);

        public override Vector3Int GetTileContainingRawPosition(Vector3 rawPosition)
        {
            return Vector3Int.FloorToInt(rawPosition.Mult(tileSize.Inverse()));
        }

        public void PopulateMatrix()
        {
            PopulateMatrixWithObjects();
            RebuildNeighborData();
        }

        protected void PopulateMatrixWithObjects()
        {
            WFCTile temp;

            ForEachTileIndex(index =>
            {
                temp = new WFCTile();

                temp.TileIndex = index;
                temp.RawPosition = ((Vector3)index).Mult(tileSize).Mult(scaleFactor);
                temp.World = this;

                tileMatrix[index.x, index.y, index.z] = temp;
            });
        }

        protected void RebuildNeighborData()
        {
            foreach(WFCTile tile in tileMatrix)
            {
                tile.RefreshNeighbors();
            }
        }

        public void ForEachTileIndex(Action<Vector3Int> callback)
        {
            for(int i = 0; i < tileMatrix.GetLength(0); i++)
            {
                for(int j = 0; j < tileMatrix.GetLength(1); j++)
                {
                    for(int k = 0; k < tileMatrix.GetLength(2); k++)
                    {
                        callback(new(i, j, k));
                    }
                }
            }
        }
    }
}
