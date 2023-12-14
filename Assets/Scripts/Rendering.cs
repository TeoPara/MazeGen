using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class Rendering
{
    // Properties for the tilemaps. Find the tilemaps, if it's for the first time that we're accessed
    static Tilemap RectTilemap
    {
        get
        {
            if (rectTilemapSet) return rectTilemap;
            rectTilemap = GameObject.Find("Grid").transform.Find("Tilemap").GetComponent<Tilemap>();
            rectTilemapSet = true;
            return rectTilemap;
        }
    } static Tilemap rectTilemap; static bool rectTilemapSet;
    static Tilemap HexTilemap
    {
        get
        {
            if (hexTilemapSet) return hexTilemap;
            hexTilemap = GameObject.Find("HexGrid").transform.Find("Tilemap").GetComponent<Tilemap>();
            hexTilemapSet = true;
            return hexTilemap;
        }
    } static Tilemap hexTilemap; static bool hexTilemapSet;

    /// <summary> Get the centered, world-space position of a Hex tile </summary>
    public static Vector3 GetHexWorldPos(Vector2Int position) 
        => HexTilemap.CellToWorld((Vector3Int)position);
    
    

    /// <summary> Places an orb on a coordinate that's used as an indicator for stuff </summary>
    /// <param name="pos"> The position of the orb. An offset of (0.5f, 0.5f) will be added </param>
    /// <param name="color"> The color of the sprite </param>
    /// <param name="id"> The id used to identify this orb. If you create another orb with the same id, the previous one will be destroyed </param>
    /// <param name="hex"> Whether you're placing the orb on a hex based tilemap or not </param>
    public static void PlaceOrb(Vector2Int pos, Color color, int id, bool hex)
    {
        // Destroy the Orb with already the same id if present
        if (PlacedOrbs.TryGetValue(id, out GameObject orb))
        {
            Object.Destroy(orb);
            PlacedOrbs.Remove(id);
        }
        
        PlacedOrbs.Add(id, Object.Instantiate(Resources.Load<GameObject>("Orb"), hex ? HexTilemap.CellToWorld((Vector3Int)pos) : new Vector3(pos.x + 0.5f, pos.y + 0.5f), Quaternion.identity));
        PlacedOrbs[id].GetComponent<SpriteRenderer>().color = color;
    }
    static readonly Dictionary<int, GameObject> PlacedOrbs = new();
    
    
    /// <summary> Sets the camera's position and zoom level according to the given map's dimensions </summary>
    public static void FocusCamera(this Main.Node[,] map)
    {
        CameraControl.Cam.transform.position = new Vector3(map.GetLength(0) / 2f, map.GetLength(1) / 2f, -10);
        CameraControl.Cam.orthographicSize = (map.GetLength(0) > map.GetLength(1) ? map.GetLength(0) : map.GetLength(1)) / 2f + 0.5f;
    }
    
    /// <summary> Sets all tiles to be in-line with the state of all Nodes </summary>
    /// <param name="map"> Collection of all nodes </param>
    public static void RenderTiles(this Main.Node[,] map)
    {
        RectTilemap.ClearAllTiles();
        HexTilemap.ClearAllTiles();

        bool hex = map[0,0] is Main.Node.Hex;
        
        for (int x = 0; x < map.GetLength(0); x++)
        {
            for (int y = 0; y < map.GetLength(1); y++)
            {
                if (!hex)
                {
                    Main.Node.Rect c = map[x, y] as Main.Node.Rect;
                    RectTilemap.SetTile(RectTilemap.WorldToCell(new Vector3(x, y)), Resources.Load<TileBase>("Rect/" + GetTileBaseName(c.Openings, false)));
                }
                else
                {
                    Main.Node.Hex c = map[x, y] as Main.Node.Hex;
                    HexTilemap.SetTile(new Vector3Int(x,y), Resources.Load<TileBase>("Hex/"+GetTileBaseName(c.Openings, true)));
                }
            }   
        }
    }
    
    /// <summary> Sets a single tile to be in-line with the state of a single Node </summary>
    /// <param name="map"> Collection of all nodes </param>
    /// <param name="pos"> The position of the target Node on the map </param>
    public static void RenderTile(this Main.Node[,] map, Vector2Int pos)
    {
        bool hex = map[0,0] is Main.Node.Hex;
        
        if (!hex)
        {
            Main.Node.Rect c = map[pos.x, pos.y] as Main.Node.Rect;
            RectTilemap.SetTile(RectTilemap.WorldToCell(new Vector3(pos.x, pos.y)), Resources.Load<TileBase>("Rect/" + GetTileBaseName(c.Openings, false)));
        }
        else
        {
            Main.Node.Hex c = map[pos.x, pos.y] as Main.Node.Hex;
            HexTilemap.SetTile(new Vector3Int(pos.x,pos.y), Resources.Load<TileBase>("Hex/"+GetTileBaseName(c.Openings, true)));
        }
    }
    
    
    /// <summary> Converts an openings bool[] to the right filename </summary>
    /// <param name="c"> The openings array of the Node </param>
    /// <param name="hex"> Whether the node is a hexagonal node or not </param>
    /// <returns> The name of the TileBase in the Resources folder that should be used </returns>
    static string GetTileBaseName(bool[] c, bool hex)
    {
        char[] dirs = hex ? new []{'-','-','-','-','-','-'} : new []{'-','-','-','-'};

        for (int i = 0; i < (hex ? 6 : 4); i++)
            if (!c[i])
                dirs[i] = 'X';

        return new string(dirs);
    }
}
