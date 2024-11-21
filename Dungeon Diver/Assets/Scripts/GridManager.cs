using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.Text;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using UnityEngine.UI;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }
    [SerializeField] GameObject tile;
    [SerializeField] TextMeshProUGUI turnText;
    [SerializeField] private Camera combatCamera;
    [SerializeField] private Canvas GameOver;
    [SerializeField] private Canvas ItemGain;
    [SerializeField] private TextMeshProUGUI itemText;
    [SerializeField] private UnityEngine.UI.Image hpBar;
    [SerializeField] private TextMeshProUGUI hpText;
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
    public int frontline = 2;
    private Item randomItem;
    private bool isBoss = false;
    private int difficulty = 1;

    [SerializeField] private GameObject[] prefabs;

    private List<Item> items = new List<Item>
    {
        new Item("Dagger", 1, 2, 0),
        new Item("Boots", 0, 2 , 1),
        new Item("Hammer", 3, 0, 0),
        new Item("Leather Armor",0,0,3),
        new Item("Iron Chestplate", 0,-2,5),
        new Item("Magic Ring", 5, 0 ,-3),
        new Item("Steel Gauntlets", 2, 0 ,2)
    };



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
    }

    private void Update()
    {
        if (!currentCamera)
        {
            currentCamera = combatCamera;
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
        units[1, 0] = SpawnSingleUnit(UnitType.Player1, playerTeam);
        units[1, 2] = SpawnSingleUnit(UnitType.Player2, playerTeam);
        units[0, 0] = SpawnSingleUnit(UnitType.Player3, playerTeam);
        units[0, 2] = SpawnSingleUnit(UnitType.Player4, playerTeam);
    }
    private void SpawnRandomEnemy() {
        int enemyTeam = 1;
        bool haveEnemy = false;
        Vector2Int[] spawnPositions = {
        new Vector2Int(2, 0), new Vector2Int(2, 1), new Vector2Int(2, 2), new Vector2Int(2, 3),
        new Vector2Int(3, 0), new Vector2Int(3, 1), new Vector2Int(3, 2), new Vector2Int(3, 3)
    };
        foreach (Vector2Int pos in spawnPositions)
        {
            if (units[pos.x, pos.y] == null)
            {
                if (SpawnChance(difficulty))
                {
                    UnitType enemyType = (UnitType)GetWeightedRandomValue(difficulty);
                    units[pos.x, pos.y] = SpawnSingleUnit(enemyType, enemyTeam);
                    haveEnemy = true;
                }

            }
        }
        
    }
    bool SpawnChance(int weight, int maxWeight = 12)
    {
        float probabilityYes = Mathf.Clamp01(weight / (float)maxWeight);

        float rand = Random.Range(0f, 1f);

        return rand <= probabilityYes;
    }
    int GetWeightedRandomValue(int weight, int maxWeight = 12)
    {
        float probability5 = Mathf.Clamp01(1f - (weight / (float)maxWeight));
        float probability7 = Mathf.Clamp01(weight / (float)maxWeight);
        float probability6 = 1f - (probability5 + probability7); 

        float total = probability5 + probability6 + probability7;
        probability5 /= total;
        probability6 /= total;

        float rand = Random.Range(0f, 1f);
        if (rand <= probability5)
            return 5;
        else if (rand <= probability5 + probability6)
            return 6;
        else
            return 7;
    }
    private Unit SpawnSingleUnit(UnitType type, int team) {
        Unit unit = Instantiate(prefabs[(int)type - 1], transform).GetComponent<Unit>();

        unit.unitType = type;
        unit.team = team;

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
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void StartTurnSystem()
    {
        heal(5);
        if (!isBoss) { 
        SpawnRandomEnemy();
        }
        PositionAllPieces();
        difficulty++;
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
        if (CheckDefeatCondition())
        {
            GameOver.gameObject.SetActive(true);
            return;
        }
        if (AllEnemiesDefeated() && isBoss) {
            DungeonMaster.Instance.WinGame();
            return;
        }
        if (AllEnemiesDefeated())
        {
            DisplayItemReward();
            return;
        }
        currentTurn = unit;
        UpdateHPBar(currentTurn);
        Debug.Log("It's " + currentTurn.unitType + "'s turn!");
        Debug.Log(unit.currentX + " " + unit.currentY);
        if (frontline <= 0)
        {
            Vector2Int newPositionPlayer3 = new Vector2Int(currentTurn.currentX + 1, currentTurn.currentY);
            Vector2Int newPositionPlayer4 = new Vector2Int(currentTurn.currentX + 1, currentTurn.currentY);

            if (currentTurn.unitType == UnitType.Player3 && currentTurn.IsValidMove(newPositionPlayer3) && !currentTurn.IsTileOccupied(newPositionPlayer3))
            {
                currentTurn.Move(newPositionPlayer3);
            }
            else if (currentTurn.unitType == UnitType.Player4 && currentTurn.IsValidMove(newPositionPlayer4) && !currentTurn.IsTileOccupied(newPositionPlayer4))
            {
                currentTurn.Move(newPositionPlayer4);
            }
        }
        string unitName;
        switch (currentTurn.unitType)
        {
            case UnitType.Player1:
                unitName = "Knight";
                break;
            case UnitType.Player2:
                unitName = "Fighter";
                break;
            case UnitType.Player3:
                unitName = "Archer";
                break;
            case UnitType.Player4:
                unitName = "Mage";
                break;
            default:
                unitName = currentTurn.unitType.ToString(); 
                break;
        }

        turnText.text = "Current: " + unitName;

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

                    float PriorityWeight = 10.0f;
                    float distance = PriorityWeight * Mathf.Abs(x - currentTurn.currentX) + Mathf.Abs(y - currentTurn.currentY);

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
    private bool CheckDefeatCondition()
    {
        foreach (Unit unit in turnOrder)
        {
            if (unit != null && unit.team == 0)
                return false;
        }
        return true;
    }
    public bool AllEnemiesDefeated()
    {
        foreach (Unit unit in turnOrder)
        {
            if (unit != null && unit.team == 1)
            {
                return false; 
            }
        }
        return true;
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
            if (currentTurn.IsValidMove(selectedTile) && !currentTurn.IsTileOccupied(selectedTile))
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
    public void SkipTurn()
    {
        if (currentTurn.team == 0)
        {
            Debug.Log("Turn skipped.");
            EndTurn();
        } else
        {
            Debug.Log("Enemy Turn.");
            return;
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
    private void ResetCombat()
    {
        currentTurn = null;
        currentTurnIndex = 0;

    }
    public void RemoveUnitFromTurnOrder(Unit unit)
    {
        if (turnOrder.Contains(unit))
        {
            turnOrder.Remove(unit);
            Debug.Log(unit.unitType + " removed from turn order.");
        }
    }
    public void StartCombat()
    {
        SpawnRandomEnemy();
        PositionAllPieces();
        StartTurnSystem();
    }
    private void heal(int heal) {
        foreach (Unit unit in turnOrder) {
            unit.hp = unit.hp + heal;
            if (unit.hp > unit.vigor) { 
                unit.hp = unit.vigor;
            }
        }
    }
    public void DisplayItemReward()
    {
        randomItem = items[Random.Range(0, items.Count)];

        itemText.text = "Item:" + randomItem.name + "\nStats:\n" +
            "Str:" +randomItem.strength+
            "           Dex:" + randomItem.dexterity +
            "\nVig:" + randomItem.vigor;
        
        ItemGain.gameObject.SetActive(true);
    }
    public void ChoseKnight() 
    {
        AssignStatsToUnit(UnitType.Player1);
        ItemGain.gameObject.SetActive(false);
        ResetCombat();
        DungeonMaster.Instance.ReturnToDungeon();

    }
    public void ChoseArcher()
    {
        AssignStatsToUnit(UnitType.Player3);
        ItemGain.gameObject.SetActive(false);
        ResetCombat();
        DungeonMaster.Instance.ReturnToDungeon();
    }
    public void ChoseFighter()
    {
        AssignStatsToUnit(UnitType.Player2);
        ItemGain.gameObject.SetActive(false);
        ResetCombat();
        DungeonMaster.Instance.ReturnToDungeon();
    }
    public void ChoseMage()
    {
        AssignStatsToUnit(UnitType.Player4);
        ItemGain.gameObject.SetActive(false);
        ResetCombat();
        DungeonMaster.Instance.ReturnToDungeon();
    }
    void AssignStatsToUnit(UnitType unitType) {
        Unit unit = GetUnitByType(unitType);

        unit.strength += randomItem.strength;
        unit.dexterity += randomItem.dexterity;
        unit.vigor += randomItem.vigor;

    }
    Unit GetUnitByType(UnitType unitType)
    {
        foreach (Unit unit in units)
        {
            if (unit != null && unit.unitType == unitType)
            {
                return unit;
            }
        }
        return null;
    }
    private void UpdateHPBar(Unit unit)
    {
        if (hpBar != null)
        {
            hpBar.fillAmount = (float)unit.hp / unit.vigor;
        }

        if (hpText != null)
        {
            hpText.text = $"{unit.hp} / {unit.vigor}";
        }
    }
    public void BossFightStart() {
        isBoss = true;
        int enemyTeam = 1;
        units[2, 0] = SpawnSingleUnit(UnitType.Enemy3, enemyTeam);
        units[2, 2] = SpawnSingleUnit(UnitType.Enemy3, enemyTeam);
        units[3, 1] = SpawnSingleUnit(UnitType.Enemy4, enemyTeam);
        heal(5);
        PositionAllPieces();
        StartTurnSystem();
    }
}

public class Item
{
    public int strength;
    public int dexterity;
    public int vigor;
    public string name;

    public Item(string name, int strength, int dexterity, int vigor)
    {
        this.name = name;
        this.dexterity = dexterity;
        this.vigor = vigor;
        this.strength = strength;
    }
}
