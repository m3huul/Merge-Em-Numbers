using UnityEngine;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
  public static InputManager Instance;
  [SerializeField] internal int IColumnIndex = 2;
  [SerializeField] private Transform BlockTransform;

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
  }

  public void OnPointerDown(PointerEventData eventData)
  {
    OnUserInput(eventData);
  }
  public void OnPointerUp(PointerEventData eventData)
  {
    if (BlockTransform)
    {
      StartCoroutine(BoardManager.Instance.PullTheBlock(BlockTransform.GetComponent<Block>(), BoardManager.Instance.fastPullDownSpeed));
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
    IColumnIndex = closestIndex;
    return closestIndex;
  }
}
