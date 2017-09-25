using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class DebugOverlayResources : ScriptableObject
{
    [Tooltip("Number of columns of glyphs on texture")]
    public int charCols = 30;
    [Tooltip("Number of rows of glyphs on texture")]
    public int charRows = 16;
    [Tooltip("Width in pixels of each glyph")]
    public int cellWidth = 32;
    [Tooltip("Height in pixels of each glyph")]
    public int cellHeight = 32;

    public Material lineMaterial;
    public Material glyphMaterial;
}
