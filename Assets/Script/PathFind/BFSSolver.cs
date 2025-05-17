using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Diagnostics;
using System.Reflection;

public class BFSSolver : MonoBehaviour
{
    [Header("참조할 오브젝트")]
    public MazeGenerator mazeGenerator;    // MazeGenerator 할당
    public GameObject agentPrefab;         // 에이전트 프리팹
    public TMP_Text timeText;              // 경과 시간 표시용

    [Header("에이전트 이동 설정")]
    public float moveSpeed = 2f;

    /// <summary>
    /// UI 버튼(OnClick)에서 호출: BFS 알고리즘을 시작합니다.
    /// </summary>
    public void StartBFS()
    {
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

        List<MazeCell> path = BFSSearch(grid, startCell, goalCell, width, depth);
        if (path == null)
        {
            UnityEngine.Debug.LogError("BFS: 경로를 찾을 수 없습니다!");
            return;
        }
        GameObject agent = Instantiate(agentPrefab, startCell.transform.position, Quaternion.identity);
        StartCoroutine(MoveAgent(agent, path));
    }

    private List<MazeCell> BFSSearch(MazeCell[,] grid, MazeCell start, MazeCell goal, int width, int depth)
    {
        Queue<Node> queue = new Queue<Node>();
        bool[,] visited = new bool[width, depth];

        int startX = (int)start.transform.position.x;
        int startZ = (int)start.transform.position.z;
        int goalX = (int)goal.transform.position.x;
        int goalZ = (int)goal.transform.position.z;

        Node startNode = new Node { cell = start, x = startX, z = startZ, parent = null };
        queue.Enqueue(startNode);
        visited[startX, startZ] = true;

        int[] dx = { 1, -1, 0, 0 };
        int[] dz = { 0, 0, 1, -1 };

        while (queue.Count > 0)
        {
            Node current = queue.Dequeue();
            if (current.x == goalX && current.z == goalZ)
            {
                return RetracePath(current);
            }
            for (int i = 0; i < 4; i++)
            {
                int nx = current.x + dx[i];
                int nz = current.z + dz[i];
                if (nx >= 0 && nx < width && nz >= 0 && nz < depth && !visited[nx, nz])
                {
                    MazeCell neighborCell = grid[nx, nz];
                    // 결정: 현재 셀과 인접 셀 사이의 이동 가능 여부 확인 (벽 제거 상태)
                    string direction = "";
                    if (nx > current.x) direction = "right";
                    else if (nx < current.x) direction = "left";
                    else if (nz > current.z) direction = "up";
                    else if (nz < current.z) direction = "down";

                    if (IsAccessible(current.cell, neighborCell, direction))
                    {
                        Node nextNode = new Node { cell = neighborCell, x = nx, z = nz, parent = current };
                        queue.Enqueue(nextNode);
                        visited[nx, nz] = true;
                    }
                }
            }
        }
        return null;
    }

    private bool IsAccessible(MazeCell fromCell, MazeCell toCell, string direction)
    {
        FieldInfo leftWallField = typeof(MazeCell).GetField("_leftWall", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo rightWallField = typeof(MazeCell).GetField("_rightWall", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo frontWallField = typeof(MazeCell).GetField("_frontWall", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo backWallField = typeof(MazeCell).GetField("_backWall", BindingFlags.NonPublic | BindingFlags.Instance);

        switch (direction)
        {
            case "right":
                return !((GameObject)rightWallField.GetValue(fromCell)).activeSelf &&
                       !((GameObject)leftWallField.GetValue(toCell)).activeSelf;
            case "left":
                return !((GameObject)leftWallField.GetValue(fromCell)).activeSelf &&
                       !((GameObject)rightWallField.GetValue(toCell)).activeSelf;
            case "up":
                return !((GameObject)frontWallField.GetValue(fromCell)).activeSelf &&
                       !((GameObject)backWallField.GetValue(toCell)).activeSelf;
            case "down":
                return !((GameObject)backWallField.GetValue(fromCell)).activeSelf &&
                       !((GameObject)frontWallField.GetValue(toCell)).activeSelf;
            default:
                return false;
        }
    }

    private List<MazeCell> RetracePath(Node endNode)
    {
        List<MazeCell> path = new List<MazeCell>();
        Node current = endNode;
        while (current != null)
        {
            path.Add(current.cell);
            current = current.parent;
        }
        path.Reverse();
        return path;
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
            // BFS 잔상 타일 생성 (타일 타입: BFS)
            TileManager.CreateOrUpdateTile(cell.transform.position, FloorTileType.BFS);
            yield return null;
        }
        stopwatch.Stop();
        timeText.text = "BFS Time: " + stopwatch.Elapsed.TotalSeconds.ToString("F2") + " s";
        Destroy(agent);
    }

    private class Node
    {
        public MazeCell cell;
        public int x;
        public int z;
        public Node parent;
    }
}
