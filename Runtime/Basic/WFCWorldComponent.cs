using IDEK.Tools.Coroutines.TaskRoutines;
using IDEK.Tools.Logging;
using IDEK.Tools.ShocktroopExtensions;
using IDEK.Tools.WorldManagement.CoreLib.Tiles;
using IDEK.Tools.WorldManagement.Unity;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
// using Sirenix.Utilities;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IDEK.WFC.Basic
{

    [Serializable]
    public class TileData
    {
        [HorizontalGroup(width: 0.4f)]
        public string title;
        [HorizontalGroup]
        public GameObject dioramaGameObj;
    }

    public abstract class WFCWorldComponent : MonoBehaviour
    {
        public float crawlWait = 0.1f;

        public WFCWorld3D world;
        public Vector3Int worldDimensions;

        public List<TileData> tiles;

        public TileCursor<Vector3, Vector3Int, WFCTile> cursor = new();

        private void Awake()
        {
            Initialize();
        }

        private void OnValidate()
        {
            foreach(TileData tile in tiles)
            {
                if(!tile.title.IsNullOrWhitespace() || tile.dioramaGameObj == null) continue;
                tile.title = tile.dioramaGameObj.name;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            Generate();
        }

        // Update is called once per frame
        void Update()
        {

        }

        ////////////////////// Public Methods //////////////////////

        private void Initialize()
        {
            world.tileMatrix = new WFCTile[worldDimensions.x, worldDimensions.y, worldDimensions.z];
            world.scaleFactor = transform.lossyScale;
        }

        [Button]
        public void Generate()
        {
            Initialize();
            DeleteAllChildObjects();
            world.PopulateMatrix();

            StartCoroutine(CursorCrawl());
        }

        private IEnumerator CursorCrawl()
        {
            cursor.world = world;

            //TODO: remove pieces from this if we give starting sample data
            HashSet<WFCTile> unfilledTiles = world.tileMatrix.Cast<WFCTile>().ToHashSet();

            cursor.TrySetTileIndex(new(
                UnityEngine.Random.Range(0, worldDimensions.x),
                UnityEngine.Random.Range(0, worldDimensions.y),
                UnityEngine.Random.Range(0, worldDimensions.z)
            ));

            float[] tileWeightsNonAllocArray = new float[tiles.Count];

            while(unfilledTiles.Count > 0)
            {
                ConsoleLog.Log("WFC Crawl | " + unfilledTiles.Count + " unfilled Tiles remaining!");

                yield return new WaitForSeconds(crawlWait);

                FillTileWithCollapsedElement(cursor.tile, tileWeightsNonAllocArray);
                cursor.tile.RefreshNeighbors();

                unfilledTiles.Remove(cursor.tile);

                if(unfilledTiles.Count <= 0) break;

                WFCTile candidate = (WFCTile)cursor.tile.Neighbors.Where(neighbor => unfilledTiles.Contains(neighbor)).FirstOrDefault();

                ConsoleLog.Log("Attempting to move to index" + candidate?.TileIndex);

                if(cursor.TryMoveTo(candidate)) continue;

                WFCTile arbitrary = unfilledTiles.GetArbitrary();
                ConsoleLog.Log("Failed to move to neighbor, Attempting to move to arbitrary index" + arbitrary.TileIndex);

                if(!cursor.TryMoveTo(arbitrary))
                {
                    ConsoleLog.LogError("Operation exception; failed to pull and move to an arbitary tile element. " +
                        "Logic error may exist. Aborting process to allow review of interim result.");
                    break;
                }
            }

            ConsoleLog.Log("WFC Crawl complete!");
        }

        /// <summary>
        /// Fills the tile with an element
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="tileWeights"></param>
        public void FillTileWithCollapsedElement(WFCTile tile, float[] tileWeights)
        {
            TileData chosenTilePrefab = CollapseTileToElement(tile, tileWeights);
            if(chosenTilePrefab == null) return;

            ConsoleLog.Log($"WFC Crawl | Instantiating prefab \"{chosenTilePrefab.dioramaGameObj}\" at tile index {tile.TileIndex}");

            if(chosenTilePrefab.dioramaGameObj != null)
            {
                GameObject it = Instantiate(chosenTilePrefab.dioramaGameObj, tile.RawPosition + transform.position, transform.rotation, transform);
            }
        }

        private TileData CollapseTileToElement(WFCTile tile, float[] tileWeights)
        {
            float[] newArrayTest = new float[tileWeights.Length];

            SetScoreWeights(tile, newArrayTest);
            ConsoleLog.Log("Weights: " + newArrayTest.ToEnumeratedString(forceOneLine: true));
            int chosen = ChooseCandidate(newArrayTest);

            if(chosen < 0 || chosen >= tiles.Count) return null;

            tile.elementID = chosen;

            return tiles[chosen];
        }

        protected abstract void SetScoreWeights(WFCTile tile, float[] tileWeights);

        private static int ChooseCandidate(float[] tileWeights)
        {
            float bestScore = float.NegativeInfinity;
            List<int> bestTileCandidateIndices = new();

            for(int i = 0; i < tileWeights.Length; i++)
            {
                if(tileWeights[i] == bestScore)
                {
                    bestTileCandidateIndices.Add(i);
                }
                else if(tileWeights[i] > bestScore)
                {
                    bestScore = tileWeights[i];
                    bestTileCandidateIndices.Clear();
                    bestTileCandidateIndices.Add(i);
                }
            }

            int chosen = bestTileCandidateIndices.GetRandom();
            return chosen;
        }

        [Button]
        private void DeleteAllChildObjects()
        {
            for(int i = transform.childCount - 1; i >= 0; i--)
            {
                if(Application.isPlaying)
                    Destroy(transform.GetChild(i).gameObject);
                else
                    DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }
    }
}
