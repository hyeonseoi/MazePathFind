using System.Collections;
using System.Collections.Generic;
using System.Diagnostics; // Stopwatch 사용
using System.Reflection;
using UnityEngine;
using TMPro;

public class RightHandRuleSolver : MonoBehaviour
{
    [Header("참조할 오브젝트")]
    public MazeGenerator mazeGenerator;    // MazeGenerator 스크립트를 할당
    public GameObject agentPrefab;         // 에이전트 프리팹 (이동 애니메이션용)
    public TMP_Text timeText;              // 경과 시간 표시용

    [Header("에이전트 이동 설정")]
    public float moveSpeed = 2f;

    // MazeCell 내부의 벽(GameObject)에 접근하기 위한 Reflection 필드
    private FieldInfo leftWallField;
    private FieldInfo rightWallField;
    private FieldInfo frontWallField;
    private FieldInfo backWallField;

    // 방향 열거형 (Up: 전진, Right: x+, Down: 후진, Left: x-)
    public enum Direction { Up, Right, Down, Left }

    private void Awake()
    {
        leftWallField  = typeof(MazeCell).GetField("_leftWall", BindingFlags.NonPublic | BindingFlags.Instance);
        rightWallField = typeof(MazeCell).GetField("_rightWall", BindingFlags.NonPublic | BindingFlags.Instance);
        frontWallField = typeof(MazeCell).GetField("_frontWall", BindingFlags.NonPublic | BindingFlags.Instance);
        backWallField  = typeof(MazeCell).GetField("_backWall", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    /// <summary>
    /// UI 버튼(OnClick)에서 호출: 우수법 알고리즘을 시작합니다.
    /// </summary>
    public void StartRightHandRule()
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

        // 우수법 에이전트 생성 및 이동 시작
        GameObject agent = Instantiate(agentPrefab, startCell.transform.position, Quaternion.identity);
        StartCoroutine(MoveAgentRightHand(agent, grid, startCell, goalCell, width, depth));
    }

    private IEnumerator MoveAgentRightHand(GameObject agent, MazeCell[,] grid, MazeCell currentCell, MazeCell goalCell, int width, int depth)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        // 초기 방향 설정 (예: 오른쪽)
        Direction currentDirection = Direction.Right;

        while (currentCell != goalCell)
        {
            // 우수법: 우측 방향을 우선 확인
            Direction rightDir = TurnRight(currentDirection);
            MazeCell rightNeighbor = GetNeighbor(currentCell, rightDir, grid, width, depth);
            if (rightNeighbor != null && IsAccessible(currentCell, rightNeighbor, rightDir))
            {
                currentDirection = rightDir;
                currentCell = rightNeighbor;
            }
            else
            {
                // 전진
                MazeCell forwardNeighbor = GetNeighbor(currentCell, currentDirection, grid, width, depth);
                if (forwardNeighbor != null && IsAccessible(currentCell, forwardNeighbor, currentDirection))
                {
                    currentCell = forwardNeighbor;
                }
                else
                {
                    // 좌측 확인
                    Direction leftDir = TurnLeft(currentDirection);
                    MazeCell leftNeighbor = GetNeighbor(currentCell, leftDir, grid, width, depth);
                    if (leftNeighbor != null && IsAccessible(currentCell, leftNeighbor, leftDir))
                    {
                        currentDirection = leftDir;
                        currentCell = leftNeighbor;
                    }
                    else
                    {
                        // 이동 불가능하면 턴어라운드
                        currentDirection = TurnAround(currentDirection);
                        MazeCell backNeighbor = GetNeighbor(currentCell, currentDirection, grid, width, depth);
                        if (backNeighbor != null && IsAccessible(currentCell, backNeighbor, currentDirection))
                        {
                            currentCell = backNeighbor;
                        }
                        else
                        {
                            UnityEngine.Debug.LogError("No accessible path from cell at " + currentCell.transform.position);
                            break;
                        }
                    }
                }
            }

            // 부드럽게 이동
            Vector3 targetPos = currentCell.transform.position;
            while (Vector3.Distance(agent.transform.position, targetPos) > 0.01f)
            {
                agent.transform.position = Vector3.MoveTowards(agent.transform.position, targetPos, moveSpeed * Time.deltaTime);
                yield return null;
            }
            // 이동한 셀에 잔상 타일 생성 (타일 타입: RightHandRule)
            TileManager.CreateOrUpdateTile(currentCell.transform.position, FloorTileType.RightHandRule);
            yield return null;
        }

        stopwatch.Stop();
        timeText.text = "Right-Hand Rule Time: " + stopwatch.Elapsed.TotalSeconds.ToString("F2") + " s";
        Destroy(agent);
    }

    private MazeCell GetNeighbor(MazeCell cell, Direction dir, MazeCell[,] grid, int width, int depth)
    {
        int x = (int)cell.transform.position.x;
        int z = (int)cell.transform.position.z;
        switch (dir)
        {
            case Direction.Up:    if (z + 1 < depth) return grid[x, z + 1]; break;
            case Direction.Right: if (x + 1 < width) return grid[x + 1, z]; break;
            case Direction.Down:  if (z - 1 >= 0)    return grid[x, z - 1]; break;
            case Direction.Left:  if (x - 1 >= 0)    return grid[x - 1, z]; break;
        }
        return null;
    }

    private Direction TurnRight(Direction dir)
    {
        switch (dir)
        {
            case Direction.Up:    return Direction.Right;
            case Direction.Right: return Direction.Down;
            case Direction.Down:  return Direction.Left;
            case Direction.Left:  return Direction.Up;
        }
        return dir;
    }
    private Direction TurnLeft(Direction dir)
    {
        switch (dir)
        {
            case Direction.Up:    return Direction.Left;
            case Direction.Left:  return Direction.Down;
            case Direction.Down:  return Direction.Right;
            case Direction.Right: return Direction.Up;
        }
        return dir;
    }
    private Direction TurnAround(Direction dir)
    {
        switch (dir)
        {
            case Direction.Up:    return Direction.Down;
            case Direction.Down:  return Direction.Up;
            case Direction.Left:  return Direction.Right;
            case Direction.Right: return Direction.Left;
        }
        return dir;
    }

    private bool IsAccessible(MazeCell fromCell, MazeCell toCell, Direction dir)
    {
        switch (dir)
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
}
