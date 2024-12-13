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
    public int hp;
    public int range;
    private Animator animator;
    private Vector3 originalDirection;

    void Start()
    {
        animator = GetComponent<Animator>();

        originalDirection = transform.forward;
    }

    public void InitializeStats(int str, int dex, int vig, int ran, int hp)
    {
        this.strength = str;
        this.dexterity = dex;
        this.vigor = vig;
        this.range = ran;
        this.hp = vigor;
    }
    public bool IsValidMove(Vector2Int newPosition)
    {
        if (newPosition.x < 0 || newPosition.x >= 4 || newPosition.y < 0 || newPosition.y >= 4)
        {
            return false;
        }

        int deltaX = Mathf.Abs(newPosition.x - currentX);
        int deltaY = Mathf.Abs(newPosition.y - currentY);

        if (unitType == UnitType.Player1 || unitType == UnitType.Player2)
        {
            if (deltaY != 1 || deltaX != 0)
            {
                return false;
            }
        }
        else if (unitType == UnitType.Player3 || unitType == UnitType.Player4)
        {
            if (deltaX + deltaY > 2)
            {
                return false;
            }
        }
        else
        {

            if (deltaX + deltaY != 1)
            {
                return false;
            }
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

        int attackRange = 1;

        if (unitType == UnitType.Player3 || unitType == UnitType.Player4)
        {
            attackRange = 2;
        }

        if (deltaX + deltaY > attackRange)
        {
            Debug.Log("Target is out of attack range.");
            return;
        }

        animator.SetTrigger("AttackTrigger");

        target.hp -= this.strength;
        Debug.Log(this.unitType + " attacked " + target.unitType + " for " + this.strength + " damage");

        if (target.hp <= 0)
        {
            Debug.Log(target.unitType + " has been defeated");
            if (target.unitType == UnitType.Player1 || target.unitType == UnitType.Player2) { GridManager.Instance.frontline--; }
            GridManager.Instance.RemoveUnitFromTurnOrder(target);
            Destroy(target.gameObject);
        }
        animator.SetTrigger("IdleTrigger");
    }

    public void Move(Vector2Int newPosition)
    {
        int tileSize = GridManager.Instance.tileSize;

        if (IsValidMove(newPosition) && !IsTileOccupied(newPosition))
        {
            Vector3 movementDirection = new Vector3(newPosition.x - currentX, 0, newPosition.y - currentY).normalized;

            StartCoroutine(RotateToFaceDirection(movementDirection, () =>
            {
                animator.SetTrigger("MoveTrigger");
                GridManager.Instance.units[currentX, currentY] = null;
                currentX = newPosition.x;
                currentY = newPosition.y;
                GridManager.Instance.units[currentX, currentY] = this;

                StartCoroutine(MoveToPosition(new Vector3(newPosition.x * tileSize, 1, newPosition.y * tileSize), () =>
                {
                    animator.SetTrigger("IdleTrigger");
                    StartCoroutine(RotateToFaceDirection(originalDirection, () =>
                    {
                        
                    }));
                }));
            }));
        }
    }
    private IEnumerator RotateToFaceDirection(Vector3 direction, System.Action onComplete = null)
    {
        if (direction == Vector3.zero)
        {
            onComplete?.Invoke();
            yield break;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        float rotationSpeed = 10f;

        while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            yield return null;
        }

        transform.rotation = targetRotation;

        onComplete?.Invoke();
    }

    private IEnumerator MoveToPosition(Vector3 targetPosition, System.Action onComplete = null)
    {
        float speed = 5f;

        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPosition;

        onComplete?.Invoke();
    }
    public bool IsTileOccupied(Vector2Int tilePosition)
    {
        return GridManager.Instance.units[tilePosition.x, tilePosition.y] != null;
    }
}
