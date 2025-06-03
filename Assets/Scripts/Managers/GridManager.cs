using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
  public static GridManager Instance;
  [SerializeField] private int width = 5, height = 8;
  [SerializeField] private Transform GridParent;
  [SerializeField] internal List<BoardBlocks> BlockGrid; //List of all block GO's in the grid  Col, Row
  [SerializeField] internal List<Block> BlockList; //List of all block GO's in the grid
  [SerializeField] private float EmptyDroppableOffset = 0.1f; // Offset to check if the block can be dropped
  
  private void Awake()
  {
    Instance = this;
    if (!GridParent)
    {
      Debug.LogError("GridParent is not assigned in the inspector.");
    }
    else
    {
      BlockGrid = new();
      for (int i = 0; i < width; i++)
      {
        BlockGrid.Add(new BoardBlocks());
        for (int j = 0; j < height; j++)
        {
          BlockGrid[i].Column.Add(new BlockData());
          BlockGrid[i].Column[j].boardPosition = GridParent.GetChild(i).GetChild(j);
          BlockGrid[i].Column[j].gridPosition = new Vector2Int(i, j);
        }
      }
    }
  }

  internal void Reset()
  {
    BlockList.ForEach(x => Destroy(x.gameObject));
    BlockList.Clear();
    for (int i = 0; i < BlockGrid.Count; i++)
    {
      for (int j = 0; j < BlockGrid[i].Column.Count; j++)
      {
        BlockGrid[i].Column[j].value = 0;
      }
    }
  }

  internal BlockData GetBlockData(Vector2Int position)
  {
    if (IsValidPosition(position))
    {
      return BlockGrid[position.x].Column[position.y];
    }
    else
    {
      // Debug.LogError("Invalid position while getting block data.");
      return null;
    }
  }

  internal BlockData GetMovableBlockData(Transform currentBlockTransform, int col)
  {
    for (int i = BlockGrid[col].Column.Count - 1; i >= 0; i--)
    {
      if (IsEmpty(new Vector2Int(col, i)) && currentBlockTransform.position.y >= BlockGrid[col].Column[i].boardPosition.position.y + EmptyDroppableOffset)
      {
        return BlockGrid[col].Column[i];
      }
    }
    return null;
  }

  internal BlockData GetMovableBlockData(int col, int row)
  {
    for (int i = row; i < height; i++)
    {
      if (IsEmpty(new Vector2Int(col, i)))
      {
        return BlockGrid[col].Column[i];
      }
    }
    return null;
  }

  internal BlockData GetDroppableBlockData(Transform currentBlockPosition, int currCol, int colToMoveTo)
  {
    if (colToMoveTo > currCol)
    {
      BlockData nextColBlockData = GetMovableBlockData(currentBlockPosition, currCol + 1);
      if (nextColBlockData == null)
      {
        return GetMovableBlockData(currentBlockPosition, currCol);
      }
      BlockData blockData = GetMovableBlockData(currentBlockPosition, colToMoveTo);
      if (blockData != null)
      {
        return blockData;
      }
    }
    else if (colToMoveTo < currCol)
    {
      BlockData prevColBlockData = GetMovableBlockData(currentBlockPosition, currCol - 1);
      if (prevColBlockData == null)
      {
        return GetMovableBlockData(currentBlockPosition, currCol);
      }
      BlockData blockData = GetMovableBlockData(currentBlockPosition, colToMoveTo);
      if (blockData != null)
      {
        return blockData;
      }
    }
      return GetMovableBlockData(currentBlockPosition, currCol);
  }

  internal IEnumerator MoveAllBlocksDown()
  {
    for (int i = 0; i < BlockGrid.Count; i++)
    {
      for (int j = BlockGrid[i].Column.Count - 1; j >= 0; j--)
      {
        if (BlockGrid[i].Column[j].value != 0)
        {
          BlockData blockData = GetMovableBlockData(i, j);
          if (blockData != null)
          {
            Block BlockToMove = BlockList.Find(x => x.GridPos == new Vector2Int(i, j));
            if (BlockToMove == null)
            {
              Debug.LogError("Block to move is null.");
              continue;
            }
            Vector2Int currPos = BlockToMove.GridPos;
            // Debug.Log("Moving block at: " + BlockToMove.GridPos + " to: " + blockData.gridPosition);
            yield return BoardManager.Instance.DropBlockInstantly(BlockToMove, blockData, BoardManager.Instance.FastDropSpeed);
            RemoveBlockFromGrid(currPos);
          }
        }
      }
    }
  }

  internal void CheckBlocksForMerge()
  {
    HashSet<int> affectedColumn = new();
    foreach (MergeData data in BoardManager.Instance.MergeData)
    { 
      switch (data.Direction)
      {
        case MergeDirection.Left:
          affectedColumn.Add(data.TargetBlock.gridPosition.x);
          affectedColumn.Add(data.LeftBlock.gridPosition.x);
          break;

        case MergeDirection.Right:
          affectedColumn.Add(data.TargetBlock.gridPosition.x);
          affectedColumn.Add(data.RightBlock.gridPosition.x);
          break;

        case MergeDirection.Bottom:
          affectedColumn.Add(data.TargetBlock.gridPosition.x);
          break;

        case MergeDirection.LeftRight:
          affectedColumn.Add(data.TargetBlock.gridPosition.x);
          affectedColumn.Add(data.LeftBlock.gridPosition.x);
          affectedColumn.Add(data.RightBlock.gridPosition.x);
          break;

        case MergeDirection.LeftBottom:
          affectedColumn.Add(data.TargetBlock.gridPosition.x);
          affectedColumn.Add(data.LeftBlock.gridPosition.x);
          break;

        case MergeDirection.RightBottom:
          affectedColumn.Add(data.TargetBlock.gridPosition.x);
          affectedColumn.Add(data.RightBlock.gridPosition.x);
          break;

        case MergeDirection.LeftRightBottom:
          affectedColumn.Add(data.TargetBlock.gridPosition.x);
          affectedColumn.Add(data.LeftBlock.gridPosition.x);
          affectedColumn.Add(data.RightBlock.gridPosition.x);
          break;
      }
    }

    BoardManager.Instance.MergeData.Clear();
    if (affectedColumn.Count > 0)
    {  
      foreach (int col in affectedColumn)
      {
        for (int row = 0; row < height; row++)
        {
          Vector2Int POI = new(col, row);
          if (CheckBlockForMerge(POI, out MergeData data))
          {
            BoardManager.Instance.MergeData.Add(data);
            break;
          }
        }
      }
    }
  }

  internal bool CheckBlockForMerge(Vector2Int POI, out MergeData mergeData)
  {
    mergeData = new MergeData();
    if (POI == null || !IsValidPosition(POI) || IsEmpty(POI))
    {
      // Debug.LogError("POI is not set or invalid.");
      mergeData = null;
      return false;
    }
    BlockData targetBlock = GetBlockData(POI);
    if (targetBlock !=null && CheckBlockAleradyInMerge(targetBlock))
    {
      Debug.Log("Block already in merge: " + targetBlock.gridPosition);
      mergeData = null;
      return false;
    }

    BlockData leftBlock = GetBlockData(new Vector2Int(POI.x - 1, POI.y));
    if (leftBlock != null && CheckBlockAleradyInMerge(leftBlock))
    {
      leftBlock = null;
    }

    BlockData rightBlock = GetBlockData(new Vector2Int(POI.x + 1, POI.y));
    if(rightBlock != null && CheckBlockAleradyInMerge(rightBlock))
    {
      rightBlock = null;
    }

    BlockData bottomBlock = GetBlockData(new Vector2Int(POI.x, POI.y + 1));
    if (bottomBlock != null && CheckBlockAleradyInMerge(bottomBlock))
    {
      bottomBlock = null;
    }

    if (targetBlock?.value == leftBlock?.value && leftBlock?.value == rightBlock?.value && rightBlock?.value == bottomBlock?.value)
    {
      mergeData.LeftBlock = leftBlock;
      mergeData.RightBlock = rightBlock;
      mergeData.BottomBlock = bottomBlock;
      mergeData.TargetBlock = targetBlock;
      mergeData.Direction = MergeDirection.LeftRightBottom;
      return true;
    }
    else if (targetBlock?.value == leftBlock?.value && leftBlock?.value == rightBlock?.value)
    {
      mergeData.LeftBlock = leftBlock;
      mergeData.RightBlock = rightBlock;
      mergeData.TargetBlock = targetBlock;
      mergeData.Direction = MergeDirection.LeftRight;
      return true;
    }
    else if (targetBlock?.value == leftBlock?.value && leftBlock?.value == bottomBlock?.value)
    {
      mergeData.LeftBlock = leftBlock;
      mergeData.BottomBlock = bottomBlock;
      mergeData.TargetBlock = targetBlock;
      mergeData.Direction = MergeDirection.LeftBottom;
      return true;
    }
    else if (targetBlock?.value == rightBlock?.value && rightBlock?.value == bottomBlock?.value)
    {
      mergeData.RightBlock = rightBlock;
      mergeData.BottomBlock = bottomBlock;
      mergeData.TargetBlock = targetBlock;
      mergeData.Direction = MergeDirection.RightBottom;
      return true;
    }
    else if (targetBlock?.value == leftBlock?.value)
    {
      mergeData.LeftBlock = leftBlock;
      mergeData.TargetBlock = targetBlock;
      mergeData.Direction = MergeDirection.Left;
      return true;
    }
    else if (targetBlock?.value == rightBlock?.value)
    {
      mergeData.RightBlock = rightBlock;
      mergeData.TargetBlock = targetBlock;
      mergeData.Direction = MergeDirection.Right;
      return true;
    }
    else if (targetBlock?.value == bottomBlock?.value)
    {
      mergeData.BottomBlock = bottomBlock;
      mergeData.TargetBlock = targetBlock;
      mergeData.Direction = MergeDirection.Bottom;
      return true;
    }
    else
    {
      mergeData = null;
      return false;
    }
  }


  bool CheckBlockAleradyInMerge(BlockData blockData)
  {
    foreach (MergeData data in BoardManager.Instance.MergeData)
    {
      if (data.TargetBlock == blockData)
      {
        return true;
      }
      if(data.LeftBlock != null && data.LeftBlock == blockData)
      {
        return true;
      }
      if(data.RightBlock != null && data.RightBlock == blockData)
      {
        return true;
      }
      if(data.BottomBlock != null && data.BottomBlock == blockData)
      {
        return true;
      }
    }
    return false;
  }

  internal void SetBlockDataOnTheGrid(Block block, Vector2Int position)
  {
    if (IsValidPosition(position))
    {
      BlockGrid[position.x].Column[position.y].value = block.Value;
      block.SetGridPosition(position);
    }
    else
    {
      Debug.LogError("Invalid position while setting block data.");
    }
  }

  void RemoveBlockFromGrid(Vector2Int position)
  {
    if (IsValidPosition(position))
    {
      // Debug.Log("Removing block value from grid at: " + position);
      BlockGrid[position.x].Column[position.y].value = 0;
    }
  }

  internal void RemoveBlock(Vector2Int position)
  {
    if (IsValidPosition(position))
    {
      BlockGrid[position.x].Column[position.y].value = 0;
      Block block = BlockList.Find(b => b.GridPos == position);
      if (block == null)
      {
        Debug.LogError("Block to remove is null at position: " + position);
        return;
      }
      BlockList.Remove(block);
      Destroy(block.gameObject);
    }
    else
    {
      Debug.LogError("Invalid position while removing block.");
    }
  }

  internal bool IsEmpty(Vector2Int position)
  {
    if (IsValidPosition(position))
    {
      return BlockGrid[position.x].Column[position.y].value == 0;
    }
    else
    {
      // Debug.LogError("Invalid position while checking if empty.");
      return true;
    }
  }

  internal bool IsValidPosition(Vector2Int position)
  {
    return position.x >= 0 && position.x < width && position.y >= 0 && position.y < height;
  }

  internal bool CheckGameEnd()
  {
    for (int i = 0; i < BlockGrid.Count; i++)
    {
      if (!IsEmpty(new(i, 0)))
      {
        return true;
      }
    }
    return false;
  }
}

[Serializable]
public class BoardBlocks
{
  public List<BlockData> Column = new();
}

[Serializable]
public class BlockData
{
  public int value;
  public Transform boardPosition;
  public Vector2Int gridPosition;
}
public enum MergeDirection
{
  Left,
  Right,
  Bottom,
  LeftRight,
  LeftBottom,
  RightBottom,
  LeftRightBottom,
  None,
}

[Serializable]
public class MergeData
{
  public BlockData LeftBlock;
  public BlockData RightBlock;
  public BlockData BottomBlock;
  public BlockData TargetBlock;
  public MergeDirection Direction;
}

