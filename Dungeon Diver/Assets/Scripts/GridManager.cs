using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }
    [SerializeField] GameObject tile;
    [SerializeField] TextMeshProUGUI turnText;
    public int tileSize;
    public Unit[,] units;
    private Unit currentTurn;
    public List<Unit> turnOrder;
    private int currentTurnIndex = 0;
    private const int TILECOUNTX = 4;
    private const int TILECOUNTY = 4;
    private GameObject[,] tiles;
    private Camera currentCamera;
    private Vector2Int currentHover = new Vector2Int(-1, -1);
    private Vector2Int selectedTile = new Vector2Int(-1, -1);
    private Color originalColor;

    [SerializeField] private GameObject[] prefabs;


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
        GenerateAllTiles(tileSize, TILECOUNTX, TILECOUNTY);
        SpawnAllUnits();
        PositionAllPieces();
        StartTurnSystem();
    }

    private void Update()
    {
        if (!currentCamera)
        {
            currentCamera = Camera.main;
            return;
        }

        RaycastHit info;
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out info, 1000, LayerMask.GetMask("Tile")))
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
        else
        {
            if (currentHover != new Vector2Int(-1, -1))
            {
                SetTileHoverState(currentHover, false);
                currentHover = new Vector2Int(-1, -1);
            }
        }
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
    private void SetTileSelectedState(Vector2Int position, bool isSelected)
    {
        if (position.x >= 0 && position.x < TILECOUNTX && position.y >= 0 && position.y < TILECOUNTY)
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
        if (position.x >= 0 && position.x < TILECOUNTX && position.y >= 0 && position.y < TILECOUNTY)
        {
            GameObject tile = tiles[position.x, position.y];
            Renderer tileRenderer = tile.GetComponent<Renderer>();
            tileRenderer.material.color = originalColor;
        }
    }
    private void SetTileHoverState(Vector2Int position, bool isHovering)
    {
        if (position.x >= 0 && position.x < TILECOUNTX && position.y >= 0 && position.y < TILECOUNTY)
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

    // Board generation
    private void GenerateAllTiles(float tileSize, int tileCountX, int tileCountY)
    {
        tiles = new GameObject[TILECOUNTX, TILECOUNTY];
        for (int x = 0; x < TILECOUNTX; x++)
            for (int y = 0; y < TILECOUNTY; y++)
                tiles[x, y] = GenerateSingleTile(tileSize, x, y);
    }

    private GameObject GenerateSingleTile(float tileSize, int x, int y)
    {
        GameObject tileObject = Instantiate(tile, new Vector3(x * tileSize, 0, y * tileSize), Quaternion.identity);
        tileObject.name = string.Format("Tile X:{0}, Y:{1}", x, y);
        tileObject.transform.parent = transform;
        tileObject.layer = LayerMask.NameToLayer("Tile");
        return tileObject;
    }

    private void SpawnAllUnits()
    {
        units = new Unit[TILECOUNTY, TILECOUNTX];
        int playerTeam = 0;
        int enemyTeam = 1;
        units[1, 0] = SpawnSingleUnit(UnitType.Player1, playerTeam);
        //units[1, 2] = SpawnSingleUnit(UnitType.Player1, playerTeam);
        //units[0, 0] = SpawnSingleUnit(UnitType.Player1, playerTeam);
        //units[0, 2] = SpawnSingleUnit(UnitType.Player1, playerTeam);
        units[2, 0] = SpawnSingleUnit(UnitType.Enemy1, enemyTeam);

    }
    private Unit SpawnSingleUnit(UnitType type, int team) {
        Unit unit = Instantiate(prefabs[(int)type - 1], transform).GetComponent<Unit>();

        unit.unitType = type;
        unit.team = team;

        if (team == 0)
            unit.InitializeStats(10, 5, 20);
        else
            unit.InitializeStats(8, 5, 15);

        return unit;
    }
    //positioning
    private void PositionAllPieces()
    {
        for (int x = 0; x < TILECOUNTX; x++) {
            for (int y = 0; y < TILECOUNTY; y++) {
                if (units[x, y] != null) {
                    PositionSinglePiece(x, y);
                }
            }
        }
    }
    private void PositionSinglePiece(int x, int y, bool force = false)
    {
        units[x, y].currentX = x;
        units[x, y].currentY = y;
        units[x, y].transform.position = new Vector3(x * tileSize, 1, y * tileSize);
    }

    private void StartTurnSystem()
    {
        turnOrder = new List<Unit>();

        foreach (Unit unit in units)
        {
            if (unit != null)
                turnOrder.Add(unit);
        }

        turnOrder.Sort((a, b) => b.dexterity.CompareTo(a.dexterity));

        StartTurn(turnOrder[0]);
    }
    private void StartTurn(Unit unit)
    {
        currentTurn = unit;
        Debug.Log("It's " + currentTurn.unitType + "'s turn!");
        Debug.Log(unit.currentX + " " + unit.currentY);

        turnText.text = "current:" + currentTurn.unitType;
        if (currentTurn.team == 1)
        {
            StartCoroutine(EnemyTurn());
        }
    }
    private IEnumerator EnemyTurn()
    {
        yield return new WaitForSeconds(0.5f);

        Unit closestPlayer = FindClosestPlayer();
        if (closestPlayer != null)
        {
            Vector2Int playerPosition = new Vector2Int(closestPlayer.currentX, closestPlayer.currentY);
            Vector2Int enemyPosition = new Vector2Int(currentTurn.currentX, currentTurn.currentY);

            if (Mathf.Abs(playerPosition.x - enemyPosition.x) + Mathf.Abs(playerPosition.y - enemyPosition.y) == 1)
            {
                currentTurn.Attack(closestPlayer);
            }
            else
            {
                Vector2Int moveDirection = GetMoveDirection(enemyPosition, playerPosition);
                Vector2Int newEnemyPosition = new Vector2Int(enemyPosition.x + moveDirection.x, enemyPosition.y + moveDirection.y);

                if (currentTurn.IsValidMove(newEnemyPosition))
                {
                    currentTurn.Move(newEnemyPosition);
                }
            }
        }

        yield return new WaitForSeconds(1.0f);

        EndTurn();
    }
    private Unit FindClosestPlayer()
    {
        Unit closestPlayer = null;
        float closestDistance = float.MaxValue;

        for (int x = 0; x < TILECOUNTX; x++)
        {
            for (int y = 0; y < TILECOUNTY; y++)
            {
                Unit unit = units[x, y];
                if (unit != null && unit.team == 0) 
                {
                    float distance = Mathf.Abs(x - currentTurn.currentX) + Mathf.Abs(y - currentTurn.currentY);
                    if (distance < closestDistance)
                    {
                        closestPlayer = unit;
                        closestDistance = distance;
                    }
                }
            }
        }

        return closestPlayer;
    }
    private Vector2Int GetMoveDirection(Vector2Int from, Vector2Int to)
    {
        int deltaX = to.x - from.x;
        int deltaY = to.y - from.y;

        if (Mathf.Abs(deltaX) > Mathf.Abs(deltaY))
        {
            return new Vector2Int(deltaX > 0 ? 1 : -1, 0);
        }
        else
        {
            return new Vector2Int(0, deltaY > 0 ? 1 : -1);
        }
    }
    private void EndTurn()
    {
        currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;
        StartTurn(turnOrder[currentTurnIndex]);
    }
    
    private Vector2Int LookUpTileIndex(GameObject hitInfo)
    {
        for (int x = 0; x < TILECOUNTX; x++)
            for (int y = 0; y < TILECOUNTY; y++)
                if (tiles[x, y] == hitInfo)
                    return new Vector2Int(x, y);

        return new Vector2Int(-1, -1);
    }
    public void OnMoveButtonPress()
    {
        if (currentTurn != null && selectedTile != new Vector2Int(-1, -1))
        {
            if (currentTurn.IsValidMove(selectedTile))
            {
                currentTurn.Move(selectedTile);
                EndTurn();
            }
            else
            {
                Debug.Log("Invalid move.");
            }
        }
    }

    public void OnAttackButtonPress()
    {
        if (currentTurn != null && selectedTile != new Vector2Int(-1, -1))
        {
            Unit targetUnit = units[selectedTile.x, selectedTile.y];
            if (targetUnit != null && targetUnit.team != currentTurn.team)
            {
                currentTurn.Attack(targetUnit);
                EndTurn();
            }
            else
            {
                Debug.Log("Invalid attack target.");
            }
        }
    }
    public void RemoveUnitFromTurnOrder(Unit unit)
    {
        if (turnOrder.Contains(unit))
        {
            turnOrder.Remove(unit);
            Debug.Log(unit.unitType + " removed from turn order.");
        }
    }
}
