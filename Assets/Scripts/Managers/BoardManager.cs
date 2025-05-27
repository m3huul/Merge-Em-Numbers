using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Collections;
using UnityEngine.UI;

public class BoardManager : MonoBehaviour
{
  public static BoardManager Instance;
  [SerializeField] internal List<int> BlockNumbers = new();
  [SerializeField] internal List<Color> BlockColors = new();
  [SerializeField] internal float basePullDownSpeed = 3f;
  [SerializeField] internal float fastPullDownSpeed = 0.05f;
  [SerializeField] private float fallDuration = 0.05f;
  internal int FocusedColumnIndex = 2;
  private Tween CurrBlockTween;
  private bool didDownwardMerge = false;
  private bool didSideMerge = false;
  [SerializeField] private Button RestartButton;
  void Awake()
  {
    if (Instance == null)
    {
      Instance = this;
    }

    RestartButton.onClick.AddListener(() =>
    {
      RestartGame();
    });

    Time.timeScale = 1f;
    Application.targetFrameRate = -1;
  }

  void Start()
  {
    for (int i = 0; i < 4; i++)
    {
      int newValue = GenerateNextNumber();
      BlockColors.Add(GetColorForValue(newValue));
      BlockNumbers.Add(newValue);
    }
  }

  internal void KillGame()
  {
    Debug.Log("No Block To Move To. Game End");
    InputManager.Instance.enabled = false;
    StopAllCoroutines();
    if (CurrBlockTween != null)
    {
      CurrBlockTween.Kill();
      CurrBlockTween = null;
    }
    RestartButton.gameObject.SetActive(true);
  }

  void RestartGame()
  {
    Debug.Log("Restarting Game");
    RestartButton.gameObject.SetActive(false);
    GridManager.Instance.Reset();
    SpawnManager.Instance.Reset();
  }

  int GenerateNextNumber()
  {
    int lastNumber = BlockNumbers[BlockNumbers.Count - 1];
    return lastNumber * 2;
  }

  internal Color GetColorForValue(int value)
  {
    int index = BlockNumbers.IndexOf(value);
    if (index != -1 && index < BlockColors.Count)
    {
      return BlockColors[index];
    }
    else
    {
      return GenerateNewColor();
    }
  }

  Color GenerateNewColor()
  {
    float hue = BlockNumbers.Count * 0.15f % 1;
    return Color.HSVToRGB(hue, 0.8f, 1.0f);
  }

  internal void CheckAndExpandNumbers()
  {
    for (int i = 0; i < GridManager.Instance.BlockGrid.Count; i++)
    {
      for (int j = 0; j < GridManager.Instance.BlockGrid[i].Column.Count; j++)
      {
        int blockValue = GridManager.Instance.BlockGrid[i].Column[j].value;
        if (!BlockNumbers.Contains(blockValue))
        {
          BlockNumbers.Add(blockValue);
          BlockColors.Add(GetColorForValue(blockValue));
        }
      }
    }
  }

  internal IEnumerator PullTheBlock(Block BlockToPull, BlockData MoveToBlock, float duration)
  {
    if (MoveToBlock == null)
    {
      Debug.Log("Game Over - No available space!");
      yield break;
    }
    // Debug.Log("Pulling block to: " + MoveToBlock.gridPosition.ToString());
    CurrBlockTween?.Kill();
    CurrBlockTween = BlockToPull.transform.DOMoveY(MoveToBlock.boardPosition.position.y, duration)
    .OnComplete(() =>
    {
      InputManager.Instance.BlockTransform = null;
      InputManager.Instance.enabled = false;
      // Debug.Log("Block Dropped at : " + MoveToBlock.gridPosition.ToString());
      BlockToPull.transform.position = MoveToBlock.boardPosition.position;
      GridManager.Instance.SetBlockData(BlockToPull, MoveToBlock.gridPosition);
      StartCascade();
    })
    .SetEase(Ease.Linear);
  }

  internal IEnumerator PullTheBlockDown(Block BlockToPull, BlockData BlockMoveTo, float duration)
  {
    yield return BlockToPull.transform.DOLocalMoveY(BlockMoveTo.boardPosition.localPosition.y, duration).SetEase(Ease.Linear).WaitForCompletion();
    GridManager.Instance.SetBlockData(BlockToPull, BlockMoveTo.gridPosition, BlockToPull.Value);
  }

  public void StartCascade()
  {
    StartCoroutine(CascadeRoutine());
  }

  private IEnumerator CascadeRoutine()
  {
    bool keepCascading = true;

    while (keepCascading)
    {
      Debug.Log("Cascading...");
      yield return GridManager.Instance.MoveAllBlocksDown();
      Debug.Log("After Move All Blocks Down");

      yield return new WaitForSecondsRealtime(fallDuration); // Wait before merge
      Debug.Log("Cascading...");

      didDownwardMerge = false;
      yield return HandleDownwardMerges(FocusedColumnIndex);
      if (didDownwardMerge)
        yield return new WaitForSecondsRealtime(fallDuration);

      Debug.Log("After Handle Downward Merges");
      didSideMerge = false;
      yield return HandleSideMerges(FocusedColumnIndex);
      if (didSideMerge)
        yield return new WaitForSecondsRealtime(fallDuration);

      Debug.Log("After Handle Side Merges");

      keepCascading = didDownwardMerge || didSideMerge;
    }

    yield return new WaitForSecondsRealtime(fallDuration);

    SpawnManager.Instance.SpawnNextBlock();
  }

