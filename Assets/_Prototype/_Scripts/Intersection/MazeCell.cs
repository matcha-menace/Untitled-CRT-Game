using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

public enum MazeCellType { Visited, Unvisited, Inaccessible, Entrance }

public class MazeCell : MonoBehaviour
{
    [SerializeField] private GameObject leftWall;
    [SerializeField] private GameObject rightWall;
    [SerializeField] private GameObject frontWall;
    [SerializeField] private GameObject backWall;
    [SerializeField] private GameObject cornerWalls;

    [SerializeField, ReadOnly] private int indexX, indexZ;

    //getters & setters
    public GameObject LeftWall => leftWall;
    public GameObject RightWall => rightWall;
    public GameObject FrontWall => frontWall;
    public GameObject BackWall => backWall;
    public MazeCellType CellType { get; private set; } = MazeCellType.Unvisited;
    public float CellWidth { get => Vector3.Distance(leftWall.transform.position, rightWall.transform.position);}
    public int IndexX { get => indexX; set => indexX = value; }
    public int IndexZ { get => indexZ; set => indexZ = value; }

    void Start() {
        name = "Cell (" + indexX + "," + indexZ + ")";
    }

    public void Visit()
    {
        CellType = MazeCellType.Visited;
    }

    public void BecomeEntrance()
    {
        CellType = MazeCellType.Entrance;
        leftWall.SetActive(false);
        rightWall.SetActive(false);
        frontWall.SetActive(false);
        backWall.SetActive(false);
    }

    public void ClearAllWalls()
    {
        leftWall.SetActive(false);
        rightWall.SetActive(false);
        frontWall.SetActive(false);
        backWall.SetActive(false);
        cornerWalls.SetActive(false);
    }

    public void ClearLeftWall()
    {
        leftWall.SetActive(false);
    }

    public void ClearRightWall()
    {
        rightWall.SetActive(false);
    }

    public void ClearFrontWall()
    {
        frontWall.SetActive(false);
    }

    public void ClearBackWall()
    {
        backWall.SetActive(false);
    }
}
