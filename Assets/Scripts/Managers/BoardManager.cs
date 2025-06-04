using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class BoardManager : MonoBehaviour
{
  public static BoardManager Instance;

  [Header("Game Settings")]
  [SerializeField] internal List<int> BlockValues = new();
  [SerializeField] internal List<Color> BlockColors = new();
  [SerializeField] internal float BaseDropSpeed = 120f;
  [SerializeField] internal float FastDropSpeed = 0.1f;
  [SerializeField] private float CascadeDelay = 0.1f;

  [Header("Merge Data")]
  [SerializeField] internal List<MergeData> MergeData = new();

  [Header("UI Elements")]
  [SerializeField] private Button RestartButton;

  internal int CurrColumn = 2;

  private Tween CurrBlockTween;

  void Awake()
  {
    Application.targetFrameRate = 200;
    if (Instance == null) Instance = this;

    if (!RestartButton)
    {
      Debug.LogError("RestartButton is not assigned in the inspector.");
      return;
    }
    RestartButton.onClick.AddListener(RestartGame);


    if (BlockValues.Count <= 0 && BlockColors.Count <= 0)
    {
      Debug.LogError("BlockValues and BlockColors are not initialized. Please set them in the inspector.");
      return;
    }   
    ExpandNumAndColorList(32);
  }

  #region Game Flow

  internal void EndGame()
  {
    Debug.Log("No Block To Move To. Game End");

    InputManager.Instance.enabled = false;

    StopAllCoroutines();
    CurrBlockTween?.Kill();
    DOTween.KillAll();

    RestartButton.gameObject.SetActive(true);
  }

  void RestartGame()
  {
    Debug.Log("Restarting Game");

    RestartButton.gameObject.SetActive(false);
    GridManager.Instance.Reset();
    SpawnManager.Instance.Reset();
  }

  internal void StartCascade()
  {
    StartCoroutine(CascadeRoutine());
  }

  private IEnumerator CascadeRoutine()
  {
    while (MergeData.Count > 0)
    {
      yield return new WaitForSecondsRealtime(CascadeDelay);
      yield return ExecuteMerges();
      yield return GridManager.Instance.ApplyGravityToBlocks();
      GridManager.Instance.FindMergeableBlocks();
    }
    SpawnManager.Instance.SpawnNextBlock();
  }

  #endregion

  #region Block Movement Logic

  internal IEnumerator DropBlock(Block block, BlockData destination, float duration)
  {
    if (destination == null)
    {
      Debug.LogError("Game Over - No available space!");
      yield break;
    }

    CurrBlockTween?.Kill();
    CurrBlockTween = block.transform.DOLocalMoveY(destination.boardPosition.localPosition.y, duration).SetEase(Ease.Linear)
      .OnComplete(() =>
      {
        InputManager.Instance.BlockTransform = null;
        InputManager.Instance.enabled = false;
        GridManager.Instance.PlaceBlockOnGrid(block, destination.gridPosition);

        if (GridManager.Instance.TryGetMerge(destination.gridPosition, out MergeData data))
        {
          MergeData.Add(data);
          StartCascade();
        }
        else
        {
          MergeData = new();
          SpawnManager.Instance.SpawnNextBlock();
        }
      });
  }

  internal IEnumerator DropBlockInstantly(Block BlockToPull, BlockData BlockMoveTo, float duration)
  {
    yield return BlockToPull.transform.DOLocalMoveY(BlockMoveTo.boardPosition.localPosition.y, duration)
      .SetEase(Ease.Linear).WaitForCompletion();
    GridManager.Instance.PlaceBlockOnGrid(BlockToPull, BlockMoveTo.gridPosition);
  }

  #endregion

  #region Merge Logic  

  IEnumerator ExecuteMerges()
  {
    foreach (MergeData data in MergeData)
    {
      Block targetBlock = GridManager.Instance.BlockList.Find(b => b.GridPos == data.TargetBlock.gridPosition);
      Block leftBlock = GridManager.Instance.BlockList.Find(b => b.GridPos == data.LeftBlock?.gridPosition);
      Block rightBlock = GridManager.Instance.BlockList.Find(b => b.GridPos == data.RightBlock?.gridPosition);
      Block bottomBlock = GridManager.Instance.BlockList.Find(b => b.GridPos == data.BottomBlock?.gridPosition);
      switch (data.Direction)
      {
        case MergeDirection.Left:
          // Debug.Log("Merging Left : " + targetBlock.GridPos + " with " + leftBlock.GridPos);
          yield return targetBlock.Merge(leftBlock);
          break;

        case MergeDirection.Right:
          // Debug.Log("Merging Right : " + targetBlock.GridPos + " with " + rightBlock.GridPos);
          yield return targetBlock.Merge(rightBlock);
          break;

        case MergeDirection.Bottom:
          // Debug.Log("Merging Bottom : " + targetBlock.GridPos + " with " + bottomBlock.GridPos);
          yield return targetBlock.Merge(bottomBlock);
          break;

        case MergeDirection.LeftRight:
          // Debug.Log("Merging Left and Right : " + targetBlock.GridPos + " with " + leftBlock.GridPos + " and " + rightBlock.GridPos);
          yield return targetBlock.Merge(leftBlock, rightBlock);
          break;

        case MergeDirection.LeftBottom:
          // Debug.Log("Merging Left and Bottom : " + targetBlock.GridPos + " with " + leftBlock.GridPos + " and " + bottomBlock.GridPos);
          yield return targetBlock.Merge(leftBlock, bottomBlock);
          break;

        case MergeDirection.RightBottom:
          // Debug.Log("Merging Right and Bottom : " + targetBlock.GridPos + " with " + rightBlock.GridPos + " and " + bottomBlock.GridPos);
          yield return targetBlock.Merge(rightBlock, bottomBlock);
          break;

        case MergeDirection.LeftRightBottom:
          // Debug.Log("Merging Left, Right and Bottom : " + targetBlock.GridPos + " with " + leftBlock.GridPos + ", " + rightBlock.GridPos + " and " + bottomBlock.GridPos);
          yield return targetBlock.Merge(leftBlock, rightBlock, bottomBlock);
          break;
      }
    }
  }

  #endregion

  #region Color and Value Generation

  internal int ExpandNumAndColorList(int value)
  {
    while (!BlockValues.Contains(value))
    {
      int newValue = GenerateNextNumber();
      BlockColors.Add(GetColorForValue(newValue));
      BlockValues.Add(newValue);
    }
    return BlockValues.IndexOf(value);
  }

  Color GetColorForValue(int value)
  {
    int index = BlockValues.IndexOf(value);
    if (index != -1 && index < BlockColors.Count)
    {
      return BlockColors[index];
    }
    else
    {
      return GenerateNewColor();
    }
  }

  Color GenerateNewColor()
  {
    float hue = BlockValues.Count * 0.15f % 1;
    return Color.HSVToRGB(hue, 0.8f, 1.0f);
  }

  int GenerateNextNumber()
  {
    int lastNumber = BlockValues[BlockValues.Count - 1];
    return lastNumber * 2;
  }

  #endregion
}
