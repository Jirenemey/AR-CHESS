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
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private float yOffset = 0.2f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;
    [SerializeField] private float dragOffset = 0.15f;

    [Header("Prefabs & Materials")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;

    // LOGIC
    [SerializeField] private ARRaycastManager arRaycastManager;
    Vector2 touchPosition;
    static List<ARRaycastHit> hits = new List<ARRaycastHit>();
    Camera m_MainCamera;
    private ChessPiece[,] chessPieces;
    private ChessPiece currentlyDragging;
    private List<Vector2Int> availableMoves = new List<Vector2Int>();
    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8;
    private GameObject[,] tiles;
    private Vector2Int currentHover;
    private Vector3 bounds;
    private bool selectPiece = false; 
    private bool canMove;

    void Awake()
    {
        arRaycastManager = GetComponent<ARRaycastManager>();
        GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);

        SpawnAllPieces();
        PositionAllPieces();
    }
    void Start(){
        m_MainCamera = Camera.main;
    } 

    private void Update(){
        if(Input.touchCount > 0) {
            Touch touch = Input.GetTouch(0);
            touchPosition = touch.position;

            //Checking to see if the position of the touch
            // is over a UI object in case of UI overlay on screen.
        if (!EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) {
            if (touch.phase == TouchPhase.Began) {
                Ray ray = m_MainCamera.ScreenPointToRay(touchPosition);
                RaycastHit hitObject;

                    if (Physics.Raycast(ray, out hitObject, LayerMask.GetMask("Tile"))) {
                        Vector2Int hitPosition = LookupTileIndex(hitObject.transform.gameObject);

                        if(chessPieces[hitPosition.x, hitPosition.y] != null && selectPiece == false){
                            // Is it our turn?
                            if(true){

                                currentlyDragging = chessPieces[hitPosition.x, hitPosition.y];
                                selectPiece = true;
                                availableMoves = currentlyDragging.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);

                                if(currentHover == -Vector2Int.one){
                                    currentHover = hitPosition;
                                    tiles[hitPosition.x, hitPosition.y].GetComponent<MeshRenderer>().material = hoverMaterial;
                                }

                                if(currentHover != hitPosition){
                                    tiles[currentHover.x, currentHover.y].GetComponent<MeshRenderer>().material = tileMaterial;
                                    currentHover = hitPosition;
                                    tiles[hitPosition.x, hitPosition.y].GetComponent<MeshRenderer>().material = hoverMaterial;
                                }

                                HighlightTiles();
                            }
                        } else if(selectPiece == true){
                            // Valid move?
                            
                            Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);

                            canMove = MoveTo(currentlyDragging, hitPosition.x, hitPosition.y);
                            // if second input is same team piece or cant move there set the piece down 
                            // else move piece over there and capture enemy piece if there
                            if(!canMove){
                                currentlyDragging.SetPosition(GetTileCentre(previousPosition.x, previousPosition.y));
                                currentlyDragging = null;
                            }
                            currentlyDragging = null;
                            RemoveHighlightTiles();
                            tiles[previousPosition.x, previousPosition.y].GetComponent<MeshRenderer>().material = tileMaterial;
                            selectPiece = false;
                        }

                        //Do whatever you want to do with the hitObject,
                        // which in this case would be your, well, case.
                        // Identify it either through name or tag, for instance below.
                        //if(hitObject.transform.CompareTag("Tile")) {
                        //Do something with the case
                        //}
                    } else{
                        if(currentHover != -Vector2Int.one){
                            tiles[currentHover.x, currentHover.y].GetComponent<MeshRenderer>().material = tileMaterial;
                            currentHover = -Vector2Int.one;
                            currentlyDragging = null;
                            RemoveHighlightTiles();
                            selectPiece = false;
                        }
                    } 
                }
            }
        }
        if(currentlyDragging){
            currentlyDragging.SetPosition(Vector3.up * dragOffset);
        }
    }


