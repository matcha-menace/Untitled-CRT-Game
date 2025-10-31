using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

public class MazeManager : MonoBehaviour
{
    [SerializeField] private MazeCell mazeCellPrefab;
    [SerializeField, BoxGroup("properties")] private int mazeWidth;
    [SerializeField, BoxGroup("properties")] private int mazeDepth;
    [SerializeField, BoxGroup("properties")] private Vector2Int entrancePos;
	[SerializeField, BoxGroup("properties")] private int easyLevel;

    [SerializeField, ReadOnly] private List<Vector2Int> indexList = new();
    private MazeCell[,] mazeGrid;
    private Vector3 mazeStartWorldPos;

    void Awake()
    {
		//set world pos
		mazeStartWorldPos = new Vector3(transform.position.x - mazeWidth / 2 * mazeCellPrefab.CellWidth, transform.position.y, transform.position.z - mazeDepth / 2 * mazeCellPrefab.CellWidth);

        mazeGrid = new MazeCell[mazeWidth, mazeDepth];

        for (int x = 0; x < mazeWidth; x++)
        {
            for (int z = 0; z < mazeDepth; z++)
            {
                mazeGrid[x, z] = Instantiate(mazeCellPrefab, new Vector3(x, 0, z), Quaternion.identity);
                
                //set to position according to the cell width
                mazeGrid[x, z].transform.position = new Vector3(mazeStartWorldPos.x + (x * mazeGrid[x, z].CellWidth), 0, mazeStartWorldPos.z + (z * mazeGrid[x, z].CellWidth));
                mazeGrid[x, z].transform.parent = gameObject.transform;

                //assign index x and index z
                mazeGrid[x, z].IndexX = x;
                mazeGrid[x, z].IndexZ = z;
                indexList.Add(new Vector2Int(x, z));

                //check if is entrance
                if (x == entrancePos.x && z == entrancePos.y)
                {
                    mazeGrid[x, z].BecomeEntrance();
                }
            }
        }

        //leave space for entrance and house first
        ClearAllSurrondingWalls(entrancePos.x, entrancePos.y);

        //decrease level of difficulty by randomly removing walls
        RandomRemoveWalls(easyLevel);

        //generate maze
        GenerateMaze(null, mazeGrid[0, 0]);

        //create exit portal
        StartCoroutine(DeferredExitCheck());
	}


    IEnumerator DeferredExitCheck()
    {
        yield return null;
        StaticCreateExit();
    }

    private void GenerateMaze(MazeCell previousCell, MazeCell currentCell)
    {
        currentCell.Visit();
        ClearWalls(previousCell, currentCell);

        MazeCell nextCell;

        do
        {
            nextCell = GetNextUnvisitedCell(currentCell);

            if (nextCell != null)
            {
                GenerateMaze(currentCell, nextCell);
            }
        } while (nextCell != null);
	}

    private MazeCell GetNextUnvisitedCell(MazeCell currentCell)
    {
        var unvisitedCells = GetUnvisitedCells(currentCell);

        return unvisitedCells.OrderBy(_ => Random.Range(1, 10)).FirstOrDefault();
    }

    private IEnumerable<MazeCell> GetUnvisitedCells(MazeCell currentCell)
    {
        int x = currentCell.IndexX;
        int z = currentCell.IndexZ;

        if (x + 1 < mazeWidth)
        {
            var cellToRight = mazeGrid[x + 1, z];

            if (cellToRight.CellType == MazeCellType.Unvisited)
            {
                yield return cellToRight;
            }
        }

        if (x - 1 >= 0)
        {
            var cellToLeft = mazeGrid[x - 1, z];

            if (cellToLeft.CellType == MazeCellType.Unvisited)
            {
                yield return cellToLeft;
            }
        }

        if (z + 1 < mazeDepth)
        {
            var cellToFront = mazeGrid[x, z + 1];

            if (cellToFront.CellType == MazeCellType.Unvisited)
            {
                yield return cellToFront;
            }
        }

        if (z - 1 >= 0)
        {
            var cellToBack = mazeGrid[x, z - 1];

            if (cellToBack.CellType == MazeCellType.Unvisited)
            {
                yield return cellToBack;
            }
        }
    }

