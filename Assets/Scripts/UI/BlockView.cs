using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BlockView : MonoBehaviour
{
    [SerializeField] internal Block blockData=new();
    private TMP_Text m_numText;
    internal bool Landed=false;

    void Awake(){
        m_numText=transform.GetChild(0).GetComponent<TMP_Text>();
    }

    internal void initBlockView(){
        BoardManager manager=BoardManager.Instance;
        Vector3 initPosi = BoardManager.Instance.m_boardBlocks[InputManager.Instance.m_columnIndex].Column[0].boardPosition.position;
        Vector3 StartPosi= initPosi+new Vector3(0, 400, 0);
        
        transform.position=StartPosi;
        blockData.boardPosition=this.transform;

        //Value Calc here
        blockData.value=BoardManager.Instance.BlockNumbers[Random.Range(0, BoardManager.Instance.BlockNumbers.Count)];
        m_numText.text=blockData.value.ToString();
    }

    internal void OnLand(){
        Landed=true;
    }
}
