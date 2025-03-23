using UnityEngine;

public enum FloorTileType {
    MazeGeneration,
    PathfindingAStar,
    PathfindingOther
}

public class FloorTileMarker : MonoBehaviour
{
    public FloorTileType tileType;

    /// <summary>
    /// 씬 내 모든 타일 마커(FloorTileMarker)가 붙은 오브젝트를 제거합니다.
    /// </summary>
    public static void RemoveAllFloorTiles()
    {
        FloorTileMarker[] markers = GameObject.FindObjectsOfType<FloorTileMarker>();
        Debug.Log("제거할 타일 수: " + markers.Length);
        foreach (var marker in markers)
        {
            Destroy(marker.gameObject);
        }
    }

    /// <summary>
    /// 지정한 타입의 타일만 제거합니다.
    /// </summary>
    public static void RemoveFloorTilesByType(FloorTileType type)
    {
        FloorTileMarker[] markers = GameObject.FindObjectsOfType<FloorTileMarker>();
        foreach (var marker in markers)
        {
            if (marker.tileType == type)
                Destroy(marker.gameObject);
        }
    }
}
