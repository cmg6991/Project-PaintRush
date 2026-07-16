using System.Collections.Generic;
using UnityEngine;

public class DoorOpen : MonoBehaviour
{
    [SerializeField] private List<Color> requiredColors;

    private List<Color> paintedColors = new();

    private bool isOpened;

    private void OnEnable()
    {
        if (MonsterManager.Instance != null)
        {
            MonsterManager.Instance.OnAliveCountChanged += OnAliveCountChanged;
        }
    }

    private void OnDisable()
    {
        if (MonsterManager.Instance != null)
        {
            MonsterManager.Instance.OnAliveCountChanged -= OnAliveCountChanged;
        }
    }

    private void OnAliveCountChanged(int aliveCount)
    {
        CheckOpen();
    }

    public void AddPaintColor(Color color)
    {
        if (isOpened)
            return;
        foreach (Color requiredColor in requiredColors)
        {
            // 총에서 발사한 색이 필요한 색인지 확인
            if (IsSameColor(color, requiredColor))
            {
                // 이미 칠한 색이면 중복 추가 X
                foreach (Color paintedColor in paintedColors)
                {
                    if (IsSameColor(color, paintedColor))
                        return;
                }

                paintedColors.Add(color);

                Debug.Log($"색깔 진행도: {paintedColors.Count}/{requiredColors.Count}");
                CheckOpen();
                return;
            }
        }
    }

    private bool IsSameColor(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 0.1f &&
           Mathf.Abs(a.g - b.g) < 0.1f &&
           Mathf.Abs(a.b - b.b) < 0.1f;
    }

    private void CheckOpen()
    {
        bool colorComplete = paintedColors.Count >= requiredColors.Count;
        bool monsterComplete = MonsterManager.Instance != null &&
                               MonsterManager.Instance.AliveCount == 1;

        if (colorComplete && monsterComplete)
        {
            isOpened = true;
            OpenDoor();
        }
    }

    private void OpenDoor()
    {
        Debug.Log("문이 열립니다!");

        // 예시
        // animator.SetTrigger("Open");
        // GetComponent<Collider2D>().enabled = false;
        // Destroy(gameObject);
    }
}
