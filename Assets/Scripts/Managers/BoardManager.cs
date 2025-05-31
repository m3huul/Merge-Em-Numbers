using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Collections;
using UnityEngine.UI;

public class BoardManager : MonoBehaviour
{
  public static BoardManager Instance;
  [SerializeField] internal List<int> BlockNumbers = new();
  [SerializeField] internal List<Color> BlockColors = new();
  [SerializeField] internal List<MergeData> MergeData;
  [SerializeField] internal float basePullDownSpeed = 3f;
  [SerializeField] internal float fastPullDownSpeed = 0.05f;
  [SerializeField] private float fallDuration = 0.05f;
  [SerializeField] private Button RestartButton;
  internal int FocusedColumnIndex = 2;
  private Tween CurrBlockTween;
  void Awake()
  {
    if (Instance == null)
    {
      Instance = this;
    }

    RestartButton.onClick.AddListener(() =>
    {
      RestartGame();
    });

    Time.timeScale = 1f;
    Application.targetFrameRate = -1;
    ExpandNumAndColorList(32);
  }

  internal void KillGame()
  {
    Debug.Log("No Block To Move To. Game End");
    InputManager.Instance.enabled = false;
    StopAllCoroutines();
    if (CurrBlockTween != null)
    {
      CurrBlockTween.Kill();
      CurrBlockTween = null;
    }
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

  int GenerateNextNumber()
  {
    int lastNumber = BlockNumbers[BlockNumbers.Count - 1];
    return lastNumber * 2;
  }

  internal int ExpandNumAndColorList(int value)
  {
    while (!BlockNumbers.Contains(value))
    {
      int newValue = GenerateNextNumber();
      BlockColors.Add(GetColorForValue(newValue));
      BlockNumbers.Add(newValue);
    }
    return BlockNumbers.IndexOf(value);
  }

  internal Color GetColorForValue(int value)
  {
    int index = BlockNumbers.IndexOf(value);
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
    float hue = BlockNumbers.Count * 0.15f % 1;
    return Color.HSVToRGB(hue, 0.8f, 1.0f);
  }

  internal IEnumerator PullTheBlock(Block BlockToPull, BlockData MoveToBlock, float duration)
  {
    if (MoveToBlock == null)
    {
      Debug.Log("Game Over - No available space!");
      yield break;
    }
    // Debug.Log("Pulling block to: " + MoveToBlock.gridPosition.ToString());
    CurrBlockTween?.Kill();
    CurrBlockTween = BlockToPull.transform.DOMoveY(MoveToBlock.boardPosition.position.y, duration)
    .OnComplete(() =>
    {
      InputManager.Instance.BlockTransform = null;
      InputManager.Instance.enabled = false;
      // Debug.Log("Block Dropped at : " + MoveToBlock.gridPosition.ToString());
      BlockToPull.transform.position = MoveToBlock.boardPosition.position;
      GridManager.Instance.SetBlockDataOnTheGrid(BlockToPull, MoveToBlock.gridPosition);
      if (GridManager.Instance.CheckBlockForMerge(MoveToBlock.gridPosition, out MergeData data))
      {
        MergeData.Add(data);
        StartCascade();
      }
      else
      {
        MergeData = new();
        SpawnManager.Instance.SpawnNextBlock();
      }
    })
    .SetEase(Ease.Linear);
  }

  internal IEnumerator PullTheBlockDown(Block BlockToPull, BlockData BlockMoveTo, float duration)
  {
    yield return BlockToPull.transform.DOLocalMoveY(BlockMoveTo.boardPosition.localPosition.y, duration).SetEase(Ease.Linear).WaitForCompletion();
    GridManager.Instance.SetBlockDataOnTheGrid(BlockToPull, BlockMoveTo.gridPosition);
  }

  public void StartCascade()
  {
    StartCoroutine(CascadeRoutine());
  }

  private IEnumerator CascadeRoutine()
  {
    while (MergeData.Count > 0)
    {
      yield return new WaitForSecondsRealtime(fallDuration);
      yield return Merge();
      yield return GridManager.Instance.MoveAllBlocksDown();
      GridManager.Instance.CheckBlocksForMerge();
    }
    SpawnManager.Instance.SpawnNextBlock();
  }

  IEnumerator Merge()
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
          Debug.Log("Merging Left : " + targetBlock.GridPos + " with " + leftBlock.GridPos);
          yield return targetBlock.Merge(leftBlock);
          break;

        case MergeDirection.Right:
          Debug.Log("Merging Right : " + targetBlock.GridPos + " with " + rightBlock.GridPos);
          yield return targetBlock.Merge(rightBlock);
          break;

        case MergeDirection.Bottom:
          Debug.Log("Merging Bottom : " + targetBlock.GridPos + " with " + bottomBlock.GridPos);
          yield return targetBlock.Merge(bottomBlock);
          break;

        case MergeDirection.LeftRight:
          Debug.Log("Merging Left and Right : " + targetBlock.GridPos + " with " + leftBlock.GridPos + " and " + rightBlock.GridPos);
          yield return targetBlock.Merge(leftBlock, rightBlock);
          break;

        case MergeDirection.LeftBottom:
          Debug.Log("Merging Left and Bottom : " + targetBlock.GridPos + " with " + leftBlock.GridPos + " and " + bottomBlock.GridPos);
          yield return targetBlock.Merge(leftBlock, bottomBlock);
          break;

        case MergeDirection.RightBottom:
          Debug.Log("Merging Right and Bottom : " + targetBlock.GridPos + " with " + rightBlock.GridPos + " and " + bottomBlock.GridPos);
          yield return targetBlock.Merge(rightBlock, bottomBlock);
          break;

        case MergeDirection.LeftRightBottom:
          Debug.Log("Merging Left, Right and Bottom : " + targetBlock.GridPos + " with " + leftBlock.GridPos + ", " + rightBlock.GridPos + " and " + bottomBlock.GridPos);
          yield return targetBlock.Merge(leftBlock, rightBlock, bottomBlock);
          break;
      }
    }
  }
}

