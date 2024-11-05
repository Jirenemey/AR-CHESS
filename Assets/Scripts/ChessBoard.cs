using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.EventSystems;

public enum SpecialMove{
    None = 0,
    EnPassant,
    Castling,
    Promotion
}

public class ChessLogic : MonoBehaviour
{
    [Header("Art")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] private Material hoverMaterial;
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private float yOffset = 0.2f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;
    [SerializeField] private float dragOffset = 0.015f;
    [SerializeField] private GameObject victoryScreen;

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
    private bool isWhiteTurn;
    private SpecialMove specialMove;
    private List<Vector2Int[]> moveList = new List<Vector2Int[]>();

    void Awake()
    {
        arRaycastManager = GetComponent<ARRaycastManager>();
        GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);

        SpawnAllPieces();
        PositionAllPieces();
        isWhiteTurn = true;
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
                            if(chessPieces[hitPosition.x, hitPosition.y].team == 0 && isWhiteTurn || chessPieces[hitPosition.x, hitPosition.y].team == 1 && !isWhiteTurn){

                                currentlyDragging = chessPieces[hitPosition.x, hitPosition.y];
                                selectPiece = true;
                                availableMoves = currentlyDragging.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                                specialMove = currentlyDragging.GetSpecialMoves(ref chessPieces, ref moveList, ref availableMoves);

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
                            }
                            currentlyDragging = null;
                            RemoveHighlightTiles();
                            tiles[previousPosition.x, previousPosition.y].GetComponent<MeshRenderer>().material = tileMaterial;
                            selectPiece = false;
                            currentHover = -Vector2Int.one;
                        }
                            if(currentlyDragging)
                                currentlyDragging.SetPosition(new Vector3(0, -5, 0) * dragOffset);
                    } else{
                        if(currentHover != -Vector2Int.one){
                            tiles[currentHover.x, currentHover.y].GetComponent<MeshRenderer>().material = tileMaterial;
                            currentHover = -Vector2Int.one;
                            currentlyDragging.SetPosition(GetTileCentre(currentlyDragging.currentX, currentlyDragging.currentY));
                            currentlyDragging = null;
                            RemoveHighlightTiles();
                            selectPiece = false;
                        }
                    } 
                }
            }
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

// Checkmate
private void Checkmate(int team){
    DisplayVictory(team);
}
private void DisplayVictory(int winningTeam){
    victoryScreen.SetActive(true);
    victoryScreen.transform.GetChild(winningTeam).gameObject.SetActive(true);
}
public void OnResetButton(){
    //UI
    victoryScreen.transform.GetChild(0).gameObject.SetActive(false);
    victoryScreen.transform.GetChild(1).gameObject.SetActive(false);
    victoryScreen.SetActive(false);

    // Fields reset
    currentlyDragging = null;
    availableMoves.Clear();
    moveList.Clear();

    //Clean up
    for(int x = 0; x < TILE_COUNT_X; x++){
        for(int y = 0; y < TILE_COUNT_Y; y++){
            if(chessPieces[x, y] != null)
                Destroy(chessPieces[x, y].gameObject);

                chessPieces[x, y] = null;
        }
    }

    SpawnAllPieces();
    PositionAllPieces();
    isWhiteTurn = true;
}
public void ExitButton(){
    Application.Quit();
}

// Special Moves
private void ProcessSpecialMove(){
    if(specialMove == SpecialMove.EnPassant){
        var newMove = moveList[moveList.Count - 1];
        ChessPiece myPawn = chessPieces[newMove[1].x, newMove[1].y];
        var targetPawnPosition = moveList[moveList.Count - 2];
        ChessPiece enemyPawn = chessPieces[targetPawnPosition[1].x, targetPawnPosition[1].y];

        if(myPawn.currentX == enemyPawn.currentX){
            if(myPawn.currentY == enemyPawn.currentY - 1 || myPawn.currentY == enemyPawn.currentY + 1){
                Destroy(chessPieces[targetPawnPosition[1].x, targetPawnPosition[1].y].gameObject);
                chessPieces[enemyPawn.currentX, enemyPawn.currentY] = null;
            }
        }
    }

    if(specialMove == SpecialMove.Castling){
        Vector2Int[] lastMove = moveList[moveList.Count - 1];

        // Left rook 
        if(lastMove[1].x == 2){
            if(lastMove[1].y == 0 ) // white side
            {
                ChessPiece rook = chessPieces[0, 0];
                chessPieces[3, 0] = rook;
                PositionSinglePiece(3, 0);
                chessPieces[0, 0] = null;
            } else if(lastMove[1].y == 7){ // black side
                ChessPiece rook = chessPieces[0, 7];
                chessPieces[3, 7] = rook;
                PositionSinglePiece(3, 7);
                chessPieces[0, 7] = null;
            }
        }

        // Right rook 
        if(lastMove[1].x == 6){
            if(lastMove[1].y == 0 ) // white side
            {
                ChessPiece rook = chessPieces[7, 0];
                chessPieces[5, 0] = rook;
                PositionSinglePiece(5, 0);
                chessPieces[7, 0] = null;
            } else if(lastMove[1].y == 7){ // black side
                ChessPiece rook = chessPieces[7, 7];
                chessPieces[5, 7] = rook;
                PositionSinglePiece(5, 7);
                chessPieces[7, 7] = null;
            }
        }
    }

    if(specialMove == SpecialMove.Promotion){
        Vector2Int[] lastMove = moveList[moveList.Count - 1];
        ChessPiece targetPawn = chessPieces[lastMove[1].x, lastMove[1].y];

        if(targetPawn.type == ChessPieceType.Pawn){
            if(targetPawn.team == 0 && lastMove[1].y == 7){ // White team
                ChessPiece newQueen = SpawnSinglePiece(ChessPieceType.Queen, 0);
                Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                chessPieces[lastMove[1].x, lastMove[1].y] = newQueen;
                PositionSinglePiece(lastMove[1].x, lastMove[1].y);
            } else if(targetPawn.team == 1 && lastMove[1].y == 0){ // Black team
                ChessPiece newQueen = SpawnSinglePiece(ChessPieceType.Queen, 1);
                Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                chessPieces[lastMove[1].x, lastMove[1].y] = newQueen;
                PositionSinglePiece(lastMove[1].x, lastMove[1].y);
            }
        }
    }
}

//Operations
private bool ContainsValidMove(ref List<Vector2Int> moves, Vector2Int pos){
    for (int i = 0; i < moves.Count; i++)
        if(moves[i].x == pos.x && moves[i].y == pos.y)
            return true;
    
    return false;
}
private bool MoveTo(ChessPiece cp, int x, int y){
    if(!ContainsValidMove(ref availableMoves, new Vector2Int(x,y)))
        return false;


    Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);

    // Is there another piece on the target position?
    if(chessPieces[x, y] != null){
        ChessPiece ocp = chessPieces[x, y]; // other chess piece

        if(cp.team == ocp.team){
            return false;
        }
        
        // If its the enemy team
        Destroy(chessPieces[x, y].gameObject);

        if(ocp.type == ChessPieceType.King && ocp.team == 0)
            Checkmate(1);
        if(ocp.type == ChessPieceType.King && ocp.team == 1)
            Checkmate(0);
    }

    chessPieces[x, y] = cp;
    chessPieces[previousPosition.x, previousPosition.y] = null;

    PositionSinglePiece(x, y);

    isWhiteTurn = !isWhiteTurn;
    
    moveList.Add(new Vector2Int[] {previousPosition, new Vector2Int(x, y)});
    ProcessSpecialMove();

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
