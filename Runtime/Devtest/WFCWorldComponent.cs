using IDEK.Tools.Coroutines.TaskRoutines;
using IDEK.Tools.Logging;
using IDEK.Tools.ShocktroopExtensions;
using IDEK.Tools.WorldManagement.CoreLib.Tiles;
using IDEK.Tools.WorldManagement.Unity;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IDEK.DevTest.WFC
{

    [Serializable]
    public class TileData
    {
        [HorizontalGroup(width: 0.4f)]
        public string title;
        [HorizontalGroup]
        public GameObject dioramaGameObj;
    }

    public class WFCWorldComponent : MonoBehaviour
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

            //TaskRoutine.Start(CursorCrawl);
            StartCoroutine(CursorCrawl());

            //foreach(WFCTile tile in world.tileMatrix)
            //{
            //    //check rules
            //    CheckRules(tile);
            //    //instantiate the object that received the highest score

            //    Instantiate(tiles.GetRandom().dioramaGameObj, tile.RawPosition, transform.rotation, transform);
            //}
            
        }


        private IEnumerator CursorCrawl()
        {
            //TileC cursor = new Vector3TileCursor();
            cursor.world = world;

            //TODO: remove pieces from this if we give starting sample data
            HashSet<WFCTile> unfilledTiles = world.tileMatrix.Cast<WFCTile>().ToHashSet();

            cursor.TrySetTileIndex(new(
                UnityEngine.Random.Range(0, worldDimensions.x),
                UnityEngine.Random.Range(0, worldDimensions.y),
                UnityEngine.Random.Range(0, worldDimensions.z)
            ));


            //IDEA:
            //do we want to do a hilbert curve instead of a random walk?
            //that's a pretty well-known way to trace a continuous,
            //non-crossing line through an n-dimensional space...

            float[] tileWeightsNonAllocArray = new float[tiles.Count];

            while(unfilledTiles.Count > 0)
            {
                ConsoleLog.Log("WFC Crawl | " + unfilledTiles.Count + " unfilled Tiles remaining!");

                yield return new WaitForSeconds(crawlWait);

                FillTileWithCollapsedElement(cursor.tile, tileWeightsNonAllocArray);
                cursor.tile.RefreshNeighbors();

                unfilledTiles.Remove(cursor.tile);

                if(unfilledTiles.Count <= 0) break;

                //yield return new WaitForSeconds(crawlWait);

                WFCTile candidate = (WFCTile)cursor.tile.Neighbors.Where(neighbor => unfilledTiles.Contains(neighbor)).FirstOrDefault();

                //yield return new WaitForSeconds(crawlWait);

                ConsoleLog.Log("Attempting to move to index" + candidate?.TileIndex);

                if(cursor.TryMoveTo(candidate)) continue;

                //yield return new WaitForSeconds(crawlWait);

                WFCTile arbitrary = unfilledTiles.GetArbitrary();
                ConsoleLog.Log("Failed to move to neighbor, Attempting to move to arbitrary index" + arbitrary.TileIndex);

                if(!cursor.TryMoveTo(arbitrary))
                {
                    ConsoleLog.LogError("Operation exception; failed to pull and move to an arbitary tile element. " +
                        "Logic error may exist. Aborting process to allow review of interim result.");
                    //operation error; abort and review results
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
                //it.transform.localScale = it.transform.localScale.Mult(transform.localScale);
            }
        }

        private TileData CollapseTileToElement(WFCTile tile, float[] tileWeights)
        {
            //get scores
            //for our first test, we will simply do a very basic hardcoded neighbor check:
            //water and grass cannot be adjacent

            float[] newArrayTest = new float[tileWeights.Length];

            //Set Up Weights
            SetScoreWeights(tile, newArrayTest);
            ConsoleLog.Log("Weights: " + newArrayTest.ToEnumeratedString(forceOneLine: true));
            int chosen = ChooseCandidate(newArrayTest);

            if(chosen < 0 || chosen >= tiles.Count) return null;

            tile.elementID = chosen;

            return tiles[chosen];
        }

        //TODO: come up with a way to define these using scrobs (and/or make the logic easier to parse)
        private static void SetScoreWeights(WFCTile tile, float[] tileWeights)
        {
            for(int i = 0; i < tileWeights.Length; i++) 
            {
                tileWeights[i] = 0;
            }

            //if air directly below, we must be air
            if(tile.Below != null && tile.Below.elementID == 0)
            {
                tileWeights[0] = float.PositiveInfinity;
                return; //immediately move on; no further questions!
            }
            else if(tile.Above != null && tile.Above.elementID > 0) //if solid material above, no air below.
            {
                tileWeights[0] = float.NegativeInfinity;
            }

            //if next to water, can't be grass
            if(tile.IsAdjacentToElementID(3))
            {
                tileWeights[1] = -1f;
            }

            //if next to grass, can't be water
            if(tile.IsAdjacentToElementID(1))
            {
                tileWeights[3] = -1f;
            }

            //solids can't be on top of water (if solid above, water weight is impossible)
            if(tile.Above != null && (tile.Above.elementID > 0 && tile.Above.elementID != 3))
            {
                tileWeights[3] = -1f;
            }
        }

        private static int ChooseCandidate(float[] tileWeights)
        {
            float bestScore = float.NegativeInfinity;
            List<int> bestTileCandidateIndices = new();

            for(int i = 0; i < tileWeights.Length; i++)
            {
                if(tileWeights[i] == bestScore)//if multiple have same best score, they are all candidates
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


        //private void CheckRules(WFCTile tile)
        //{
        //    //water and grass cannot touch


        //    //sand must be next to both water and grass
        //        //if a tile is adjacent to sand and that sand doesn't already have both a water and a grass tile, the missing one must be placed.
        //        //

        //    //tile.Neighbors
        //}

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
