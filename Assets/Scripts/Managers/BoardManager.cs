using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Collections;

public class BoardManager : MonoBehaviour
{
  public static BoardManager Instance;
  [SerializeField] internal List<int> BlockNumbers = new();
  [SerializeField] internal List<Color> BlockColors = new();
  [SerializeField] internal Gradient colorGradient;
  [SerializeField] internal float basePullDownSpeed = 15f;
  [SerializeField] internal float fastPullDownSpeed = 0.2f;
  [SerializeField] private float fallDuration = 0.2f;
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
    if (index != -1)
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

  internal void CheckAndExpandNumbers(int newValue)
  {
    if (!BlockNumbers.Contains(newValue))
    {
      BlockColors.Add(GetColorForValue(newValue));
      BlockNumbers.Add(newValue);
    }
  }

  internal IEnumerator PullTheBlock(Block BlockToPull, float duration)
  {
    BlockData BlockData = GridManager.Instance.GetMovableBlockData(InputManager.Instance.IColumnIndex);
    if (BlockData == null)
    {
      Debug.LogError("Game Over - No available space!");
      yield break;
    }
    CurrBlockTween?.Kill();
    CurrBlockTween = BlockToPull.transform.DOLocalMoveY(BlockData.boardPosition.localPosition.y, duration)
    .OnComplete(() =>
    {
      BlockToPull.transform.localPosition = BlockData.boardPosition.localPosition;
      GridManager.Instance.SetBlockData(BlockToPull, BlockData.gridPosition);
      StartCascade();
    });
  }

  internal IEnumerator PullTheBlock(Block BlockToPull, BlockData MoveToBlock, float duration)
  {
    yield return BlockToPull.transform.DOLocalMoveY(MoveToBlock.boardPosition.localPosition.y, duration).WaitForCompletion();
    GridManager.Instance.SetBlockData(BlockToPull, MoveToBlock.gridPosition);
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
      yield return HandleDownwardMerges();
      yield return new WaitForSeconds(fallDuration);

      didSideMerge = false;
      yield return HandleSideMerges();
      yield return new WaitForSeconds(fallDuration);

      keepCascading = didDownwardMerge || didSideMerge;
    }

    yield return new WaitForSeconds(fallDuration);

    SpawnManager.Instance.SpawnNextBlock();
  }

  private IEnumerator HandleDownwardMerges()
  {
    bool didMerge = false;

    for (int x = 0; x < GridManager.Instance.BlockGrid.Count; x++)
    {
      for (int y = GridManager.Instance.BlockGrid[x].Column.Count - 1; y >= 0; y--)
      {
        Vector2Int currPos = new Vector2Int(x, y);
        Vector2Int belowPos = new Vector2Int(x, y + 1);

        if (!GridManager.Instance.IsValidPosition(currPos) || !GridManager.Instance.IsValidPosition(belowPos))
          continue;

        var blockAbove = GridManager.Instance.GetBlockData(currPos);
        var blockBelow = GridManager.Instance.GetBlockData(belowPos);

        if (blockAbove != null && blockBelow != null && blockAbove.value == blockBelow.value)
        {
          yield return GridManager.Instance.BlockList.Find(b => b.GridPos == currPos).MergeBlock(GridManager.Instance.BlockList.Find(b => b.GridPos == belowPos));
          didMerge = true;
          yield return new WaitForSeconds(fallDuration);
        }
      }
    }

    didDownwardMerge = didMerge;
  }


  private IEnumerator HandleSideMerges()
  {
    bool didMerge = false;

    for (int y = 0; y < GridManager.Instance.BlockGrid[0].Column.Count; y++)
    {
      for (int x = 0; x < GridManager.Instance.BlockGrid.Count - 1; x++)
      {
        Vector2Int leftPos = new Vector2Int(x - 1, y);
        Vector2Int rightPos = new Vector2Int(x + 1, y);

        if (!GridManager.Instance.IsValidPosition(leftPos) || !GridManager.Instance.IsValidPosition(rightPos))
          continue;

        var leftBlock = GridManager.Instance.GetBlockData(leftPos);
        var rightBlock = GridManager.Instance.GetBlockData(rightPos);
        var middleBlock = GridManager.Instance.GetBlockData(new Vector2Int(x, y));

        if (middleBlock == null)
          continue;

        if (leftBlock != null && rightBlock != null && rightBlock.value == middleBlock.value && leftBlock.value == middleBlock.value)
        {
          yield return GridManager.Instance.BlockList.Find(b => b.GridPos == middleBlock.gridPosition).MergeBlock(GridManager.Instance.BlockList.Find(b => b.GridPos == leftBlock.gridPosition), GridManager.Instance.BlockList.Find(b => b.GridPos == rightBlock.gridPosition));
          didMerge = true;
          yield return new WaitForSeconds(fallDuration);
          continue;
        }
        if (leftBlock != null && rightBlock == null && leftBlock.value == middleBlock.value)
        {
          yield return GridManager.Instance.BlockList.Find(b => b.GridPos == middleBlock.gridPosition).MergeBlock(GridManager.Instance.BlockList.Find(b => b.GridPos == leftBlock.gridPosition));
          didMerge = true;
          yield return new WaitForSeconds(fallDuration);
          continue;
        }
        if (leftBlock == null && rightBlock != null && rightBlock.value == middleBlock.value)
        {
          yield return GridManager.Instance.BlockList.Find(b => b.GridPos == middleBlock.gridPosition).MergeBlock(GridManager.Instance.BlockList.Find(b => b.GridPos == rightBlock.gridPosition));
          didMerge = true;
          yield return new WaitForSeconds(fallDuration);
          continue;
        }
      }
    }
    didSideMerge = didMerge;
  }
}

