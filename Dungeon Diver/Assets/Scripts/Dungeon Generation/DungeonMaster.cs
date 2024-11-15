using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;

public enum RoomType
{
    Empty,
    Start,
    Room,
    End,
    Shop,
    Item
}

public class DungeonMaster : MonoBehaviour
{
    public int gridSize = 9;
    public int minimumRooms = 20;
    public GameObject roomPrefab;
    public GameObject startRoomPrefab;
    public GameObject endRoomPrefab;
    public GameObject shopPrefab;
    public GameObject itemPrefab;
    [SerializeField] private GameObject mapCord;
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private Camera dungeonCamera;
    [SerializeField] private Camera combatCamera;
    [SerializeField] private Canvas dungeonUI;
    [SerializeField] private Canvas combatUI;
    [SerializeField] private Canvas teamUI;
    [SerializeField] private TextMeshProUGUI Character1;
    [SerializeField] private TextMeshProUGUI Character2;
    [SerializeField] private TextMeshProUGUI Character3;
    [SerializeField] private TextMeshProUGUI Character4;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GameObject playerLocationPrefab;
    private GameObject playerLocation;
    private Camera currentCamera;
    private Vector2Int currentHover = new Vector2Int(-1, -1);
    private Vector2Int selectedTile = new Vector2Int(-1, -1);
    private Vector2Int playerTile = new Vector2Int(-1, -1);
    private Color originalColor;
    private GameObject[,] tiles;
    private RoomType[,] dungeonGrid;
    private Vector2Int startPosition;
    private List<Vector2Int> roomPositions;

