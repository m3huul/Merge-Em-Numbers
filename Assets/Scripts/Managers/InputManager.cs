using UnityEngine;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
  public static InputManager Instance;

  [Header("Current Input State")]
  [SerializeField] internal Transform BlockTransform;
  [SerializeField] internal int CurrentColumn = 2;

  [Header("Drop Prediction")]
  [SerializeField] private BlockData BlockToMoveData;

  void Awake()
  {
    if (Instance == null)
    {
      Instance = this;
    }
  }

  #region Input Events

  public void OnPointerDown(PointerEventData eventData)
  {
    OnUserInput(eventData);
  }

  public void OnDrag(PointerEventData eventData)
  {
    OnUserInput(eventData);
  }

  public void OnPointerUp(PointerEventData eventData)
  {
    if (BlockTransform != null && BlockToMoveData != null)
    {
      BoardManager.Instance.CurrColumn = CurrentColumn;
      Block blockComponent = BlockTransform.GetComponent<Block>();
      StartCoroutine(BoardManager.Instance.DropBlock(blockComponent, BlockToMoveData, BoardManager.Instance.FastDropSpeed));
      BlockTransform = null;
    }
  }

  #endregion

  #region Input Logic

  internal void SetActiveBlock(Transform blockTransform)
  {
    BlockTransform = blockTransform;
    TrySetDropPrediction(CurrentColumn, CurrentColumn, true);
  }

  void TrySetDropPrediction(int currCol, int targetCol, bool init = false)
  {
    BlockToMoveData = GridManager.Instance.GetDropTarget(BlockTransform, currCol, targetCol);

    if (BlockToMoveData == null)
    {
      if (init)
      {
        Debug.Log("No Block To Move To");
        BoardManager.Instance.EndGame();
      }
      else
      {
        Debug.LogError("No Block To Move To. Column Index: " + currCol + " to " + targetCol);
      }
      return;
    }

    float duration = CalculateDuration(BlockToMoveData?.boardPosition, BoardManager.Instance.BaseDropSpeed);
    StartCoroutine(BoardManager.Instance.DropBlock(BlockTransform.GetComponent<Block>(), BlockToMoveData, duration));
  }

  float CalculateDuration(Transform targetTransform, float speed)
  {
    float distance = Vector3.Distance(BlockTransform.position, targetTransform.position);
    return distance / speed;
  }

  private void OnUserInput(PointerEventData eventData)
  {
    if (BlockTransform != null)
    {
      Transform closestColumnPosition = GridManager.Instance.BlockGrid[FindClosestColumn(eventData.position.x)].Cells[0].boardPosition;

      // Move the block to the closest column's X position if found
      if (closestColumnPosition != null)
      {
        Vector3 targetPosition = BlockTransform.position;
        targetPosition.x = closestColumnPosition.position.x;
        BlockTransform.position = targetPosition;
      }
    }
  }

  private int FindClosestColumn(float xPosition)
  {
    float closestDistance = float.MaxValue;
    int closestIndex = 0;

    for (int i = 0; i < GridManager.Instance.BlockGrid.Count; i++)
    {
      float columnX = GridManager.Instance.BlockGrid[i].Cells[0].boardPosition.position.x;
      float distance = Mathf.Abs(xPosition - columnX);

      if (distance < closestDistance)
      {
        closestDistance = distance;
        closestIndex = i;
      }
    }

    if (CurrentColumn != closestIndex)
    {
      TrySetDropPrediction(CurrentColumn, closestIndex);
      if (BlockToMoveData != null)
        CurrentColumn = BlockToMoveData.gridPosition.x;
    }

    return CurrentColumn;
  }

  #endregion
}
