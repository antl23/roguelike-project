using System.Collections.Generic;
using UnityEngine;

public enum RoomType
{
    Empty,
    Start,
    Room,
    End,
    Shop,
    Item,
    Blessing
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
    public GameObject blessingPrefab;

    private RoomType[,] dungeonGrid;
    private Vector2Int startPosition;
    private List<Vector2Int> roomPositions;

    void Start()
    {
        GenerateDungeon();
        DisplayDungeonInLog();
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
        RoomType[] specialRooms = { RoomType.Shop, RoomType.Item, RoomType.Blessing };
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
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                GameObject roomInstance = null;

                switch (dungeonGrid[x, y])
                {
                    case RoomType.Start:
                        roomInstance = Instantiate(startRoomPrefab, new Vector3(x, 0, y), Quaternion.identity);
                        break;
                    case RoomType.Room:
                        roomInstance = Instantiate(roomPrefab, new Vector3(x, 0, y), Quaternion.identity);
                        break;
                    case RoomType.End:
                        roomInstance = Instantiate(endRoomPrefab, new Vector3(x, 0, y), Quaternion.identity);
                        break;
                    case RoomType.Shop:
                        roomInstance = Instantiate(shopPrefab, new Vector3(x, 0, y), Quaternion.identity);
                        break;
                    case RoomType.Item:
                        roomInstance = Instantiate(itemPrefab, new Vector3(x, 0, y), Quaternion.identity);
                        break;
                    case RoomType.Blessing:
                        roomInstance = Instantiate(blessingPrefab, new Vector3(x, 0, y), Quaternion.identity);
                        break;
                }

                if (roomInstance != null)
                {
                    roomInstance.transform.parent = transform; // Organize under DungeonMaster
                }
            }
        }
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
                    case RoomType.Blessing:
                        dungeonLayout.Append("B ");
                        break;
                }
            }
            dungeonLayout.AppendLine();
        }

        Debug.Log(dungeonLayout.ToString());
    }

}