  private IEnumerator HandleDownwardMerges(int ColIndex)
  {
    yield return CheckCenterColumn(ColIndex);
    yield return CheckLeftColumns(ColIndex);
    yield return CheckRightColumns(ColIndex);
  }

  IEnumerator CheckCenterColumn(int col, bool down = true)
  {
    for (int i = GridManager.Instance.BlockGrid[col].Column.Count - 1; i >= 0; i--)
    {
      if (down)
      {
        yield return DownwardMerge(col, i);
      }
      else
      {
        yield return SideMerge(col, i);
      }
    }
  }

  IEnumerator CheckLeftColumns(int col, bool down = true)
  {
    for (int i = col - 1; i >= 0; i--)
    {
      for (int j = GridManager.Instance.BlockGrid[i].Column.Count - 1; j >= 0; j--)
      {
        if (down)
        {
          yield return DownwardMerge(i, j);
        }
        else
        {
          yield return SideMerge(i, j);
        }
      }
    }
  }

  IEnumerator CheckRightColumns(int col, bool down = true)
  {
    for (int i = col + 1; i < GridManager.Instance.BlockGrid.Count; i++)
    {
      for (int j = GridManager.Instance.BlockGrid[i].Column.Count - 1; j >= 0; j--)
      {
        if (down)
        {
          yield return DownwardMerge(i, j);
        }
        else
        {
          yield return SideMerge(i, j);
        }
      }
    }
  }

  IEnumerator DownwardMerge(int x, int y)
  {
    if (GridManager.Instance.IsEmpty(new Vector2Int(x, y)))
      yield break;

    // Check if the block above and below are valid positions
    Vector2Int currPos = new(x, y);
    Vector2Int belowPos = new(x, y + 1);

    if (!GridManager.Instance.IsValidPosition(currPos) || !GridManager.Instance.IsValidPosition(belowPos))
      yield break;

    var blockAbove = GridManager.Instance.GetBlockData(currPos);
    var blockBelow = GridManager.Instance.GetBlockData(belowPos);

    if (blockAbove.value == 0 || blockBelow.value == 0)
      yield break;

    if (blockAbove != null && blockBelow != null && blockAbove.value == blockBelow.value)
    {
      // Debug.Log($"Merging {blockAbove.gridPosition} and {blockBelow.gridPosition}");
      yield return GridManager.Instance.BlockList.Find(b => b.GridPos == belowPos).MergeBlock(GridManager.Instance.BlockList.Find(b => b.GridPos == currPos));
      didDownwardMerge = true;
      yield return new WaitForSecondsRealtime(fallDuration);
    }
  }

  private IEnumerator HandleSideMerges(int ColIndex)
  {
    yield return CheckCenterColumn(ColIndex, false);
    yield return CheckLeftColumns(ColIndex, false);
    yield return CheckRightColumns(ColIndex, false);
  }

  IEnumerator SideMerge(int x, int y)
  {
    if (GridManager.Instance.IsEmpty(new Vector2Int(x, y)))
      yield break;

    // Check if the block to the left and right are valid positions
    Vector2Int leftPos = new Vector2Int(x - 1, y);
    Vector2Int rightPos = new Vector2Int(x + 1, y);

    if (GridManager.Instance.IsEmpty(leftPos) && GridManager.Instance.IsEmpty(rightPos))
      yield break;

    var leftBlock = GridManager.Instance.GetBlockData(leftPos);
    var rightBlock = GridManager.Instance.GetBlockData(rightPos);
    var middleBlock = GridManager.Instance.GetBlockData(new Vector2Int(x, y));

    if (rightBlock?.value == middleBlock?.value && leftBlock?.value == middleBlock?.value)
    {
      // Debug.Log("Merging left and right");
      // Debug.Log("Merging " + middleBlock?.gridPosition + " and " + leftBlock?.gridPosition + " and " + rightBlock?.gridPosition);
      yield return GridManager.Instance.BlockList.Find(b => b.GridPos == middleBlock?.gridPosition).MergeBlock(GridManager.Instance.BlockList.Find(b => b.GridPos == leftBlock?.gridPosition), GridManager.Instance.BlockList.Find(b => b.GridPos == rightBlock?.gridPosition));
      didSideMerge = true;
      yield return new WaitForSecondsRealtime(fallDuration);
    }
    if (leftBlock?.value == middleBlock?.value)
    {
      // Debug.Log("Merging left");
      // Debug.Log("Merging " + middleBlock?.gridPosition + " and " + leftBlock?.gridPosition);
      yield return GridManager.Instance.BlockList.Find(b => b.GridPos == middleBlock?.gridPosition).MergeBlock(GridManager.Instance.BlockList.Find(b => b.GridPos == leftBlock?.gridPosition), true);
      didSideMerge = true;
      yield return new WaitForSecondsRealtime(fallDuration);
    }
    if (rightBlock?.value == middleBlock?.value)
    {
      // Debug.Log("Merging right");
      // Debug.Log("Merging " + middleBlock?.gridPosition + " and " + rightBlock?.gridPosition);
      yield return GridManager.Instance.BlockList.Find(b => b.GridPos == middleBlock?.gridPosition).MergeBlock(GridManager.Instance.BlockList.Find(b => b.GridPos == rightBlock?.gridPosition), true);
      didSideMerge = true;
      yield return new WaitForSecondsRealtime(fallDuration);
    }
  }
}

