using UnityEngine;

public class GridManager : MonoBehaviour
{
  public static GridManager Instance;
  [SerializeField] private int width = 5, height = 8;
  private Block[,] grid;

  private void Awake()
  {
    Instance = this;
    grid = new Block[width, height];
  }

  internal bool IsValidPosition(Vector2Int position)
  {
    return position.x >= 0 && position.x < width && position.y >= 0 && position.y < height;
  }

  internal bool IsEmpty(Vector2Int position)
  {
    return grid[position.x, position.y] == null;
  }

  internal Block GetBlock(Vector2Int position)
  {
    if (IsValidPosition(position))
    {
      return grid[position.x, position.y];
    }
    else
    {
      Debug.LogError("Invalid position while getting block.");
      return null;
    }
  }

  internal void PlaceBlock(Block block, Vector2Int position)
  {
    if (IsValidPosition(position) && IsEmpty(position))
    {
      grid[position.x, position.y] = block;
      block.transform.position = new Vector3(position.x, position.y, 0);
    }
    else
    {
      Debug.LogError("Invalid position or block already exists while placing block.");
    }
  }

  public void RemoveBlock(Vector2Int position)
  {
    if (IsValidPosition(position))
    {
      grid[position.x, position.y] = null;
    }
    else
    {
      Debug.LogError("Invalid position while removing block.");
    }
  }
}
