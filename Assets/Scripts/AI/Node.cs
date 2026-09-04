using UnityEngine;

public class Node
{
    public Vector3 position;
    public int gridX, gridY;
    public float g, h;
    public Node parent;
    public float f { get { return g + h; } }
    public Node(Vector3 _pos, int _x, int _y)
    {
        position = _pos;
        gridX = _x;
        gridY = _y;
    }
}