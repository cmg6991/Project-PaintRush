using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using NUnit.Framework;

[System.Serializable]
public class TutorialGuideData
{
    public TutorialZone.TutorialStep step;
    public string titleText;                     // Jump, Climb
    [TextArea] public string descText;           // [Space]키 눌러 점프하세요 
    public Sprite[] keyIconSprites;                // 키보드 키 아이콘
    public Sprite[] animationFrames;             // GIF 연출용 프레임 이미지 리스트
    public float frameRate = 0.15f;              // 각 프레임 전환 속도
    public float autoHideDelay = 4.0f;
}

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("--- 튜토리얼 UI 요소 설정 ---")]
    public GameObject tutorialPanel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descText;
    public List<Image> keyIconImageList = new List<Image>();            // 키보드 키 아이콘 UI
    public Image animDisplayImage;                                      // GIF 애니메이션 재생 UI Image

    [Header("--- 기믹별 가이드 데이터 리스트 ---")]
    public List<TutorialGuideData> guideDataList = new List<TutorialGuideData>();

    [Header("--- 튜토리얼 상태 플래그 ---")]
    public bool isCutscenePlaying = false;          // 컷씬, 카메라 안내 연출 진행 중 여부 

    [Header("--- 튜토리얼 입력 허용 플래그 ---")]
    public bool canMove = false;
    public bool canJump = false;
    public bool canClimb = false;
    public bool canHang = false;
    public bool canShoot = false;
    public bool canShowGun = false;
    public bool canShowItem = false;
    public bool canMonsterMove = false;

    private Coroutine animCoroutine;
    private Coroutine autoHideCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // 튜토리얼 상태 초기화 함수
    public void ResetTutorialFlags()
    {
        isCutscenePlaying = false;
        canMove = false;
        canJump = false;
        canClimb = false;
        canHang = false;
        canShoot = false;
        canShowItem = false;
        canShowGun = false;
        canMonsterMove = false;
        HideTutorialMessage();
    }

    // 단계별 GIF 애니메이션 UI 가이드 시작
    public void ShowGuideAnimation (TutorialZone.TutorialStep step, string customMessage = "", Transform targetTransform = null)
    {
        // 이미 재생 중인 애니메이션이 있다면 중지
        if (animCoroutine != null) StopCoroutine(animCoroutine);
        if (autoHideCoroutine != null) StopCoroutine(autoHideCoroutine);

        TutorialGuideData data = guideDataList.Find(x => x.step == step);

        if (data != null)
        {
            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(true);
            }
            if (titleText != null) titleText.text = data.titleText;
            if (descText != null) descText.text = !string.IsNullOrEmpty(customMessage) ? customMessage : data.descText;
        
            // 다중 키 아이콘 슬롯 설정 (W, S 키 등)
            if (keyIconImageList != null && keyIconImageList.Count > 0)
            {
                for (int i = 0; i< keyIconImageList.Count; i++)
                {
                    if (data.keyIconSprites != null && i < data.keyIconSprites.Length && data.keyIconSprites[i] != null)
                    {
                        keyIconImageList[i].sprite = data.keyIconSprites[i];
                        keyIconImageList[i].gameObject.SetActive(true);
                    }
                    else
                    {
                        keyIconImageList[i].gameObject.SetActive(false);
                    }
                }
            }

            // GIF 프레임 애니메이션 코루틴 재생
            if (animDisplayImage != null && data.animationFrames != null && data.animationFrames.Length > 0)
            {
                animDisplayImage.gameObject.SetActive(true);
                animCoroutine = StartCoroutine(PlayFrameAnimationRoutine(data));
            }
            else if (animDisplayImage != null)
            {
                animDisplayImage.gameObject.SetActive(false);
            }

            // N초후 가이드 패널 자동 감추기 코루틴
            if (data.autoHideDelay > 0)
            {
                autoHideCoroutine = StartCoroutine(AutoHideRoutine(data.autoHideDelay));
            }
        }
        else
        {
            // 전용 가이드 데이터가 없으면 기존 텍스트만 표시
            ShowTutorialMessage(customMessage);
        }
    }

    // GIF 프레임 이미지 연속 루프 코루틴
    private IEnumerator PlayFrameAnimationRoutine(TutorialGuideData data)
    {
        int index = 0;
        while (true)
        {
            if (data.animationFrames.Length > 0)
            {
                animDisplayImage.sprite = data.animationFrames[index];
                index = (index + 1) % data.animationFrames.Length;
            }
            yield return new WaitForSeconds(data.frameRate);
        }    
    }

    // N초 후 자동 닫기 코루틴
    private IEnumerator AutoHideRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideTutorialMessage();
    }

    // 텍스트 전용 메시지 표시
    public void ShowTutorialMessage(string message)
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(true);
        if (descText != null) descText.text = message;
    }

    // UI 가이드 메시지 및 애니메이션 숨기기
    public void HideTutorialMessage()
    {
        if (animCoroutine != null)
        {
            StopCoroutine(animCoroutine);
            animCoroutine = null;
        }

        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }
    }
}
