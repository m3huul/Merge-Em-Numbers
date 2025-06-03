using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;

public class Block : MonoBehaviour
{
  [Header("Block Settings")]
  [SerializeField] private float SpawnYOffset = 110f;
  [SerializeField] internal int Value;

  internal Vector2Int GridPos;

  private Image BlockImage;
  private TMP_Text ValueText;

  private void Awake()
  {
    BlockImage = GetComponent<Image>();
    ValueText = transform.GetChild(0).GetComponent<TMP_Text>();
  }

  #region Initialization
  internal void Init(int index)
  {
    Vector3 initPosi = GridManager.Instance.BlockGrid[InputManager.Instance.CurrentColumn].Cells[0].boardPosition.position;
    Vector3 startPosi = initPosi + new Vector3(0, SpawnYOffset, 0);
    transform.position = startPosi;
    SetValue(index);
  }

  internal void SetValue(int index)
  {
    BlockImage.color = BoardManager.Instance.BlockColors[index];
    Value = BoardManager.Instance.BlockValues[index];
    ValueText.text = BoardManager.Instance.BlockValues[index].ToString();
  }

  internal void SetGridPosition(Vector2Int pos)
  {
    GridPos = pos;
  }

  #endregion

  #region Merge Methods

  internal IEnumerator Merge(Block targetBlock)
  {
    targetBlock.BlockImage.DOFade(0, 0.1f).SetEase(Ease.Linear);
    yield return targetBlock.transform.DOLocalMove(transform.localPosition, 0.1f).SetEase(Ease.Linear).WaitForCompletion();

    GridManager.Instance.RemoveBlock(targetBlock.GridPos);

    Value *= 2;
    GridManager.Instance.PlaceBlockOnGrid(this, GridPos);

    UpdateValueDisplay();
  }

  internal IEnumerator Merge(Block leftBlock, Block rightBlock)
  {
    leftBlock.BlockImage.DOFade(0, 0.1f).SetEase(Ease.Linear);
    rightBlock.BlockImage.DOFade(0, 0.1f).SetEase(Ease.Linear);
    leftBlock.transform.DOLocalMoveX(transform.localPosition.x, 0.1f);
    yield return rightBlock.transform.DOLocalMoveX(transform.localPosition.x, 0.1f).SetEase(Ease.Linear).WaitForCompletion();

    GridManager.Instance.RemoveBlock(leftBlock.GridPos);
    GridManager.Instance.RemoveBlock(rightBlock.GridPos);

    Value *= 4;
    GridManager.Instance.PlaceBlockOnGrid(this, GridPos);

    UpdateValueDisplay();
  }

  internal IEnumerator Merge(Block leftBlock, Block rightBlock, Block bottomBlock)
  {
    leftBlock.BlockImage.DOFade(0, 0.1f).SetEase(Ease.Linear);
    rightBlock.BlockImage.DOFade(0, 0.1f).SetEase(Ease.Linear);
    bottomBlock.BlockImage.DOFade(0, 0.1f).SetEase(Ease.Linear);

    leftBlock.transform.DOLocalMoveX(transform.localPosition.x, 0.1f);
    rightBlock.transform.DOLocalMoveX(transform.localPosition.x, 0.1f).SetEase(Ease.Linear).WaitForCompletion();
    yield return bottomBlock.transform.DOLocalMoveY(transform.localPosition.y, 0.1f).SetEase(Ease.Linear).WaitForCompletion();

    GridManager.Instance.RemoveBlock(leftBlock.GridPos);
    GridManager.Instance.RemoveBlock(rightBlock.GridPos);
    GridManager.Instance.RemoveBlock(bottomBlock.GridPos);

    Value *= 4;
    GridManager.Instance.PlaceBlockOnGrid(this, GridPos);

    UpdateValueDisplay();
  }

  void UpdateValueDisplay()
  {
    int ValueIndex = BoardManager.Instance.BlockValues.IndexOf(Value);
    if (ValueIndex == -1)
    {
      ValueIndex = BoardManager.Instance.ExpandNumAndColorList(Value);
    }
    SetValue(ValueIndex);
  }

  #endregion
}
