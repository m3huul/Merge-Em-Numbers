using UnityEngine;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
  public static InputManager Instance;
  [SerializeField] internal int IColumnIndex = 2;
  [SerializeField] internal Transform BlockTransform;
  private BlockData BlockToMoveData;

  void Awake()
  {
    if (Instance == null)
    {
      Instance = this;
    }
  }
  internal void setBlock(Transform blockTransform)
  {
    BlockTransform = blockTransform;

    BlockToMoveData = GridManager.Instance.GetPullableBlockData(BlockTransform, IColumnIndex, IColumnIndex);

    if (BlockToMoveData == null)
    {
      Debug.Log("No Block To Move To. Game End");
      return;
    }

    StartCoroutine(BoardManager.Instance.PullTheBlock(BlockTransform.GetComponent<Block>(), BlockToMoveData, BlockToMoveData.gridPosition.y * BoardManager.Instance.basePullDownSpeed));
  }

  public void OnPointerDown(PointerEventData eventData)
  {
    OnUserInput(eventData);
  }

  public void OnPointerUp(PointerEventData eventData)
  {
    if (BlockTransform)
    {
      BoardManager.Instance.FocusedColumnIndex = IColumnIndex;
      StartCoroutine(BoardManager.Instance.PullTheBlock(BlockTransform.GetComponent<Block>(), BlockToMoveData, BlockToMoveData.gridPosition.y * BoardManager.Instance.fastPullDownSpeed));
      BlockTransform = null;
    }
  }

  public void OnDrag(PointerEventData eventData)
  {
    OnUserInput(eventData);
  }

  private void OnUserInput(PointerEventData eventData)
  {
    if (BlockTransform != null)
    {
      Transform closestColumnPosition = GridManager.Instance.BlockGrid[FindClosestColumnIndex(eventData.position.x)].Column[0].boardPosition;

      // Move the block to the closest column's X position if found
      if (closestColumnPosition != null)
      {
        Vector3 targetPosition = BlockTransform.position;
        targetPosition.x = closestColumnPosition.position.x;
        BlockTransform.position = targetPosition;
      }
    }
  }

  private int FindClosestColumnIndex(float xPosition)
  {
    float closestDistance = float.MaxValue;
    int closestIndex = 0;

    for (int i = 0; i < GridManager.Instance.BlockGrid.Count; i++)
    {
      float columnX = GridManager.Instance.BlockGrid[i].Column[0].boardPosition.position.x;
      float distance = Mathf.Abs(xPosition - columnX);

      if (distance < closestDistance)
      {
        closestDistance = distance;
        closestIndex = i;
      }
    }


    if(closestIndex == IColumnIndex)
    {
      return closestIndex;
    }

    Debug.Log("Column Index Changed");

    BlockToMoveData = GridManager.Instance.GetPullableBlockData(BlockTransform, IColumnIndex, closestIndex);
    if (BlockToMoveData == null)
    {
      Debug.Log("No Block To Move To. Game End");
      return IColumnIndex;
    }
    BoardManager.Instance.PullTheBlock(BlockTransform.GetComponent<Block>(), BlockToMoveData, BlockToMoveData.gridPosition.y * BoardManager.Instance.basePullDownSpeed);
    IColumnIndex = BlockToMoveData.gridPosition.x;
    return BlockToMoveData.gridPosition.x;
  }
}