    public static DungeonMaster Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        dungeonCamera.gameObject.SetActive(true);
        combatCamera.gameObject.SetActive(false);
        dungeonUI.gameObject.SetActive(true);
        combatUI.gameObject.SetActive(false);
        currentCamera = dungeonCamera;
        GenerateDungeon();
        DisplayDungeonInLog();
        playerTile = startPosition;
        if (playerLocation == null && playerLocationPrefab != null)
        {
            playerLocation = Instantiate(playerLocationPrefab, transform);
            UpdatePlayerLocation();
        }

    }
    private void Update()
    {


        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit info, 1000, LayerMask.GetMask("Tile")))
        {
            Vector2Int hitPosition = LookUpTileIndex(info.transform.gameObject);

            if (currentHover == new Vector2Int(-1, -1))
            {
                currentHover = hitPosition;
                SetTileHoverState(currentHover, true);
            }
            else if (currentHover != hitPosition)
            {
                SetTileHoverState(currentHover, false);
                currentHover = hitPosition;
                SetTileHoverState(currentHover, true);
            }

            if (Input.GetMouseButtonDown(0))
            {
                SelectTile(currentHover);
            }
        }
        else if (currentHover != new Vector2Int(-1, -1))
        {
            SetTileHoverState(currentHover, false);
            currentHover = new Vector2Int(-1, -1);
        }
    }
    public void OnRoomSelected()
    {
        dungeonCamera.gameObject.SetActive(false);
        combatCamera.gameObject.SetActive(true);
        dungeonUI.gameObject.SetActive(false);
        combatUI.gameObject.SetActive(true);

        gridManager.StartCombat();
    }

    public void ReturnToDungeon()
    {
        dungeonCamera.gameObject.SetActive(true);
        combatCamera.gameObject.SetActive(false);
        dungeonUI.gameObject.SetActive(true);
        combatUI.gameObject.SetActive(false);

    }
    private void SelectTile(Vector2Int newSelectedTile)
    {
        if (selectedTile != new Vector2Int(-1, -1))
        {
            ResetTileColor(selectedTile);
        }

        selectedTile = newSelectedTile;
        Debug.Log("Tile selected at: " + selectedTile);
        SetTileSelectedState(selectedTile, true);
    }
    private void UpdatePlayerLocation()
    {

        Vector3 mapPosition = mapCord.transform.position;
        Vector3 playerPosition = mapPosition + new Vector3(playerTile.x * tileSize, 3, playerTile.y * tileSize);
        playerLocation.transform.position = playerPosition;
    }
    private void SetTileSelectedState(Vector2Int position, bool isSelected)
    {
        if (IsInBounds(position))
        {
            GameObject tile = tiles[position.x, position.y];
            Renderer tileRenderer = tile.GetComponent<Renderer>();

            if (isSelected)
            {
                originalColor = tileRenderer.material.color;
                tileRenderer.material.color = Color.red;
            }
            else
            {
                tileRenderer.material.color = originalColor;
            }
        }
    }

    private void ResetTileColor(Vector2Int position)
    {
        if (IsInBounds(position))
        {
            GameObject tile = tiles[position.x, position.y];
            Renderer tileRenderer = tile.GetComponent<Renderer>();
            tileRenderer.material.color = originalColor;
        }
    }

    private void SetTileHoverState(Vector2Int position, bool isHovering)
    {
        if (IsInBounds(position))
        {
            GameObject tile = tiles[position.x, position.y];
            Renderer tileRenderer = tile.GetComponent<Renderer>();

            if (isHovering)
            {
                originalColor = tileRenderer.material.color;
                tileRenderer.material.color = Color.yellow;
            }
            else
            {
                tileRenderer.material.color = originalColor;
            }
        }
    }
    private Vector2Int LookUpTileIndex(GameObject hitInfo)
    {

        for (int x = 0; x < gridSize; x++)
            for (int y = 0; y < gridSize; y++)
                if (tiles[x, y] == hitInfo)
                    return new Vector2Int(x, y);

        return new Vector2Int(-1, -1);
    }
    private void GenerateDungeon()
    {
        dungeonGrid = new RoomType[gridSize, gridSize];
        startPosition = new Vector2Int(gridSize / 2, gridSize / 2);
        roomPositions = new List<Vector2Int>();
        dungeonGrid[startPosition.x, startPosition.y] = RoomType.Start;
        roomPositions.Add(startPosition);

        ExpandDungeon(startPosition);

        AssignDeadEnds();
        InstantiateDungeon();
    }

    private void ExpandDungeon(Vector2Int position)
    {
        Queue<Vector2Int> roomQueue = new Queue<Vector2Int>();
        roomQueue.Enqueue(position);

        while (roomPositions.Count < minimumRooms && roomQueue.Count > 0)
        {
            Vector2Int current = roomQueue.Dequeue();


            int exits = UnityEngine.Random.Range(1, 5);
            List<Vector2Int> randomDirections = GetRandomizedDirections();

            foreach (var dir in randomDirections)
            {
                if (roomPositions.Count >= minimumRooms || exits <= 0) break;

                Vector2Int newRoomPos = current + dir;

                // Check if the position is valid and not cramped
                if (IsPositionValid(newRoomPos) && !IsCramped(newRoomPos, current))
                {
                    dungeonGrid[newRoomPos.x, newRoomPos.y] = RoomType.Room;
                    roomPositions.Add(newRoomPos);
                    roomQueue.Enqueue(newRoomPos);
                    exits--;
                }
            }
        }
    }

    private bool IsDeadEnd(Vector2Int position)
    {
        int adjacentRoomCount = 0;
        List<Vector2Int> directions = new List<Vector2Int> { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (var dir in directions)
        {
            Vector2Int adjacentPos = position + dir;

            if (IsInBounds(adjacentPos) && dungeonGrid[adjacentPos.x, adjacentPos.y] != RoomType.Empty)
            {
                adjacentRoomCount++;
            }
        }

        return adjacentRoomCount == 1;
    }

    private List<Vector2Int> GetRandomizedDirections()
    {
        List<Vector2Int> directions = new List<Vector2Int> { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        System.Random random = new System.Random();
        directions.Sort((a, b) => random.Next(-1, 2));
        return directions;
    }

    private bool IsPositionValid(Vector2Int position)
    {
        return position.x >= 0 && position.x < gridSize &&
               position.y >= 0 && position.y < gridSize &&
               dungeonGrid[position.x, position.y] == RoomType.Empty;
    }

    private bool IsCramped(Vector2Int newPos, Vector2Int parentPos)
    {
        List<Vector2Int> adjacentPositions = new List<Vector2Int>
        {
            newPos + Vector2Int.up,
            newPos + Vector2Int.down,
            newPos + Vector2Int.left,
            newPos + Vector2Int.right
        };

        foreach (var pos in adjacentPositions)
        {
            if (pos != parentPos && IsInBounds(pos) && dungeonGrid[pos.x, pos.y] != RoomType.Empty)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsInBounds(Vector2Int position)
    {
        return position.x >= 0 && position.x < gridSize &&
               position.y >= 0 && position.y < gridSize;
    }

    private void AssignDeadEnds()
    {
        bool endRoomAssigned = false;
        RoomType[] specialRooms = { RoomType.Shop, RoomType.Item };
        int specialRoomIndex = 0;

        foreach (var pos in roomPositions)
        {

            if (IsDeadEnd(pos)) 
            {
                if (!endRoomAssigned)
                {
                    dungeonGrid[pos.x, pos.y] = RoomType.End;
                    endRoomAssigned = true;
                }
                else
                {
                    dungeonGrid[pos.x, pos.y] = specialRooms[specialRoomIndex];
                    specialRoomIndex = (specialRoomIndex + 1) % specialRooms.Length;
                }
            }
        }
    }
    private void InstantiateDungeon()
    {
        tiles = new GameObject[gridSize, gridSize];

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                GameObject roomInstance = null;
                switch (dungeonGrid[x, y])
                {
                    case RoomType.Start:
                        roomInstance = Instantiate(startRoomPrefab);
                        break;
                    case RoomType.Room:
                        roomInstance = Instantiate(roomPrefab);
                        break;
                    case RoomType.End:
                        roomInstance = Instantiate(endRoomPrefab);
                        break;
                    case RoomType.Shop:
                        roomInstance = Instantiate(shopPrefab);
                        break;
                    case RoomType.Item:
                        roomInstance = Instantiate(itemPrefab);
                        break;
                }
                if (roomInstance != null)
                {
                    roomInstance.transform.parent = transform; 
                    tiles[x, y] = roomInstance; 
                }
            }
        }

        PositionAllRooms();
    }
    private void PositionAllRooms()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                if (tiles[x, y] != null)
                {
                    PositionSingleRoom(x, y);
                }
            }
        }
    }

    private void PositionSingleRoom(int x, int y)
    {
        Vector3 mapPosition = mapCord.transform.position;
        Vector3 roomPosition = mapPosition + new Vector3(x * tileSize, 0, y * tileSize);


        if (dungeonGrid[x, y] == RoomType.Start)
        {
            roomPosition.z -= 2;
        }
        if (dungeonGrid[x, y] == RoomType.Item)
        {
            roomPosition.y += 0.5f;
            roomPosition.z -= 0.8f;
        }

        tiles[x, y].transform.position = roomPosition;
    }
    private void DisplayDungeonInLog()
    {
        System.Text.StringBuilder dungeonLayout = new System.Text.StringBuilder();

        for (int y = gridSize - 1; y >= 0; y--)
        {
            for (int x = 0; x < gridSize; x++)
            {
                switch (dungeonGrid[x, y])
                {
                    case RoomType.Empty:
                        dungeonLayout.Append("X ");
                        break;
                    case RoomType.Start:
                        dungeonLayout.Append("S "); 
                        break;
                    case RoomType.Room:
                        dungeonLayout.Append("R ");
                        break;
                    case RoomType.End:
                        dungeonLayout.Append("E ");
                        break;
                    case RoomType.Shop:
                        dungeonLayout.Append("$ ");
                        break;
                    case RoomType.Item:
                        dungeonLayout.Append("I "); 
                        break;
                }
            }
            dungeonLayout.AppendLine();
        }

        Debug.Log(dungeonLayout.ToString());
    }
    public void MovePlayer()
    {
        if (selectedTile == new Vector2Int(-1, -1))
        {
            Debug.Log("No room selected.");
            return;
        }

        if (IsAdjacent(playerTile, selectedTile))
        {
            Debug.Log("Moving player to: " + selectedTile);
            playerTile = selectedTile; 
            UpdatePlayerLocation();
            ResetTileColor(selectedTile);
            selectedTile = new Vector2Int(-1, -1);
            OnRoomSelected();
        }
        else
        {
            Debug.Log("Cannot move to non adjacent room.");
        }
    }
    public void ViewTeam() {
        dungeonUI.gameObject.SetActive(false);
        teamUI.gameObject.SetActive(true);

            Character1.text = "Knight\nStr: " + gridManager.units[1,0].strength +
                              "\nDex: " + gridManager.units[1, 0].dexterity +
                              "\nVig: " + gridManager.units[1, 0].vigor;

            Character2.text = "Fighter\nStr: " + gridManager.units[1, 2].strength +
                              "\nDex: " + gridManager.units[1, 2].dexterity +
                              "\nVig: " + gridManager.units[1, 2].vigor;
        

            Character3.text = "Archer\nStr: " + gridManager.units[0, 0].strength +
                              "\nDex: " + gridManager.units[0, 0].dexterity +
                              "\nVig: " + gridManager.units[0, 0].vigor;
        

            Character4.text = "Mage\nStr: " + gridManager.units[0, 2].strength +
                              "\nDex: " + gridManager.units[0, 2].dexterity +
                              "\nVig: " + gridManager.units[0, 2].vigor;
        
    }
    public void HideTeamUI()
    {
        dungeonUI.gameObject.SetActive(true);
        teamUI.gameObject.SetActive(false);
    }

    private bool IsAdjacent(Vector2Int currentTile, Vector2Int targetTile)
    {
        int dx = Mathf.Abs(currentTile.x - targetTile.x);
        int dy = Mathf.Abs(currentTile.y - targetTile.y);
        return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
    }
}
