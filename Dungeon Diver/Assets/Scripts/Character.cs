using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    public Vector2Int position;

    public void MoveTo(Vector2Int newPosition)
    {
        position = newPosition;
        transform.position = new Vector3(newPosition.x, transform.position.y, newPosition.y);
    }

    public void Select()
    {
        GetComponent<Renderer>().material.color = Color.yellow;
    }

    public void Deselect()
    {
        GetComponent<Renderer>().material.color = Color.white;
    }
}
