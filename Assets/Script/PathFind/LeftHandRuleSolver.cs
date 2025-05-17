using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using TMPro;
using System.Diagnostics;

public class LeftHandRuleSolver : MonoBehaviour
{
    [Header("참조할 오브젝트")]
    public MazeGenerator mazeGenerator;    // MazeGenerator 스크립트 (씬에 할당)
    public GameObject agentPrefab;         // 좌수법 에이전트 프리팹 (이동 애니메이션용)
    public TMP_Text timeText;              // 경과 시간 표시용 TMP_Text

    [Header("에이전트 이동 설정")]
    public float moveSpeed = 2f;           // 에이전트 이동 속도

    // MazeCell 내부의 벽(GameObject)에 접근하기 위한 Reflection 필드
    private FieldInfo leftWallField;
    private FieldInfo rightWallField;
    private FieldInfo frontWallField;
    private FieldInfo backWallField;

    // 미로 탐색에 사용할 방향 열거형 (Up: 전진(z+), Right: x+, Down: z-, Left: x-)
    public enum Direction { Up, Right, Down, Left }

    private void Awake()
    {
        leftWallField  = typeof(MazeCell).GetField("_leftWall", BindingFlags.NonPublic | BindingFlags.Instance);
        rightWallField = typeof(MazeCell).GetField("_rightWall", BindingFlags.NonPublic | BindingFlags.Instance);
        frontWallField = typeof(MazeCell).GetField("_frontWall", BindingFlags.NonPublic | BindingFlags.Instance);
        backWallField  = typeof(MazeCell).GetField("_backWall", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    /// <summary>
    /// UI 버튼(OnClick)에서 호출: 좌수법 알고리즘을 시작합니다.
    /// </summary>
    public void StartLeftHandRule()
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

        // 시작 셀과 목표 셀 지정 (예: 시작: [0,0], 목표: [width-1, depth-1])
        MazeCell startCell = grid[0, 0];
        MazeCell goalCell = grid[width - 1, depth - 1];

        // 좌수법 알고리즘용 에이전트 생성
        GameObject agent = Instantiate(agentPrefab, startCell.transform.position, Quaternion.identity);
        StartCoroutine(MoveAgentLeftHand(agent, grid, startCell, goalCell, width, depth));
    }

    /// <summary>
    /// 좌수법 규칙에 따라 에이전트가 미로를 탐색하는 코루틴.
    /// 현재 방향에서 왼쪽을 우선 확인하여, 통로가 있으면 방향 전환 후 이동하고,
    /// 없다면 전진, 오른쪽, 뒤로(턴어라운드) 순으로 시도합니다.
    /// </summary>
    private IEnumerator MoveAgentLeftHand(GameObject agent, MazeCell[,] grid, MazeCell currentCell, MazeCell goalCell, int width, int depth)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        // 초기 방향 설정: 예를 들어, 시작 셀에서 오른쪽(동쪽)을 바라보도록 설정
        Direction currentDirection = Direction.Right;

        // 목표에 도달할 때까지 반복
        while (currentCell != goalCell)
        {
            // 1. 현재 방향의 왼쪽 방향 확인
            Direction leftDir = TurnLeft(currentDirection);
            MazeCell leftNeighbor = GetNeighbor(currentCell, leftDir, grid, width, depth);
            if (leftNeighbor != null && IsAccessible(currentCell, leftNeighbor, leftDir))
            {
                currentDirection = leftDir;
                currentCell = leftNeighbor;
            }
            else
            {
                // 2. 현재 방향(전진) 확인
                MazeCell forwardNeighbor = GetNeighbor(currentCell, currentDirection, grid, width, depth);
                if (forwardNeighbor != null && IsAccessible(currentCell, forwardNeighbor, currentDirection))
                {
                    currentCell = forwardNeighbor;
                }
                else
                {
                    // 3. 오른쪽 방향 확인
                    Direction rightDir = TurnRight(currentDirection);
                    MazeCell rightNeighbor = GetNeighbor(currentCell, rightDir, grid, width, depth);
                    if (rightNeighbor != null && IsAccessible(currentCell, rightNeighbor, rightDir))
                    {
                        currentDirection = rightDir;
                        currentCell = rightNeighbor;
                    }
                    else
                    {
                        // 4. 어느 방향도 통하지 않으면 턴어라운드
                        currentDirection = TurnAround(currentDirection);
                        MazeCell backNeighbor = GetNeighbor(currentCell, currentDirection, grid, width, depth);
                        if (backNeighbor != null && IsAccessible(currentCell, backNeighbor, currentDirection))
                        {
                            currentCell = backNeighbor;
                        }
                        else
                        {
                            UnityEngine.Debug.LogError("현재 셀(" + currentCell.transform.position + ")에서 이동 가능한 길이 없습니다!");
                            break;
                        }
                    }
                }
            }

            // 에이전트 이동: 부드럽게 현재 셀 위치까지 이동
            Vector3 targetPos = currentCell.transform.position;
            while (Vector3.Distance(agent.transform.position, targetPos) > 0.01f)
            {
                agent.transform.position = Vector3.MoveTowards(agent.transform.position, targetPos, moveSpeed * Time.deltaTime);
                yield return null;
            }

            // 이동한 셀에 잔상 타일 남기기 (타일 마커를 추가하여 구분)
            LeaveTrailMarker(currentCell.transform.position);

            yield return null;
        }

        stopwatch.Stop();
        timeText.text = "Left-Hand Rule Time: " + stopwatch.Elapsed.TotalSeconds.ToString("F2") + " s";

        // 목표 도착 후 에이전트 제거
        Destroy(agent);
    }

