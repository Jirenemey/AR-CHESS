using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ChessLogic : MonoBehaviour
{
    [Header("Art")]
    [SerializeField] private Material tileMaterial;

    [Header("Chess Pieces")]
    public GameObject[] WhitePawn = new GameObject[8];
    public GameObject WhiteKing;
    public GameObject WhiteQueen;
    public GameObject[] WhiteKnight = new GameObject[2];
    public GameObject[] WhiteBishop = new GameObject[2];
    public GameObject[] WhiteRook = new GameObject[2];
    public GameObject[] BlackPawn = new GameObject[8];
    public GameObject BlackKing;
    public GameObject BlackQueen;
    public GameObject[] BlackKnight = new GameObject[2];
    public GameObject[] BlackBishop = new GameObject[2];
    public GameObject[] BlackRook = new GameObject[2];

    // Link raycast tracker
    ARRaycastManager arRaycastManager;

    // Where did the Tap on screen occur
    Vector2 touchPosition;

    // Keep track of all objects being hit by Raycast
    static List<ARRaycastHit> hits = new List<ARRaycastHit>();
    

    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8;
    private GameObject[,] tiles;
    void Awake()
    {
        arRaycastManager = GetComponent<ARRaycastManager>();
        GenerateAllTiles(1, TILE_COUNT_X, TILE_COUNT_Y);
    }

    private void GenerateAllTiles(float tileSize, int tileCountX, int tileCountY){
        tiles = new GameObject[tileCountX, tileCountY];
        for (int x = 0; x < tileCountX; x++){
            for(int y = 0; y < tileCountY; y++){
                tiles[x, y] = GenerateSingleTile(tileSize, x, y);
            }
        }
    }    
    
    bool GetTouchPosition(out Vector2 touchPosition)
    {
        // Was there a Touch on Screen?
        if(Input.touchCount > 0)
        {
            // Store the Touch position
            touchPosition = Input.GetTouch(0).position;
            
            // Return Touch happened
            return true;
        }

        // No Touch
        touchPosition = Vector2.zero;


        // Return Touch did not happened
        return false;
    }

    private void Update(){
        if(!GetTouchPosition(out Vector2 touchposition))
            return;

        if (arRaycastManager.Raycast(touchPosition, hits,
            TrackableType.PlaneWithinPolygon)){
            }
    }


//Generate Board
    private GameObject GenerateSingleTile(float tileSize, int x, int y){
        GameObject tileObject = new GameObject(string.Format("X:{0}, Y:{1}", x, y));
        tileObject.transform.parent = transform;

        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(x * tileSize, 0, y * tileSize);
        vertices[1] = new Vector3(x * tileSize, 0, (y + 1) * tileSize);
        vertices[2] = new Vector3((x + 1) * tileSize, 0, y * tileSize);
        vertices[3] = new Vector3((x + 1) * tileSize, 0, (y + 1) * tileSize);

        int[] tris = new int[] {0, 1, 2, 1, 3, 2 };

        mesh.vertices = vertices;
        mesh.triangles = tris;

        mesh.RecalculateNormals();

        tileObject.AddComponent<BoxCollider>();

        return tileObject;
    }
}
