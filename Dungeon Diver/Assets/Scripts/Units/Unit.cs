using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UnitType
{ 
    Player1 = 1,
    Player2 = 2,
    Player3 = 3,
    Player4 = 4,
    Enemy1 = 5,
    Enemy2 = 6,
    Enemy3 = 7,
    Enemy4 = 8

}
public class Unit : MonoBehaviour
{
    public int team;
    public int currentX;
    public int currentY;
    public UnitType unitType;

    public int strength;
    public int dexterity;
    public int vigor;

    private Vector3 desiredPosition;

    public void InitializeStats(int str, int dex, int vig)
    {
        this.strength = str;
        this.dexterity = dex;
        this.vigor = vig;
    }
    public bool IsValidMove(Vector2Int newPosition)
    {
        if (newPosition.x < 0 || newPosition.x >= 4 || newPosition.y < 0 || newPosition.y >= 4)
        {
            return false;
        }

        int deltaX = Mathf.Abs(newPosition.x - currentX);
        int deltaY = Mathf.Abs(newPosition.y - currentY);
        if (deltaX + deltaY != 1)
        {
            return false;
        }

        if (team == 0 && newPosition.x > 1) return false;
        if (team == 1 && newPosition.x < 2) return false;

        return true;
    }
    public void Attack(Unit target)
    {
        if (target == null || target.team == this.team)
        {
            Debug.Log("No target selected.");
            return;
        }
        int deltaX = Mathf.Abs(target.currentX - currentX);
        int deltaY = Mathf.Abs(target.currentY - currentY);
        if (deltaX + deltaY != 1)
        {
            Debug.Log("Target is out of attack range.");
            return;
        }


        target.vigor -= this.strength;
        Debug.Log(this.unitType + " attacked " + target.unitType + " for " + this.strength + " damage");

        if (target.vigor <= 0)
        {
            Debug.Log(target.unitType + " has been defeated");
            GridManager.Instance.RemoveUnitFromTurnOrder(target);
            Destroy(target.gameObject);
        }
    }

    public void Move(Vector2Int newPosition)
    {
        int tileSize = GridManager.Instance.tileSize;
        if (IsValidMove(newPosition))
        {
            GridManager.Instance.units[currentX, currentY] = null;
            currentX = newPosition.x;
            currentY = newPosition.y;
            GridManager.Instance.units[currentX, currentY] = this;
            transform.position = new Vector3(newPosition.x * tileSize, 1, newPosition.y * tileSize); 
            Debug.Log(unitType + " moved to " + newPosition);
        }
    }
}
