using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Chunk : MonoBehaviour
{
    public float Weight = 1f;
    public Color ChunkColor = Color.white;

    [Header("Allowed Connections (Neighbors allowing this)")]
    public List<Chunk> NorthValid;
    public List<Chunk> EastValid;
    public List<Chunk> SouthValid;
    public List<Chunk> WestValid;

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(ChunkColor.r, ChunkColor.g, ChunkColor.b, 0.2f);
        Vector3 offset = new Vector3(50, 50, 50); // Adjust based on your chunk visual origin
        
        GUIStyle style = new GUIStyle();
        style.normal.textColor = new Color(ChunkColor.r, ChunkColor.g, ChunkColor.b, 1f);
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 7;
        
        Handles.Label(transform.position + offset, gameObject.name, style);
        Gizmos.DrawCube(transform.position + offset, new Vector3(100, 100, 100));
    }

}