    private void ClearWalls(MazeCell previousCell, MazeCell currentCell)
    {
        if (previousCell == null)
        {
            return;
        }

        if (previousCell.transform.position.x < currentCell.transform.position.x)
        {
            previousCell.ClearRightWall();
            currentCell.ClearLeftWall();
            return;
        }

        if (previousCell.transform.position.x > currentCell.transform.position.x)
        {
            previousCell.ClearLeftWall();
            currentCell.ClearRightWall();
            return;
        }

        if (previousCell.transform.position.z < currentCell.transform.position.z)
        {
            previousCell.ClearFrontWall();
            currentCell.ClearBackWall();
            return;
        }

        if (previousCell.transform.position.z > currentCell.transform.position.z)
        {
            previousCell.ClearBackWall();
            currentCell.ClearFrontWall();
            return;
        }
    }

    /// <summary>
    /// clear all walls that surronding the MazeCell (disable walls for current MazeCell and MazeCell in four other directions)
    /// </summary>
    /// <param name="x">maze cell index x</param>
    /// <param name="z">maze cell index z</param>
    private void ClearAllSurrondingWalls(int x, int z)
    {
        if (x > mazeWidth || x < 0 || z > mazeDepth || z < 0)
        {
            Debug.LogWarning(x + ", " + z + " is out of bound when clearing surrounding walls");
            return;
        }

        mazeGrid[x, z].ClearAllWalls();

        mazeGrid[Mathf.Min(x + 1, mazeWidth - 1), z].ClearLeftWall();
        mazeGrid[Mathf.Max(x - 1, 0), z].ClearRightWall();
        mazeGrid[x, Mathf.Min(z + 1, mazeDepth - 1)].ClearBackWall();
        mazeGrid[x, Mathf.Max(z - 1, 0)].ClearFrontWall();
    }

    /// <summary>
    /// get 3x3 area of MazeCell if possible (will not return cells out of bound)
    /// </summary>
    /// <param name="x">center index x</param>
    /// <param name="z">center index z</param>
    /// <returns>a list of MazeCells that circle around center x and z</returns>
    private List<MazeCell> Get3x3Cells(int x, int z) {
        List<MazeCell> cells = new List<MazeCell>();

        var clampedX = Mathf.Clamp(x, 0, mazeWidth);
        var clampedZ = Mathf.Clamp(z, 0, mazeDepth);
        
        //self
        cells.Add(mazeGrid[clampedX, clampedZ]);

        //four right directions
        cells.Add(mazeGrid[Mathf.Min(clampedX + 1, mazeWidth - 1), clampedZ]);
        cells.Add(mazeGrid[Mathf.Max(clampedX - 1, 0), clampedZ]);
        cells.Add(mazeGrid[clampedX, Mathf.Min(clampedZ + 1, mazeDepth - 1)]);
        cells.Add(mazeGrid[clampedX, Mathf.Max(clampedZ - 1, 0)]);

        //four diagnal directions
        cells.Add(mazeGrid[Mathf.Min(clampedX + 1, mazeWidth - 1), Mathf.Min(clampedZ + 1, mazeDepth - 1)]);
        cells.Add(mazeGrid[Mathf.Min(clampedX + 1, mazeWidth - 1), Mathf.Max(clampedZ - 1, 0)]);
        cells.Add(mazeGrid[Mathf.Max(clampedX - 1, 0), Mathf.Min(clampedZ + 1, mazeDepth - 1)]);
        cells.Add(mazeGrid[Mathf.Max(clampedX - 1, 0), Mathf.Max(clampedZ - 1, 0)]);
        
        return cells;
    }