//Generate Board

    private void GenerateAllTiles(float tileSize, int tileCountX, int tileCountY){
        yOffset += transform.position.y;
        bounds = new Vector3((tileCountX / 2) * tileSize, 0, (tileCountX / 2) * tileSize) + boardCenter;

        tiles = new GameObject[tileCountX, tileCountY];
        for (int x = 0; x < tileCountX; x++){
            for(int y = 0; y < tileCountY; y++){
                tiles[x, y] = GenerateSingleTile(tileSize, x, y);
                tiles[x, y].layer = LayerMask.NameToLayer("Tile");
            }
        }
    }   
    private GameObject GenerateSingleTile(float tileSize, int x, int y){
        GameObject tileObject = new GameObject(string.Format("X:{0}, Y:{1}", x, y));
        tileObject.transform.parent = transform;

        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(x * tileSize, yOffset, y * tileSize) - bounds;
        vertices[1] = new Vector3(x * tileSize, yOffset, (y + 1) * tileSize) - bounds;
        vertices[2] = new Vector3((x + 1) * tileSize, yOffset, y * tileSize) - bounds;
        vertices[3] = new Vector3((x + 1) * tileSize, yOffset, (y + 1) * tileSize) - bounds;

        int[] tris = new int[] {0, 1, 2, 1, 3, 2 };

        mesh.vertices = vertices;
        mesh.triangles = tris;

        mesh.RecalculateNormals();

        tileObject.AddComponent<BoxCollider>();

        return tileObject;
    }

//Spawn pieces
private void SpawnAllPieces(){
    chessPieces = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];

    int whiteTeam = 0, blackTeam = 1;

    // White team
    chessPieces[0,0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);
    chessPieces[1,0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
    chessPieces[2,0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
    chessPieces[3,0] = SpawnSinglePiece(ChessPieceType.Queen, whiteTeam);
    chessPieces[4,0] = SpawnSinglePiece(ChessPieceType.King, whiteTeam);
    chessPieces[5,0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
    chessPieces[6,0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
    chessPieces[7,0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);
    for(int i = 0; i < TILE_COUNT_X; i++){
        chessPieces[i, 1] = SpawnSinglePiece(ChessPieceType.Pawn, whiteTeam);
    }

    // Black team
    chessPieces[0,7] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam);
    chessPieces[1,7] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
    chessPieces[2,7] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
    chessPieces[3,7] = SpawnSinglePiece(ChessPieceType.Queen, blackTeam);
    chessPieces[4,7] = SpawnSinglePiece(ChessPieceType.King, blackTeam);
    chessPieces[5,7] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
    chessPieces[6,7] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
    chessPieces[7,7] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam);
    for(int i = 0; i < TILE_COUNT_X; i++){
        chessPieces[i, 6] = SpawnSinglePiece(ChessPieceType.Pawn, blackTeam);
    }
}
private ChessPiece SpawnSinglePiece(ChessPieceType type, int team){
    ChessPiece cp = Instantiate(prefabs[(int)type - 1], transform).GetComponent<ChessPiece>();

    cp.type = type;
    cp.team = team;
    cp.GetComponent<MeshRenderer>().material = teamMaterials[team];

    return cp;
}
private void PositionAllPieces(){
    for (int x = 0; x < TILE_COUNT_X; x++){
        for (int y = 0; y < TILE_COUNT_Y; y++){
            if(chessPieces[x,y] != null){
                PositionSinglePiece(x, y, true);
            }
        }
    }
}
private void PositionSinglePiece(int x, int y, bool force = false){
    chessPieces[x, y].currentX = x;
    chessPieces[x, y].currentY = y;
    chessPieces[x, y].SetPosition(GetTileCentre(x, y), force);
}
private Vector3 GetTileCentre(int x, int y){
    return new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2);
}

//Highlight Tiles
private void HighlightTiles(){
    for(int i = 0; i < availableMoves.Count; i++){
        tiles[availableMoves[i].x, availableMoves[i].y].GetComponent<MeshRenderer>().material = highlightMaterial;
    }
}

private void RemoveHighlightTiles(){
    for(int i = 0; i < availableMoves.Count; i++){
        tiles[availableMoves[i].x, availableMoves[i].y].GetComponent<MeshRenderer>().material = tileMaterial;
    }

    availableMoves.Clear();
}

//Operations
private bool ContainsValidMove(ref List<Vector2Int> moves, Vector2 pos){
    for (int i = 0; i < moves.Count; i++)
        if(moves[i].x == pos.x && moves[i].y == pos.y)
            return true;
    
    return false;
}
private bool MoveTo(ChessPiece cp, int x, int y){
    if(!ContainsValidMove(ref availableMoves, new Vector2(x,y)))
        return false;


    Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);

    // Is there another piece on the target position?
    if(chessPieces[x, y] != null){
        ChessPiece ocp = chessPieces[x, y];

        if(cp.team == ocp.team)
            return false;
        
        // If its the enemy team
        Destroy(chessPieces[x, y].gameObject);
    }



    chessPieces[x, y] = cp;
    chessPieces[previousPosition.x, previousPosition.y] = null;

    PositionSinglePiece(x, y);

    return true;
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
