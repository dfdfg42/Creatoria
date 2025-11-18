using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// JSON 없이 간단하게 테스트용 맵을 생성 (타일 에셋 없이도 작동)
/// </summary>
public class SimpleMapBuilder : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap groundTilemap;
    public Tilemap objectTilemap;

    [Header("Settings")]
    public int mapWidth = 50;
    public int mapHeight = 50;
    public bool buildOnStart = true;

    [Header("Debug")]
    public Color groundColor = Color.green;
    public Color wallColor = Color.gray;

    void Start()
    {
        if (buildOnStart)
        {
            BuildSimpleMap();
        }
    }

    [ContextMenu("Build Simple Test Map")]
    public void BuildSimpleMap()
    {
        if (groundTilemap == null)
        {
            Debug.LogError("Ground Tilemap is not assigned!");
            return;
        }

        Debug.Log("Building simple test map...");

        // Tilemap 초기화
        groundTilemap.ClearAllTiles();
        if (objectTilemap != null)
        {
            objectTilemap.ClearAllTiles();
        }

        // 간단한 타일 생성 (Tile 에셋 없이)
        Tile groundTile = ScriptableObject.CreateInstance<Tile>();
        groundTile.color = groundColor;

        Tile wallTile = ScriptableObject.CreateInstance<Tile>();
        wallTile.color = wallColor;

        // 바닥 깔기
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                groundTilemap.SetTile(pos, groundTile);
            }
        }

        // 벽 만들기 (테두리)
        if (objectTilemap != null)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                objectTilemap.SetTile(new Vector3Int(x, 0, 0), wallTile);
                objectTilemap.SetTile(new Vector3Int(x, mapHeight - 1, 0), wallTile);
            }

            for (int y = 0; y < mapHeight; y++)
            {
                objectTilemap.SetTile(new Vector3Int(0, y, 0), wallTile);
                objectTilemap.SetTile(new Vector3Int(mapWidth - 1, y, 0), wallTile);
            }

            // 중간에 방 몇 개
            for (int x = 10; x < 15; x++)
            {
                for (int y = 10; y < 15; y++)
                {
                    objectTilemap.SetTile(new Vector3Int(x, y, 0), wallTile);
                }
            }
        }

        Debug.Log($"Built test map: {mapWidth}x{mapHeight}");
        
        // 카메라 위치 조정
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.position = new Vector3(mapWidth / 2f, mapHeight / 2f, -10f);
            mainCam.orthographicSize = Mathf.Max(mapWidth, mapHeight) / 2f;
        }
    }

    [ContextMenu("Clear Map")]
    public void ClearMap()
    {
        if (groundTilemap != null) groundTilemap.ClearAllTiles();
        if (objectTilemap != null) objectTilemap.ClearAllTiles();
        Debug.Log("Map cleared");
    }
}
