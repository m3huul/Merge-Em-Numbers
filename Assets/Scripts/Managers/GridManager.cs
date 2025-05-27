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
      if (IsEmpty(new Vector2Int(col, i)) && currentBlockTransform.position.y >= BlockGrid[col].Column[i].boardPosition.position.y+EmptyDroppableOffset)
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
    else
    {
      return GetMovableBlockData(currentBlockPosition, currCol);
    }
    return null;
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
            Debug.Log("Moving block at: " + BlockToMove.GridPos + " to: " + blockData.gridPosition);
            yield return BoardManager.Instance.PullTheBlockDown(BlockToMove, blockData, BoardManager.Instance.fastPullDownSpeed);
            RemoveBlockFromGrid(currPos);
          }
        }
      }
    }
  }

  internal void SetBlockData(Block block, Vector2Int position, int value = -1)
  {
    if (IsValidPosition(position))
    {
      BlockGrid[position.x].Column[position.y].value = block.Value;
      block.SetGridPosition(position);

      if (value != -1)
      {
        if(!BoardManager.Instance.BlockNumbers.Contains(value))
          BoardManager.Instance.CheckAndExpandNumbers();
        block.SetValue(BoardManager.Instance.BlockNumbers.IndexOf(value));
      }
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