    private void Clear3x3Walls(int x, int z) {
        
        if (x + 2 > mazeWidth || x - 2 < 0 || z + 2 > mazeDepth || z - 2 < 0)
        {
            Debug.LogWarning(x + ", " + z + " is out of bound when clearing 3x3 walls");
            return;
        }
		
		foreach (MazeCell cell in Get3x3Cells(x, z)) {
            //Debug.Log(cell.IndexX+ "," + cell.IndexZ);
            cell.ClearAllWalls();
        }
    }

    private void RandomRemoveWalls(int numberOfCells) {
        // Vector2Int[,] tempMazeIndex = GetRandomIndexes();
        List<Vector2Int> ranIndexList = ShuffleV2List(indexList);
        //randomly clear cells
        for (int i = 0; i < ranIndexList.Count; i++) {
            int x = ranIndexList[i].x;
            int z = ranIndexList[i].y;
            //make sure the cell is not house or entrance
            if (mazeGrid[x, z].CellType == MazeCellType.Inaccessible || mazeGrid[x, z].CellType == MazeCellType.Entrance)
                continue;

            //make sure the cell is not at the edge
            if (x <= 0 || x >= mazeWidth-1)
                continue;
            if (z <= 0 || z >= mazeDepth-1)
                continue;

            if (numberOfCells > 0) {
                ClearAllSurrondingWalls(x, z);
                numberOfCells--;
            } else {
                return;
            }
        }
    }

    /// <summary>
    /// Fisher-Yates algorithm of shuffling 2d array
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="random"></param>
    /// <param name="array"></param>
    public static void Shuffle<T>(System.Random random, T[,] array)
    {
        int lengthRow = array.GetLength(1);

        for (int i = array.Length - 1; i > 0; i--)
        {
            int i0 = i / lengthRow;
            int i1 = i % lengthRow;

            int j = random.Next(i + 1);
            int j0 = j / lengthRow;
            int j1 = j % lengthRow;

            T temp = array[i0, i1];
            array[i0, i1] = array[j0, j1];
            array[j0, j1] = temp;
        }
    }
    
    private void StaticCreateExit()
    {
        mazeGrid[mazeWidth / 2, mazeDepth - 1].ClearAllWalls();
        // TODO: mark exit
        var targetPos = mazeGrid[mazeWidth / 2, mazeDepth - 1].transform.position;
    }

    private bool isCellAccessable(int x, int z) {
        if (x > mazeWidth-1 || x < 0 || z > mazeDepth-1 || z < 0)
            return false;
        if (mazeGrid[x,z].CellType == MazeCellType.Inaccessible || mazeGrid[x,z].CellType == MazeCellType.Entrance)
            return false;
        return true;
    }

    /// <summary>
    /// check if the wall can be reached in the maze (not blocked by the wall next to it)
    /// </summary>
    /// <param name="facingX">x index of the next cell that is facing the wall</param>
    /// <param name="facingZ">z index of the next cell that is facing the wall</param>
    /// <returns></returns>
    private bool isWallAccessable(int x, int z, int facingX, int facingZ) {
        if (!isCellAccessable(facingX, facingZ))
            return false;
        if (x < facingX) {//next wall is at the right
            return !mazeGrid[facingX, facingZ].LeftWall.activeSelf;
        }
        if (x > facingX) {//next wall is at the left
            return !mazeGrid[facingX, facingZ].RightWall.activeSelf;
        }
        if (z < facingZ) {//next wall is at the front
            return !mazeGrid[facingX, facingZ].BackWall.activeSelf;
        }
        if (z > facingZ) {//next wall is at the back
            return !mazeGrid[facingX, facingZ].FrontWall.activeSelf;
        }
        return false;
    }

    public List<Vector2Int> ShuffleV2List(List<Vector2Int> list) {
        for (int i = 0; i < list.Count; i++) {
            Vector2Int temp = list[i];

            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
        return list;
    }
}