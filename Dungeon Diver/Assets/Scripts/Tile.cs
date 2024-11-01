using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public Vector2Int position; 
    public Character characterOnTile;

    private Renderer tileRenderer;

    private void Start()
    {
        tileRenderer = GetComponent<Renderer>();
    }

    public bool IsOccupied()
    {
        return characterOnTile != null;
    }

    public void Highlight(bool highlight)
    {
        tileRenderer.material.color = highlight ? Color.green : Color.white;
    }

    public void PlaceCharacter(Character character)
    {
        characterOnTile = character;
        character.MoveTo(position);
    }

    public void RemoveCharacter()
    {
        characterOnTile = null;
    }
}
