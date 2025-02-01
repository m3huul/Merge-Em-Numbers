using System;
using DG;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Threading.Tasks.Sources;
using System.ComponentModel;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance;
    // [SerializeField] private KeyValuePair<int, float> PossibleValues=new(); //Make my own datatype
    [SerializeField] internal List<BoardBlocks> m_boardBlocks = new();
    [SerializeField] internal List<int> BlockNumbers = new();
    [SerializeField] private Transform m_newBlocksParent;
    [SerializeField] private GameObject m_blockPrefab;
    [SerializeField] private float basePullDownSpeed = 15f;
    [SerializeField] internal float fastPullDownSpeed = 0.2f;
    private BlockView _currMovingBlock;
    private Tween _currBlockTween;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    void Start()
    {
        SetupBlock();
    }

    void SetupBlock()
    {
        GameObject FirstBlock = Instantiate(m_blockPrefab, m_boardBlocks[2].Column[0].boardPosition.position, m_boardBlocks[2].Column[0].boardPosition.rotation, m_newBlocksParent);
        _currMovingBlock = FirstBlock.GetComponent<BlockView>();                        //Test block instantiation
        _currMovingBlock.initBlockView();

        PullTheBlock(basePullDownSpeed);

        InputManager.Instance.setBlock(_currMovingBlock);
    }

    internal void PullTheBlock(float duration)
    {
        KeyValuePair<Block, Vector3> BlockData = GetMovableBlock();

        _currBlockTween?.Kill();
        _currBlockTween = _currMovingBlock.transform.DOLocalMoveY(BlockData.Value.y, duration)
        .OnComplete(() =>
        {
            _currMovingBlock.transform.localPosition = BlockData.Value;
            OnBlockLanded(BlockData.Key);
        });

    }

    void OnBlockLanded(Block LandedBlock)
    {
        _currMovingBlock.OnLand();
        LandedBlock.value = _currMovingBlock.blockData.value;
        SetupBlock();
    }

    KeyValuePair<Block, Vector3> GetMovableBlock()
    {
        KeyValuePair<Block, Vector3> BlockData = new();
        Vector3 targetLocalPosi = new();
        for (int i = m_boardBlocks[InputManager.Instance.m_columnIndex].Column.Count - 1; i >= 0; i--)
        {
            int val = m_boardBlocks[InputManager.Instance.m_columnIndex].Column[i].value;
            if (val == 0)
            {
                targetLocalPosi = m_boardBlocks[InputManager.Instance.m_columnIndex].Column[i].boardPosition.localPosition;
                BlockData = new KeyValuePair<Block, Vector3>(m_boardBlocks[InputManager.Instance.m_columnIndex].Column[i], targetLocalPosi);
                return BlockData;
            }
        }
        return BlockData;
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

    public Block()
    {
        value = 0;
        boardPosition = null;
    }
}