using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    // [SerializeField] private KeyValuePair<int, float> PossibleValues=new(); //Make my own datatype
    [SerializeField] internal List<BoardBlocks> m_boardBlocks = new();
    [SerializeField] private Transform m_newBlocksParent;
    [SerializeField] private GameObject m_blockPrefab;
    [SerializeField] private InputManager m_inputManager;
    private int m_columns = 5;
    private int m_rows = 8;

    void Awake()
    {
        // int RandomPredictedValue

        GameObject FirstBlock=Instantiate(m_blockPrefab, m_boardBlocks[2].Column[0].boardPosition.position, m_boardBlocks[2].Column[0].boardPosition.rotation, m_newBlocksParent);
        BlockView blockView = FirstBlock.GetComponent<BlockView>();
        blockView.blockData = m_boardBlocks[2].Column[0];
        m_inputManager.setBlock(blockView);
    }
}

[Serializable]
public class BoardBlocks
{
    public List<Block> Column = new();
}

[Serializable]
public class Block
{
    public int value;
    public Transform boardPosition;
}