using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FeverUI : MonoBehaviour
{
    [SerializeField] private GameObject BG;
    [SerializeField] private RawImage frame;

    private void Awake()
    {
        Color c = frame.color;
        c.a = 0;
        frame.color = c;
    }


    public void FeverOn()
    {
        BG.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(Fade(1));
    }


    public void FeverOff()
    {
        StopAllCoroutines();
        StartCoroutine(Fade(0));
        BG.SetActive(false);
    }


    IEnumerator Fade(float target)
    {
        Color color = frame.color;
        
        float start = color.a;
        float time = 0;

        while (time < 1)
        {
            time += Time.deltaTime * 3;

            color.a = Mathf.Lerp(start, target, time);

            frame.color = color;

            yield return null;
        }
    }
}
