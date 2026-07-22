using System.Security.Cryptography.X509Certificates;
using UnityEngine;

public class RandomSplash : MonoBehaviour
{
    public GameObject[] splashPrefabs;

    public GameObject[] brushPrefabs;

    public GameObject[] feverSplashPrefabs;

    public GameObject Spawn(Vector2 mousePos, Color color, bool isBrush = false,bool isFever = false)
    {
        //GameObject[] targetArray = isBrush ? brushPrefabs : splashPrefabs;

        //GameObject prefab = targetArray[Random.Range(0, targetArray.Length)];
        //GameObject obj = Instantiate(prefab, mousePos, Quaternion.identity);

        //float scale = Random.Range(0.9f, 1.5f);
        //obj.transform.localScale = Vector3.one * scale;

        //float rot = Random.Range(0f, 360f);
        //obj.transform.rotation = Quaternion.Euler(0, 0, rot);

        //SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        //if (sr != null)
        //    sr.color = color;

        //return obj;
        GameObject[] targetArray;

        if (isFever)
        {
            targetArray = feverSplashPrefabs;
        }
        else
        {
            targetArray = isBrush ? brushPrefabs : splashPrefabs;
        }

        // 선택된 배열이 비어있는지 방어 코드 추가
        if (targetArray == null || targetArray.Length == 0)
        {
            Debug.LogWarning("스폰할 프리팹 배열이 비어있습니다!");
            return null;
        }

        GameObject prefab = targetArray[Random.Range(0, targetArray.Length)];
        GameObject obj = Instantiate(prefab, mousePos, Quaternion.identity);

        float scale = Random.Range(0.9f, 1.5f);
        obj.transform.localScale = Vector3.one * scale;

        float rot = Random.Range(0f, 360f);
        obj.transform.rotation = Quaternion.Euler(0, 0, rot);

        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = color;

        return obj;
    }
}
