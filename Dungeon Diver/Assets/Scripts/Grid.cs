using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid : MonoBehaviour
{
    public int length;
    public int width;
    bool[,] grid;

    public void Initiate(int length, int width) {
        grid = new bool[length, width];
        this.length = length;
        this.width = width;
    }
}
