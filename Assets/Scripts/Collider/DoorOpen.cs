using System.Collections.Generic;
using UnityEngine;

public class DoorOpen : MonoBehaviour
{
    [SerializeField] private List<Color> requiredColors;

    private List<Color> paintedColors = new();

    public void AddPaintColor(Color color)
    {
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
        if (paintedColors.Count >= requiredColors.Count)
        {
            Debug.Log("모든 필요한 색깔 완료!");
        }
    }
}
