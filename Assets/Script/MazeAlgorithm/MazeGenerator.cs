using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class MazeGenerator : MonoBehaviour
{
    [SerializeField] private MazeCell _mazeCellPrefab;
    [SerializeField] private TMP_InputField widthInputField;   // 미로 너비 입력 필드
    [SerializeField] private TMP_InputField depthInputField;   // 미로 깊이 입력 필드
    [SerializeField] private float generationDelay = 0.000000001f;      // 생성 딜레이 (속도 조절)

    private int _mazeWidth;
    private int _mazeDepth;
    private MazeCell[,] _mazeGrid;

    public void OnClickStart()
    {
        // 이전에 생성된 미로 셀 제거
        if (_mazeGrid != null)
        {
            foreach (var cell in _mazeGrid)
            {
                if (cell != null)
                    Destroy(cell.gameObject);
            }
        }
        // 이전에 생성된 타일(타일 마커가 붙은 오브젝트) 모두 제거
        TileManager.ClearAllTiles();

        // 입력 값 검증
        if (!int.TryParse(widthInputField.text, out int width) || width <= 0)
        {
            Debug.LogError("유효한 미로 너비를 입력하세요!");
            return;
        }
        if (!int.TryParse(depthInputField.text, out int depth) || depth <= 0)
        {
            Debug.LogError("유효한 미로 깊이를 입력하세요!");
            return;
        }
        _mazeWidth = width;
        _mazeDepth = depth;

        // 미로 그리드 생성
        _mazeGrid = new MazeCell[_mazeWidth, _mazeDepth];
        for (int x = 0; x < _mazeWidth; x++)
        {
            for (int z = 0; z < _mazeDepth; z++)
            {
                _mazeGrid[x, z] = Instantiate(_mazeCellPrefab, new Vector3(x, 0, z), Quaternion.identity);
            }
        }
        // 미로 생성 코루틴 실행 (재귀 호출)
        StartCoroutine(GenerateMaze(null, _mazeGrid[0, 0]));
    }

    private IEnumerator GenerateMaze(MazeCell previousCell, MazeCell currentCell)
    {
        currentCell.Visit();
        ClearWalls(previousCell, currentCell);
        yield return new WaitForSeconds(generationDelay);

        MazeCell nextCell;
        do
        {
            nextCell = GetNextUnvisitedCell(currentCell);
            if (nextCell != null)
                yield return GenerateMaze(currentCell, nextCell);
        } while (nextCell != null);
    }

    private MazeCell GetNextUnvisitedCell(MazeCell currentCell)
    {
        var unvisitedCells = GetUnvisitedCells(currentCell);
        return unvisitedCells.OrderBy(_ => Random.Range(1, 10)).FirstOrDefault();
    }

    private IEnumerable<MazeCell> GetUnvisitedCells(MazeCell currentCell)
    {
        int x = (int)currentCell.transform.position.x;
        int z = (int)currentCell.transform.position.z;

        if (x + 1 < _mazeWidth && !_mazeGrid[x + 1, z].IsVisited)
            yield return _mazeGrid[x + 1, z];
        if (x - 1 >= 0 && !_mazeGrid[x - 1, z].IsVisited)
            yield return _mazeGrid[x - 1, z];
        if (z + 1 < _mazeDepth && !_mazeGrid[x, z + 1].IsVisited)
            yield return _mazeGrid[x, z + 1];
        if (z - 1 >= 0 && !_mazeGrid[x, z - 1].IsVisited)
            yield return _mazeGrid[x, z - 1];
    }

    private void ClearWalls(MazeCell previousCell, MazeCell currentCell)
    {
        if (previousCell == null)
            return;

        if (previousCell.transform.position.x < currentCell.transform.position.x)
        {
            previousCell.ClearRightWall();
            currentCell.ClearLeftWall();
        }
        else if (previousCell.transform.position.x > currentCell.transform.position.x)
        {
            previousCell.ClearLeftWall();
            currentCell.ClearRightWall();
        }
        else if (previousCell.transform.position.z < currentCell.transform.position.z)
        {
            previousCell.ClearFrontWall();
            currentCell.ClearBackWall();
        }
        else if (previousCell.transform.position.z > currentCell.transform.position.z)
        {
            previousCell.ClearBackWall();
            currentCell.ClearFrontWall();
        }
    }

    public int GetWidth()  { return _mazeWidth; }
    public int GetDepth()  { return _mazeDepth; }
}
