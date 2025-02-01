using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements.Experimental;

public class BlockView : MonoBehaviour
{
    [SerializeField] internal Block blockData = new();
    [SerializeField] internal int row;
    [SerializeField] internal int column;
    private TMP_Text m_numText;
    internal Image m_image;
    internal bool Landed = false;

    void Awake()
    {
        m_numText = transform.GetChild(0).GetComponent<TMP_Text>();
        m_image = GetComponent<Image>();
    }

    internal void initBlockView()
    {
        Vector3 initPosi = BoardManager.Instance.m_boardBlocks[InputManager.Instance.m_columnIndex].Column[0].boardPosition.position;
        Vector3 StartPosi = initPosi + new Vector3(0, 200, 0);

        transform.position = StartPosi;
        blockData.boardPosition = transform;

        //Value Calc here
        int randomIndex = Random.Range(0, BoardManager.Instance.BlockNumbers.Count);
        m_image.color = BoardManager.Instance.BlockColors[randomIndex];
        blockData.value = BoardManager.Instance.BlockNumbers[randomIndex];
        UpdateBlockText();
    }

    internal void OnLand(int Row, int Col)
    {
        Landed = true;
        row = Row;
        column = Col;
    }

    internal IEnumerator MergeBlock(BlockView Block)
    {
        Block.m_image.DOFade(0, 0.1f);
        yield return Block.transform.DOLocalMoveY(transform.localPosition.y, 0.1f).WaitForCompletion();
        Destroy(Block.gameObject);
        blockData.value *= 2;
        m_image.color = BoardManager.Instance.GetColorForValue(blockData.value);
        UpdateBlockText();
    }

    void UpdateBlockText()
    {
        m_numText.text = blockData.value.ToString();
    }
}
