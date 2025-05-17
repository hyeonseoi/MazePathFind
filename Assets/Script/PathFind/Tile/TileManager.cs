using UnityEngine;
using System.Collections.Generic;

// 전역에 FloorTileType을 정의합니다.
public enum FloorTileType
{
    MazeGeneration,
    PathfindingAStar,
    PathfindingOther,
    RightHandRule,
    LeftHandRule,
    BFS,
    DFS
}

// 타일의 출처를 구분하기 위한 Marker 컴포넌트
public class FloorTileMarker : MonoBehaviour
{
    public FloorTileType tileType;
}

// 타일 생성 및 업데이트를 담당하는 TileManager
public static class TileManager
{
    // 좌표(Vector2Int)를 키로 하여 타일 오브젝트를 관리합니다.
    private static Dictionary<Vector2Int, GameObject> tileDictionary = new Dictionary<Vector2Int, GameObject>();

    /// <summary>
    /// 지정된 위치에 타일이 이미 있으면 색상과 타입을 업데이트하고,
    /// 없으면 새 타일을 생성합니다.
    /// </summary>
    /// <param name="position">타일의 월드 좌표 (x, z 사용, y는 0.1로 고정)</param>
    /// <param name="type">타일의 출처를 나타내는 타입 (FloorTileType)</param>
    public static void CreateOrUpdateTile(Vector3 position, FloorTileType type)
    {
        // 타일을 배치할 좌표 키 (x, z를 정수로 반올림)
        Vector2Int posKey = new Vector2Int(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z));

        if (tileDictionary.TryGetValue(posKey, out GameObject existingTile))
        {
            // 이미 타일이 존재하면 색상과 타입 업데이트
            Renderer rend = existingTile.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = GetColorByTileType(type);
            }
            FloorTileMarker marker = existingTile.GetComponent<FloorTileMarker>();
            if (marker != null)
            {
                marker.tileType = type;
            }
            return;
        }

        // 타일이 없다면 새 타일 생성
        GameObject newTile = GameObject.CreatePrimitive(PrimitiveType.Cube);
        newTile.transform.position = new Vector3(position.x, 0.1f, position.z);
        newTile.transform.localScale = new Vector3(1, 0.2f, 1);

        // Collider 제거 (필요 없는 경우)
        Collider col = newTile.GetComponent<Collider>();
        if (col != null)
            Object.Destroy(col);

        Renderer newRend = newTile.GetComponent<Renderer>();
        if (newRend != null)
        {
            newRend.material.color = GetColorByTileType(type);
        }

        FloorTileMarker markerComponent = newTile.AddComponent<FloorTileMarker>();
        markerComponent.tileType = type;

        tileDictionary[posKey] = newTile;
    }

    /// <summary>
    /// 타일 타입에 따른 색상을 반환합니다.
    /// 각 알고리즘별로 원하는 색상으로 수정할 수 있습니다.
    /// </summary>
    private static Color GetColorByTileType(FloorTileType type)
    {
        switch (type)
        {
            case FloorTileType.PathfindingAStar:
                return Color.red;
            case FloorTileType.PathfindingOther:
                return Color.green;
            case FloorTileType.RightHandRule:
                return Color.blue;
            case FloorTileType.BFS:
                return Color.cyan;
            case FloorTileType.DFS:
                return new Color(0.5f, 0f, 0.5f); // 보라색
            default:
                return Color.white;
        }
    }

    /// <summary>
    /// 모든 타일을 삭제하고 Dictionary를 초기화합니다.
    /// 미로 재생성 전에 호출하여 기존 타일이 남지 않도록 합니다.
    /// </summary>
    public static void ClearAllTiles()
    {
        foreach (var kvp in tileDictionary)
        {
            if (kvp.Value != null)
                Object.Destroy(kvp.Value);
        }
        tileDictionary.Clear();
    }
}
