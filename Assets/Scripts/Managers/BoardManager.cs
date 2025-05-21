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
    for(int i=0;i<GridManager.Instance.BlockGrid.Count;i++)
    {
      for(int j=0;j<GridManager.Instance.BlockGrid[i].Column.Count;j++)
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

  internal IEnumerator PullTheBlock(Block BlockToPull, float duration)
  {
    BlockData BlockData = GridManager.Instance.GetMovableBlockData(InputManager.Instance.IColumnIndex);
    if (BlockData == null)
    {
      Debug.LogError("Game Over - No available space!");
      yield break;
    }

    CurrBlockTween?.Kill();
    CurrBlockTween = BlockToPull.transform.DOMoveY(BlockData.boardPosition.position.y, duration)
    .OnComplete(() =>
    {
      Debug.Log("Block Dropped at : " + BlockData.gridPosition.ToString());
      BlockToPull.transform.position = BlockData.boardPosition.position;
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
      if(didDownwardMerge)
        yield return new WaitForSeconds(fallDuration);

      didSideMerge = false;
      yield return HandleSideMerges();
      if(didSideMerge)
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
        if(GridManager.Instance.IsEmpty(new Vector2Int(x, y)))
          continue;
        
        // Check if the block above and below are valid positions
        Vector2Int currPos = new Vector2Int(x, y);
        Vector2Int belowPos = new Vector2Int(x, y + 1);

        if (!GridManager.Instance.IsValidPosition(currPos) || !GridManager.Instance.IsValidPosition(belowPos))
          continue;

        var blockAbove = GridManager.Instance.GetBlockData(currPos);
        var blockBelow = GridManager.Instance.GetBlockData(belowPos);

        if(blockAbove.value == 0 || blockBelow.value == 0)
          continue;

        if (blockAbove != null && blockBelow != null && blockAbove.value == blockBelow.value)
        {
          Debug.Log($"Merging {blockAbove.gridPosition} and {blockBelow.gridPosition}");
          yield return GridManager.Instance.BlockList.Find(b => b.GridPos == belowPos).MergeBlock(GridManager.Instance.BlockList.Find(b => b.GridPos == currPos));
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

    for (int x = 0; x < GridManager.Instance.BlockGrid.Count; x++)
    {
      for (int y = GridManager.Instance.BlockGrid[x].Column.Count - 1; y >= 0; y--)
      {
        if (GridManager.Instance.IsEmpty(new Vector2Int(x, y)))
          continue;
        
        // Check if the block to the left and right are valid positions
        Vector2Int leftPos = new Vector2Int(x - 1, y);
        Vector2Int rightPos = new Vector2Int(x + 1, y);

        if (!GridManager.Instance.IsValidPosition(leftPos) || !GridManager.Instance.IsValidPosition(rightPos))
          continue;

        if(GridManager.Instance.IsEmpty(leftPos) && GridManager.Instance.IsEmpty(rightPos))
          continue;

        var leftBlock = GridManager.Instance.GetBlockData(leftPos);
        var rightBlock = GridManager.Instance.GetBlockData(rightPos);
        var middleBlock = GridManager.Instance.GetBlockData(new Vector2Int(x, y));

        if (rightBlock.value == middleBlock.value && leftBlock.value == middleBlock.value)
        {
          Debug.Log("Merging left and right");
          yield return GridManager.Instance.BlockList.Find(b => b.GridPos == middleBlock.gridPosition).MergeBlock(GridManager.Instance.BlockList.Find(b => b.GridPos == leftBlock.gridPosition), GridManager.Instance.BlockList.Find(b => b.GridPos == rightBlock.gridPosition));
          didMerge = true;
          yield return new WaitForSeconds(fallDuration);
          continue;
        }
        if (leftBlock.value == middleBlock.value)
        {
          Debug.Log("Merging left");
          yield return GridManager.Instance.BlockList.Find(b => b.GridPos == middleBlock.gridPosition).MergeBlock(GridManager.Instance.BlockList.Find(b => b.GridPos == leftBlock.gridPosition), true);
          didMerge = true;
          yield return new WaitForSeconds(fallDuration);
          continue;
        }
        if (rightBlock.value == middleBlock.value)
        {
          Debug.Log("Merging right");
          yield return GridManager.Instance.BlockList.Find(b => b.GridPos == middleBlock.gridPosition).MergeBlock(GridManager.Instance.BlockList.Find(b => b.GridPos == rightBlock.gridPosition), true);
          didMerge = true;
          yield return new WaitForSeconds(fallDuration);
          continue;
        }
      }
    }
    didSideMerge = didMerge;
  }
}

