using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.EventSystems;

public class ChessLogic : MonoBehaviour
{
    [Header("Art")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] private Material hoverMaterial;

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
    
    Camera m_MainCamera;

    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8;
    private GameObject[,] tiles;
    private Vector2Int currentHover;
    void Awake()
    {
        arRaycastManager = GetComponent<ARRaycastManager>();
        GenerateAllTiles(1, TILE_COUNT_X, TILE_COUNT_Y);
    }

    void Start(){
        m_MainCamera = Camera.main;
    }

    private void GenerateAllTiles(float tileSize, int tileCountX, int tileCountY){
        tiles = new GameObject[tileCountX, tileCountY];
        for (int x = 0; x < tileCountX; x++){
            for(int y = 0; y < tileCountY; y++){
                tiles[x, y] = GenerateSingleTile(tileSize, x, y);
                tiles[x, y].layer = LayerMask.NameToLayer("Tile");
            }
        }
    }    

    private void Update(){
        if(Input.touchCount > 0) {
            Touch touch = Input.GetTouch(0);
            touchPosition = touch.position;

            //Checking to see if the position of the touch
            // is over a UI object in case of UI overlay on screen.
        if (!EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) {
            if (touch.phase == TouchPhase.Began) {
                WhiteKing.transform.position += new Vector3(1, 0, 0);
                Ray ray = m_MainCamera.ScreenPointToRay(touchPosition);
                RaycastHit hitObject;

                    if (Physics.Raycast(ray, out hitObject, LayerMask.GetMask("Tile"))) {
                        Vector2Int hitPosition = LookupTileIndex(hitObject.transform.gameObject);
                        WhiteKing.transform.position += new Vector3(-1, 0, 0);

                        if(currentHover == -Vector2Int.one){
                            WhiteKing.transform.position -= new Vector3(1, 0, 0);
                            currentHover = hitPosition;
                            tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
                            tiles[hitPosition.x, hitPosition.y].GetComponent<MeshRenderer>().material = hoverMaterial;
                        }

                        if(currentHover != hitPosition){
                            WhiteKing.transform.position += new Vector3(0, 1, 0);
                            tiles[currentHover.x, currentHover.y].layer = LayerMask.NameToLayer("Tile");
                            tiles[currentHover.x, currentHover.y].GetComponent<MeshRenderer>().material = tileMaterial;
                            currentHover = hitPosition;
                            tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
                            tiles[hitPosition.x, hitPosition.y].GetComponent<MeshRenderer>().material = hoverMaterial;

                        }

                        //Do whatever you want to do with the hitObject,
                        // which in this case would be your, well, case.
                        // Identify it either through name or tag, for instance below.
                        //if(hitObject.transform.CompareTag("Tile")) {
                        //Do something with the case
                        //}
                    } else{
                        if(currentHover != -Vector2Int.one){
                            tiles[currentHover.x, currentHover.y].layer = LayerMask.NameToLayer("Tile");
                            tiles[currentHover.x, currentHover.y].GetComponent<MeshRenderer>().material = tileMaterial;
                            currentHover = -Vector2Int.one;
                        }
                    } 
                }
            }
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

    private Vector2Int LookupTileIndex(GameObject hitInfo){
        for (int x = 0; x < TILE_COUNT_X; x++){
            for (int y = 0; y < TILE_COUNT_Y; y++){
                if(tiles[x, y] == hitInfo)
                return new Vector2Int(x, y);
            }
        }
        return -Vector2Int.one;
    }
}
