using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public static InputManager Instance;
    [SerializeField] internal int m_columnIndex=2;
    private BlockView m_blockTransform;

    void Awake(){
        if(Instance==null){
            Instance=this;
        }
    }
    
    internal void setBlock(BlockView block){
        m_blockTransform=block;
    }

    public void OnPointerDown(PointerEventData eventData){
        if (m_blockTransform != null)
        {
            float closestDistance = float.MaxValue;
            Transform closestColumnPosition = null;

            // Loop through the columns in BoardManager
            for (int i = 0; i < BoardManager.Instance.m_boardBlocks.Count; i++)
            {
                if (BoardManager.Instance.m_boardBlocks[i].Column.Count > 0)
                {
                    // Get the X position of the first block in the column
                    float columnX = BoardManager.Instance.m_boardBlocks[i].Column[0].boardPosition.position.x;

                    // Calculate the distance between the event position and the column's X position
                    float distance = Mathf.Abs(eventData.position.x - columnX);

                    // Update the closest column if this one is nearer
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestColumnPosition = BoardManager.Instance.m_boardBlocks[i].Column[0].boardPosition;
                        m_columnIndex=i;
                    }
                }
            }

            // Move the block to the closest column's X position if found
            if (closestColumnPosition != null)
            {
                Vector3 targetPosition = m_blockTransform.transform.position;
                targetPosition.x = closestColumnPosition.position.x;
                m_blockTransform.transform.position = targetPosition;
            }
        }
    }
    public void OnPointerUp(PointerEventData eventData){
        m_blockTransform=null;
        BoardManager.Instance.PullTheBlock(BoardManager.Instance.fastPullDownSpeed);
    }

    public void OnDrag(PointerEventData eventData){
        if (m_blockTransform != null)
        {
            float closestDistance = float.MaxValue;
            Transform closestColumnPosition = null;

            // Loop through the columns in BoardManager
            for (int i = 0; i < BoardManager.Instance.m_boardBlocks.Count; i++)
            {
                if (BoardManager.Instance.m_boardBlocks[i].Column.Count > 0)
                {
                    // Get the X position of the first block in the column
                    float columnX = BoardManager.Instance.m_boardBlocks[i].Column[0].boardPosition.position.x;

                    // Calculate the distance between the event position and the column's X position
                    float distance = Mathf.Abs(eventData.position.x - columnX);

                    // Update the closest column if this one is nearer
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestColumnPosition = BoardManager.Instance.m_boardBlocks[i].Column[0].boardPosition;
                        m_columnIndex=i;
                    }
                }
            }

            // Move the block to the closest column's X position if found
            if (closestColumnPosition != null)
            {
                Vector3 targetPosition = m_blockTransform.transform.position;
                targetPosition.x = closestColumnPosition.position.x;
                m_blockTransform.transform.position = targetPosition;
            }
        }
    }
}
