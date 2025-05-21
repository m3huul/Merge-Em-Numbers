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
      Debug.LogError("Invalid position while getting block data.");
      return null;
    }
  }

  internal BlockData GetMovableBlockData(int col)
  {
    for (int i = BlockGrid[col].Column.Count - 1; i >= 0; i--)
    {
      if (IsEmpty(new Vector2Int(col, i)))
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
            Debug.Log("Move All Blocks Down Coroutine: " + blockData.gridPosition);
            yield return FoundBlockToPull(blockData);
            yield return new WaitForSeconds(0.2f);
          }
        }
      }
    }
  }

  IEnumerator FoundBlockToPull(BlockData blockData)
  {
    foreach (var block in BlockList) {
      if(block.GridPos == blockData.gridPosition)
      {
        yield return BoardManager.Instance.PullTheBlock(block, blockData, BoardManager.Instance.fastPullDownSpeed);
      }
    }
  }

  internal void SetBlockData(Block block, Vector2Int position, int value =-1)
  {
    if (IsValidPosition(position))
    {
      BlockGrid[position.x].Column[position.y].value = block.Value;
      block.SetGridPosition(position);

      if(value != -1)
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

  internal void RemoveBlock(Vector2Int position)
  {
    if (IsValidPosition(position))
    {
      BlockGrid[position.x].Column[position.y].value = 0;
      foreach(var block in BlockList)
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
    return BlockGrid[position.x].Column[position.y].value == 0;
  }
  
  internal bool IsValidPosition(Vector2Int position)
  {
    return position.x >= 0 && position.x < width && position.y >= 0 && position.y < height;
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
