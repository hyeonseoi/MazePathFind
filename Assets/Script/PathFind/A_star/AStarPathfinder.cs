using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;
using TMPro;

public class AStarPathfinder : MonoBehaviour
{
    [Header("참조할 오브젝트")]
    public MazeGenerator mazeGenerator;    // MazeGenerator 할당
    public GameObject agentPrefab;         // 에이전트 프리팹 (이동 애니메이션용)
    public TMP_Text timeText;              // 경과 시간 표시용 TMP_Text

    [Header("에이전트 이동 설정")]
    public float moveSpeed = 2f;

    // MazeCell의 private 벽(GameObject)에 접근하기 위한 Reflection 필드
    private FieldInfo leftWallField;
    private FieldInfo rightWallField;
    private FieldInfo frontWallField;
    private FieldInfo backWallField;

    void Awake()
    {
        leftWallField  = typeof(MazeCell).GetField("_leftWall", BindingFlags.NonPublic | BindingFlags.Instance);
        rightWallField = typeof(MazeCell).GetField("_rightWall", BindingFlags.NonPublic | BindingFlags.Instance);
        frontWallField = typeof(MazeCell).GetField("_frontWall", BindingFlags.NonPublic | BindingFlags.Instance);
        backWallField  = typeof(MazeCell).GetField("_backWall", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    // UI 버튼(OnClick)에서 호출
    public void StartAStar()
    {
        // 미로 셀들을 2차원 배열로 재구성
        MazeCell[] cells = FindObjectsOfType<MazeCell>();
        int width = mazeGenerator.GetWidth();
        int depth = mazeGenerator.GetDepth();
        MazeCell[,] grid = new MazeCell[width, depth];
        foreach (var cell in cells)
        {
            int x = Mathf.RoundToInt(cell.transform.position.x);
            int z = Mathf.RoundToInt(cell.transform.position.z);
            grid[x, z] = cell;
        }
        MazeCell startCell = grid[0, 0];
        MazeCell goalCell = grid[width - 1, depth - 1];

        // A* 알고리즘을 통해 경로 계산
        List<MazeCell> path = AStarSearch(grid, startCell, goalCell, width, depth);
        if (path == null)
        {
            UnityEngine.Debug.LogError("경로를 찾을 수 없습니다!");
            return;
        }
        // 에이전트 생성 및 경로 따라 이동
        GameObject agent = Instantiate(agentPrefab, startCell.transform.position, Quaternion.identity);
        StartCoroutine(MoveAgent(agent, path));
    }

    private List<MazeCell> AStarSearch(MazeCell[,] grid, MazeCell start, MazeCell goal, int width, int depth)
    {
        Dictionary<Vector2Int, Node> nodes = new Dictionary<Vector2Int, Node>();
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                Vector2Int pos = new Vector2Int(x, z);
                Node node = new Node
                {
                    cell = grid[x, z],
                    position = pos,
                    gCost = Mathf.Infinity,
                    hCost = Heuristic(pos, new Vector2Int(width - 1, depth - 1)),
                    parent = null
                };
                nodes[pos] = node;
            }
        }
        Node startNode = nodes[new Vector2Int((int)start.transform.position.x, (int)start.transform.position.z)];
        startNode.gCost = 0;

        List<Node> openList = new List<Node> { startNode };
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();

        while (openList.Count > 0)
        {
            Node current = openList.OrderBy(n => n.gCost + n.hCost).First();
            if (current.position == new Vector2Int(width - 1, depth - 1))
            {
                return RetracePath(startNode, current);
            }
            openList.Remove(current);
            closedSet.Add(current.position);

            foreach (Node neighbor in GetNeighbors(current, grid, width, depth, nodes))
            {
                if (closedSet.Contains(neighbor.position))
                    continue;
                float tentativeG = current.gCost + 1f;
                if (tentativeG < neighbor.gCost)
                {
                    neighbor.gCost = tentativeG;
                    neighbor.parent = current;
                    if (!openList.Contains(neighbor))
                        openList.Add(neighbor);
                }
            }
        }
        return null;
    }

    private List<MazeCell> RetracePath(Node startNode, Node endNode)
    {
        List<MazeCell> path = new List<MazeCell>();
        Node current = endNode;
        while (current != startNode)
        {
            path.Add(current.cell);
            current = current.parent;
        }
        path.Reverse();
        return path;
    }

