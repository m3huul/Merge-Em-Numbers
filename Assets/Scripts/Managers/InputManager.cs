using UnityEngine;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
  public static InputManager Instance;
  [SerializeField] internal int IColumnIndex = 2;
  [SerializeField] internal Transform BlockTransform;
  [SerializeField] private BlockData BlockToMoveData;

  void Awake()
  {
    if (Instance == null)
    {
      Instance = this;
    }
  }
  void BlockMovement(int currCol, int targetCol, bool init = false)
  {
    BlockToMoveData = GridManager.Instance.GetDroppableBlockData(BlockTransform, currCol, targetCol);

    if(BlockToMoveData == null)
    {
      if (init)
      {
        Debug.Log("No Block To Move To");
        BoardManager.Instance.KillGame();
      }
      else
      {
        Debug.LogError("No Block To Move To. Column Index: " + currCol + " to " + targetCol);
      }
      return;
    }

    float duration = CalculateDuration(BlockToMoveData?.boardPosition, BoardManager.Instance.basePullDownSpeed);
    StartCoroutine(BoardManager.Instance.PullTheBlock(BlockTransform.GetComponent<Block>(), BlockToMoveData, duration));
  }
  internal void setBlock(Transform blockTransform)
  {
    BlockTransform = blockTransform;

    BlockMovement(IColumnIndex, IColumnIndex, true);
  }

  public void OnPointerDown(PointerEventData eventData)
  {
    OnUserInput(eventData);
  }

  public void OnPointerUp(PointerEventData eventData)
  {
    if (BlockTransform != null && BlockToMoveData != null)
    {
      BoardManager.Instance.FocusedColumnIndex = IColumnIndex;
      Block blockComponent = BlockTransform.GetComponent<Block>();
      StartCoroutine(BoardManager.Instance.PullTheBlock(blockComponent, BlockToMoveData, BoardManager.Instance.fastPullDownSpeed));
      BlockTransform = null;
    }
  }

  internal float CalculateDuration(Transform targetTransform, float speed)
  {
    float distance = Vector3.Distance(BlockTransform.position, targetTransform.position);
    return distance / speed;
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

    if (closestIndex == IColumnIndex)
    {
      return closestIndex;
    }

    // Debug.Log("Column Index Changed");

    BlockMovement(IColumnIndex, closestIndex);
    if (BlockToMoveData == null)
    {
      return IColumnIndex;
    }

    IColumnIndex = BlockToMoveData.gridPosition.x;
    return BlockToMoveData.gridPosition.x;
  }
}
