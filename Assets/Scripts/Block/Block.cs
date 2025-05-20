using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Block : MonoBehaviour
{
  [SerializeField] private int Value;
  private Vector2Int gridPos;
  private Image BlockImage;
  private TMP_Text BlockNumberText;
  [SerializeField] private float startPosiYOffset = 200f;

  public int value;
  public Transform boardPosition;

  public Block()
  {
  }

  private void Awake()
  {
    BlockImage = GetComponent<Image>();
    BlockNumberText = transform.GetChild(0).GetComponent<TMP_Text>();
  }

  internal void Init()
  {
    Vector3 initPosi = BoardManager.Instance.m_boardBlocks[InputManager.Instance.m_columnIndex].Column[0].boardPosition.position;
    Vector3 startPosi = initPosi + new Vector3(0, startPosiYOffset, 0);

    transform.position = startPosi;

    BlockImage.color = BoardManager.Instance.BlockColors[value];     // Value Calc here
    BlockNumberText.text = value.ToString();
  }

  internal void SetPredictedValue(int index)
  {
    BlockImage.color = BoardManager.Instance.BlockColors[index];
    BlockNumberText.text = BoardManager.Instance.BlockNumbers[index].ToString();
  }

  internal void SetGridPosition(Vector2Int pos)
  {
    gridPos = pos;
    transform.localPosition = new Vector3(pos.x, pos.y, 0);
  }
}
