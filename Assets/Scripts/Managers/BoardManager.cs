using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Collections;
using System;

public class BoardManager : MonoBehaviour
{
  public static BoardManager Instance;
  [SerializeField] internal List<int> BlockNumbers = new();
  [SerializeField] internal List<Color> BlockColors = new();
  [SerializeField] internal Gradient colorGradient;
  [SerializeField] internal float basePullDownSpeed = 3f;
  [SerializeField] internal float fastPullDownSpeed = 0.05f;
  [SerializeField] private float fallDuration = 0.05f;
  internal int FocusedColumnIndex = 2;
  private Tween CurrBlockTween;
  private bool didDownwardMerge = false;
  private bool didSideMerge = false;
  void Awake()
  {
    if (Instance == null)
    {
      Instance = this;
    }
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
    float hue = BlockNumbers.Count * 0.1f % 1;
    return Color.HSVToRGB(hue, 0.8f, 1.0f);
  }

  internal void CheckAndExpandNumbers()
  {
    foreach (Block block in GridManager.Instance.BlockList)
    {
      int blockValue = block.Value;
      if (!BlockNumbers.Contains(blockValue))
      {
        BlockNumbers.Add(blockValue);
        BlockColors.Add(GetColorForValue(blockValue));
      }
    }
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
    Debug.Log("Pulling block to: " + MoveToBlock.gridPosition.ToString());
    float Duration = (MoveToBlock.gridPosition.y+1) * duration;

    CurrBlockTween?.Kill();
    CurrBlockTween = BlockToPull.transform.DOMoveY(MoveToBlock.boardPosition.position.y, duration)
    .OnComplete(() =>
    {
      InputManager.Instance.BlockTransform = null;
      Debug.Log("Block Dropped at : " + MoveToBlock.gridPosition.ToString());
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
      yield return GridManager.Instance.MoveAllBlocksDown();

      yield return new WaitForSeconds(fallDuration); // Wait before merge

      didDownwardMerge = false;
      yield return HandleDownwardMerges(FocusedColumnIndex);
      if (didDownwardMerge)
        yield return new WaitForSeconds(fallDuration);

      didSideMerge = false;
      yield return HandleSideMerges(FocusedColumnIndex);
      if (didSideMerge)
        yield return new WaitForSeconds(fallDuration);

      keepCascading = didDownwardMerge || didSideMerge;
    }

    yield return new WaitForSeconds(fallDuration);

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
      yield return new WaitForSeconds(fallDuration);
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
      yield return new WaitForSeconds(fallDuration);
    }
    if (leftBlock?.value == middleBlock?.value)
    {
      // Debug.Log("Merging left");
      // Debug.Log("Merging " + middleBlock?.gridPosition + " and " + leftBlock?.gridPosition);
      yield return GridManager.Instance.BlockList.Find(b => b.GridPos == middleBlock?.gridPosition).MergeBlock(GridManager.Instance.BlockList.Find(b => b.GridPos == leftBlock?.gridPosition), true);
      didSideMerge = true;
      yield return new WaitForSeconds(fallDuration);
    }
    if (rightBlock?.value == middleBlock?.value)
    {
      // Debug.Log("Merging right");
      // Debug.Log("Merging " + middleBlock?.gridPosition + " and " + rightBlock?.gridPosition);
      yield return GridManager.Instance.BlockList.Find(b => b.GridPos == middleBlock?.gridPosition).MergeBlock(GridManager.Instance.BlockList.Find(b => b.GridPos == rightBlock?.gridPosition), true);
      didSideMerge = true;
      yield return new WaitForSeconds(fallDuration);
    }
  }
}

