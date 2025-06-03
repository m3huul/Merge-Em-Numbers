using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
  public static GridManager Instance;

  [Header("Grid Settings")]
  [SerializeField] private int GridWidth = 5;
  [SerializeField] private int GridHeight = 8;
  [SerializeField] private Transform GridParent;

  [Header("Grid Data")]
  [SerializeField] internal List<BoardColumn> BlockGrid; //List of all block GO's in the grid  Col, Row
  [SerializeField] internal List<Block> BlockList; //List of all block GO's in the grid

  private void Awake()
  {
    Instance = this;
    if (!GridParent)
    {
      Debug.LogError("GridParent is not assigned in the inspector.");
      return;
    }

    InitializeGrid();
  }

  private void InitializeGrid()
  {
    BlockGrid = new();
    for (int x = 0; x < GridWidth; x++)
    {
      var column = new BoardColumn();
      for (int y = 0; y < GridHeight; y++)
      {
        var data = new BlockData
        {
          boardPosition = GridParent.GetChild(x).GetChild(y),
          gridPosition = new Vector2Int(x, y)
        };
        column.Cells.Add(data);
      }
      BlockGrid.Add(column);
    }
  }

  #region Grid State

  internal void Reset()
  {
    SpawnManager.Instance.ReturnAllItemsToPool();
    BlockList.Clear();
    foreach (var column in BlockGrid)
    {
      foreach (var cell in column.Cells)
      {
        cell.value = 0;
      }
    }
  }

  internal bool IsEmpty(Vector2Int pos) => IsValid(pos) && GetBlockData(pos).value == 0;

  internal bool IsValid(Vector2Int pos) => pos.x >= 0 && pos.x < GridWidth && pos.y >= 0 && pos.y < GridHeight;

  internal BlockData GetBlockData(Vector2Int pos) => IsValid(pos) ? BlockGrid[pos.x].Cells[pos.y] : null;

  internal void PlaceBlockOnGrid(Block block, Vector2Int pos)
  {
    if (!IsValid(pos))
    {
      Debug.LogError("Invalid position while setting block data.");
      return;
    }

    BlockGrid[pos.x].Cells[pos.y].value = block.Value;
    block.SetGridPosition(pos);
  }

  internal void RemoveBlock(Vector2Int position)
  {
    if (!IsValid(position))
      return;

    BlockGrid[position.x].Cells[position.y].value = 0;
    Block block = BlockList.Find(b => b.GridPos == position);
    if (block == null)
    {
      Debug.LogError("Block to remove is null at position: " + position);
      return;
    }
    BlockList.Remove(block);
    SpawnManager.Instance.ReturnToPool(block);
  }

  void ClearGridPosition(Vector2Int pos)
  {
    if (IsValid(pos))
      BlockGrid[pos.x].Cells[pos.y].value = 0;
  }

  internal bool IsGameOver()
  {
    for (int i = 0; i < BlockGrid.Count; i++)
      if (!IsEmpty(new(i, 0)))
        return true;

    return false;
  }

  #endregion

  #region Drop And Merge Logic

  internal IEnumerator ApplyGravityToBlocks()
  {
    for (int col = 0; col < BlockGrid.Count; col++)
    {
      for (int row = BlockGrid[col].Cells.Count - 1; row >= 0; row--)
      {
        if (BlockGrid[col].Cells[row].value != 0)
        {
          BlockData dropTarget = GetFirstEmptyBelow(col, row);
          if (dropTarget != null)
          {
            Block block = BlockList.Find(x => x.GridPos == new Vector2Int(col, row));
            if (block == null)
            {
              Debug.LogError("Block to move is null.");
              continue;
            }

            // Debug.Log("Moving block at: " + BlockToMove.GridPos + " to: " + blockData.gridPosition);
            Vector2Int currPos = block.GridPos;
            yield return BoardManager.Instance.DropBlockInstantly(block, dropTarget, BoardManager.Instance.FastDropSpeed);
            ClearGridPosition(currPos);
          }
        }
      }
    }
  }

  internal BlockData GetFirstEmptyBelow(int col, int row)
  {
    for (int i = row; i < GridHeight; i++)
    {
      if (IsEmpty(new Vector2Int(col, i)))
      {
        return BlockGrid[col].Cells[i];
      }
    }
    return null;
  }

  internal BlockData GetDropTarget(Transform fallingBlock, int currCol, int targetCol)
  {
    if (targetCol != currCol)
    {
      if (targetCol > currCol)
      {
        var sideCheck = GetDroppableInColumn(fallingBlock, currCol + 1);
        if (sideCheck != null) return GetDroppableInColumn(fallingBlock, targetCol);
      }
      else
      {
        var sideCheck = GetDroppableInColumn(fallingBlock, currCol - 1);
        if (sideCheck != null) return GetDroppableInColumn(fallingBlock, targetCol);
      }
    }
    return GetDroppableInColumn(fallingBlock, currCol);
  }

  internal BlockData GetDroppableInColumn(Transform block, int col)
  {
    for (int row = BlockGrid[col].Cells.Count - 1; row >= 0; row--)
    {
      Vector2Int pos = new(col, row);
      if (IsEmpty(pos) && block.position.y >= GetBlockData(pos).boardPosition.position.y)
        return GetBlockData(pos);
    }
    return null;
  }

  #endregion

  #region Merge Checks

  internal void FindMergeableBlocks()
  {
    HashSet<int> affectedColumn = new();
    foreach (MergeData data in BoardManager.Instance.MergeData)
    {
      affectedColumn.Add(data.TargetBlock.gridPosition.x);
      if (data.LeftBlock != null)
        affectedColumn.Add(data.LeftBlock.gridPosition.x);
      if (data.RightBlock != null)
        affectedColumn.Add(data.RightBlock.gridPosition.x);
      if (data.BottomBlock != null)
        affectedColumn.Add(data.BottomBlock.gridPosition.x);
    }

    BoardManager.Instance.MergeData.Clear();

    foreach (int col in affectedColumn)
    {
      for (int row = 0; row < GridHeight; row++)
      {
        Vector2Int POI = new(col, row);
        if (TryGetMerge(POI, out MergeData data))
        {
          BoardManager.Instance.MergeData.Add(data);
          break;
        }
      }
    }
  }

  internal bool TryGetMerge(Vector2Int POI, out MergeData result)
  {
    result = null;

    if (!IsValid(POI) || IsEmpty(POI))
      return false;

    BlockData center = GetBlockData(POI);
    if (IsPartOfOngoingMerge(center))
      return false;

    BlockData left = GetBlockData(new Vector2Int(POI.x - 1, POI.y));
    BlockData right = GetBlockData(new Vector2Int(POI.x + 1, POI.y));
    BlockData below = GetBlockData(new Vector2Int(POI.x, POI.y + 1));

    if (left != null && IsPartOfOngoingMerge(left))
      left = null;

    if (right != null && IsPartOfOngoingMerge(right))
      right = null;

    if (below != null && IsPartOfOngoingMerge(below))
      below = null;

    bool l = left?.value == center.value;
    bool r = right?.value == center.value;
    bool b = below?.value == center.value;

    if (l && r && b)
      result = new MergeData(center, left, right, below, MergeDirection.LeftRightBottom);
    else if (l && r)
      result = new MergeData(center, left, right, null, MergeDirection.LeftRight);
    else if (l && b)
      result = new MergeData(center, left, null, below, MergeDirection.LeftBottom);
    else if (r && b)
      result = new MergeData(center, null, right, below, MergeDirection.RightBottom);
    else if (l)
      result = new MergeData(center, left, null, null, MergeDirection.Left);
    else if (r)
      result = new MergeData(center, null, right, null, MergeDirection.Right);
    else if (b)
      result = new MergeData(center, null, null, below, MergeDirection.Bottom);

    return result != null;
  }

  bool IsPartOfOngoingMerge(BlockData data)
  {
    foreach (var merge in BoardManager.Instance.MergeData)
    {
      if (merge.TargetBlock == data || merge.LeftBlock == data || merge.RightBlock == data || merge.BottomBlock == data)
      {
        Debug.LogWarning("Block is part of an ongoing merge: " + data.gridPosition);
        return true;
      }
    }
    Debug.Log("Block is not part of an ongoing merge: " + data.gridPosition);
    return false;
  }

  #endregion
}

[Serializable]
public class BoardColumn
{
  public List<BlockData> Cells = new();
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
  LeftRightBottom
}

[Serializable]
public class MergeData
{
  public BlockData LeftBlock;
  public BlockData RightBlock;
  public BlockData BottomBlock;
  public BlockData TargetBlock;
  public MergeDirection Direction;
  public MergeData(BlockData target, BlockData left, BlockData right, BlockData bottom, MergeDirection direction)
  {
    TargetBlock = target;
    LeftBlock = left;
    RightBlock = right;
    BottomBlock = bottom;
    Direction = direction;
  }
}

