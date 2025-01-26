using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [SerializeField] private BoardManager m_boardManager;
    private BlockView m_blockTransform;
    internal void setBlock(BlockView block){
        m_blockTransform=block;
    }

    public void OnPointerDown(PointerEventData eventData){
        if (m_blockTransform != null)
        {
            float closestDistance = float.MaxValue;
            Transform closestColumnPosition = null;

            // Loop through the columns in BoardManager
            for (int i = 0; i < m_boardManager.m_boardBlocks.Count; i++)
            {
                if (m_boardManager.m_boardBlocks[i].Column.Count > 0)
                {
                    // Get the X position of the first block in the column
                    float columnX = m_boardManager.m_boardBlocks[i].Column[0].boardPosition.position.x;

                    // Calculate the distance between the event position and the column's X position
                    float distance = Mathf.Abs(eventData.position.x - columnX);

                    // Update the closest column if this one is nearer
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestColumnPosition = m_boardManager.m_boardBlocks[i].Column[0].boardPosition;
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

    }

    public void OnDrag(PointerEventData eventData){
        if (m_blockTransform != null)
        {
            float closestDistance = float.MaxValue;
            Transform closestColumnPosition = null;

            // Loop through the columns in BoardManager
            for (int i = 0; i < m_boardManager.m_boardBlocks.Count; i++)
            {
                if (m_boardManager.m_boardBlocks[i].Column.Count > 0)
                {
                    // Get the X position of the first block in the column
                    float columnX = m_boardManager.m_boardBlocks[i].Column[0].boardPosition.position.x;

                    // Calculate the distance between the event position and the column's X position
                    float distance = Mathf.Abs(eventData.position.x - columnX);

                    // Update the closest column if this one is nearer
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestColumnPosition = m_boardManager.m_boardBlocks[i].Column[0].boardPosition;
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