    private float Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private List<Node> GetNeighbors(Node node, MazeCell[,] grid, int width, int depth, Dictionary<Vector2Int, Node> nodes)
    {
        List<Node> neighbors = new List<Node>();
        Vector2Int pos = node.position;

        // 오른쪽 (x+1)
        if (pos.x + 1 < width)
        {
            MazeCell neighborCell = grid[pos.x + 1, pos.y];
            if (IsAccessible(node.cell, neighborCell, "right"))
                neighbors.Add(nodes[new Vector2Int(pos.x + 1, pos.y)]);
        }
        // 왼쪽 (x-1)
        if (pos.x - 1 >= 0)
        {
            MazeCell neighborCell = grid[pos.x - 1, pos.y];
            if (IsAccessible(node.cell, neighborCell, "left"))
                neighbors.Add(nodes[new Vector2Int(pos.x - 1, pos.y)]);
        }
        // 앞쪽 (z+1)
        if (pos.y + 1 < depth)
        {
            MazeCell neighborCell = grid[pos.x, pos.y + 1];
            if (IsAccessible(node.cell, neighborCell, "front"))
                neighbors.Add(nodes[new Vector2Int(pos.x, pos.y + 1)]);
        }
        // 뒤쪽 (z-1)
        if (pos.y - 1 >= 0)
        {
            MazeCell neighborCell = grid[pos.x, pos.y - 1];
            if (IsAccessible(node.cell, neighborCell, "back"))
                neighbors.Add(nodes[new Vector2Int(pos.x, pos.y - 1)]);
        }
        return neighbors;
    }

    private bool IsAccessible(MazeCell fromCell, MazeCell toCell, string direction)
    {
        switch (direction)
        {
            case "right":
                return !((GameObject)rightWallField.GetValue(fromCell)).activeSelf &&
                       !((GameObject)leftWallField.GetValue(toCell)).activeSelf;
            case "left":
                return !((GameObject)leftWallField.GetValue(fromCell)).activeSelf &&
                       !((GameObject)rightWallField.GetValue(toCell)).activeSelf;
            case "front":
                return !((GameObject)frontWallField.GetValue(fromCell)).activeSelf &&
                       !((GameObject)backWallField.GetValue(toCell)).activeSelf;
            case "back":
                return !((GameObject)backWallField.GetValue(fromCell)).activeSelf &&
                       !((GameObject)frontWallField.GetValue(toCell)).activeSelf;
            default:
                return false;
        }
    }

    private class Node
    {
        public MazeCell cell;
        public Vector2Int position;
        public float gCost;
        public float hCost;
        public Node parent;
    }

    private IEnumerator MoveAgent(GameObject agent, List<MazeCell> path)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        foreach (MazeCell cell in path)
        {
            Vector3 targetPos = cell.transform.position;
            while (Vector3.Distance(agent.transform.position, targetPos) > 0.01f)
            {
                agent.transform.position = Vector3.MoveTowards(agent.transform.position, targetPos, moveSpeed * Time.deltaTime);
                yield return null;
            }
            // 경로를 따라 이동 시 잔상 타일 생성
            LeaveTrailMarker(cell.transform.position);
        }
        stopwatch.Stop();
        timeText.text = "Time: " + stopwatch.Elapsed.TotalSeconds.ToString("F2") + " s";

        Destroy(agent);
    }

    private void LeaveTrailMarker(Vector3 position)
    {
        // 타일의 높이가 0.2이므로, y위치는 0.1로 설정하여 지면과 맞춤
        Vector3 markerPosition = new Vector3(position.x, 0.1f, position.z);
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.transform.position = markerPosition;
        marker.transform.localScale = new Vector3(1, 0.2f, 1);
        Collider col = marker.GetComponent<Collider>();
        if (col != null) Destroy(col);
        Renderer rend = marker.GetComponent<Renderer>();
        if (rend != null)
            rend.material.color = Color.red;
        // 타일에 FloorTileMarker 컴포넌트 추가 후 타입 설정
        marker.AddComponent<FloorTileMarker>().tileType = FloorTileType.PathfindingAStar;
    }
}