    /// <summary>
    /// 현재 셀에서 주어진 방향으로 인접한 MazeCell을 반환합니다.
    /// </summary>
    private MazeCell GetNeighbor(MazeCell cell, Direction direction, MazeCell[,] grid, int width, int depth)
    {
        int x = (int)cell.transform.position.x;
        int z = (int)cell.transform.position.z;
        switch (direction)
        {
            case Direction.Up:
                if (z + 1 < depth) return grid[x, z + 1];
                break;
            case Direction.Right:
                if (x + 1 < width) return grid[x + 1, z];
                break;
            case Direction.Down:
                if (z - 1 >= 0) return grid[x, z - 1];
                break;
            case Direction.Left:
                if (x - 1 >= 0) return grid[x - 1, z];
                break;
        }
        return null;
    }

    // 방향 전환 헬퍼 함수들
    private Direction TurnLeft(Direction dir)
    {
        switch (dir)
        {
            case Direction.Up: return Direction.Left;
            case Direction.Left: return Direction.Down;
            case Direction.Down: return Direction.Right;
            case Direction.Right: return Direction.Up;
        }
        return dir;
    }

    private Direction TurnRight(Direction dir)
    {
        switch (dir)
        {
            case Direction.Up: return Direction.Right;
            case Direction.Right: return Direction.Down;
            case Direction.Down: return Direction.Left;
            case Direction.Left: return Direction.Up;
        }
        return dir;
    }

    private Direction TurnAround(Direction dir)
    {
        switch (dir)
        {
            case Direction.Up: return Direction.Down;
            case Direction.Down: return Direction.Up;
            case Direction.Left: return Direction.Right;
            case Direction.Right: return Direction.Left;
        }
        return dir;
    }

    /// <summary>
    /// 두 셀 사이의 통로가 열려 있는지(벽이 제거되었는지) 검사합니다.
    /// A*에서 사용한 것과 동일한 로직을 적용합니다.
    /// </summary>
    private bool IsAccessible(MazeCell fromCell, MazeCell toCell, Direction direction)
    {
        switch (direction)
        {
            case Direction.Right:
                return !((GameObject)rightWallField.GetValue(fromCell)).activeSelf &&
                       !((GameObject)leftWallField.GetValue(toCell)).activeSelf;
            case Direction.Left:
                return !((GameObject)leftWallField.GetValue(fromCell)).activeSelf &&
                       !((GameObject)rightWallField.GetValue(toCell)).activeSelf;
            case Direction.Up:
                return !((GameObject)frontWallField.GetValue(fromCell)).activeSelf &&
                       !((GameObject)backWallField.GetValue(toCell)).activeSelf;
            case Direction.Down:
                return !((GameObject)backWallField.GetValue(fromCell)).activeSelf &&
                       !((GameObject)frontWallField.GetValue(toCell)).activeSelf;
        }
        return false;
    }

    /// <summary>
    /// 좌수법 탐색 시 이동한 셀에 잔상 타일을 남깁니다.
    /// (타일 크기: 1×0.2×1, 색상: 초록색, 타입: PathfindingOther)
    /// </summary>
        private void LeaveTrailMarker(Vector3 position)
    {
        TileManager.CreateOrUpdateTile(position, FloorTileType.PathfindingOther);
    }
}
