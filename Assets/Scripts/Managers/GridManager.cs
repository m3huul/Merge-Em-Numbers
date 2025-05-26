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
            Vector2Int currPos = BlockToMove.GridPos;
            if (BlockToMove == null)
            {
              Debug.LogError("Block to move is null.");
              continue;
            }
            Debug.Log("Moving block at: " + BlockToMove.GridPos + " to: " + blockData.gridPosition);
            yield return BoardManager.Instance.PullTheBlockDown(BlockToMove, blockData, InputManager.Instance.CalculateDuration(blockData.boardPosition, BoardManager.Instance.fastPullDownSpeed, BlockToMove.transform));
            print("Called PullTheBlock");
            RemoveBlockFromGrid(currPos);
            // yield return new WaitForSeconds(0.05f);
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
      print("Removing block value from grid at: " + position);
      BlockGrid[position.x].Column[position.y].value = 0;
    }
  }

  internal void RemoveBlock(Vector2Int position)
  {
    if (IsValidPosition(position))
    {
      BlockGrid[position.x].Column[position.y].value = 0;
      foreach (var block in BlockList)
      {
        if (block.GridPos == position)
        {
          BlockList.Remove(block);
          Destroy(block.gameObject);
          break;
        }
      }
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

  // internal Block GetBlock(Vector2Int position)
  // {
  //   if (IsValidPosition(position))
  //   {
  //     return grid[position.x, position.y];
  //   }
  //   else
  //   {
  //     Debug.LogError("Invalid position while getting block.");
  //     return null;
  //   }
  // }

  // internal void PlaceBlock(Block block, Vector2Int position)
  // {
  //   if (IsValidPosition(position) && IsEmpty(position))
  //   {
  //     grid[position.x, position.y] = block;
  //     block.transform.position = new Vector3(position.x, position.y, 0);
  //   }
  //   else
  //   {
  //     Debug.LogError("Invalid position or block already exists while placing block.");
  //   }
  // }

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
