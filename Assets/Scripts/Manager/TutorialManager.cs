using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using NUnit.Framework;

[System.Serializable]
public class TutorialGuideData
{
    public TutorialZone.TutorialStep step;
    [TextArea] public string guideText;                     // 머리 위에 띄울 한 줄 설명문
    public Sprite[] keySprites;                             // 키 아이콘 스프라이트 배열                       
    public bool isWideSingleKey;                            // 와이드 대형 1개 키 모드, false : 2분할 키 모드
    public float autoHideDelay = 4.0f;                      // 가이드 자동 감춤 시간
}

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("--- 튜토리얼 UI 요소 설정 ---")]
    public GameObject tutorialPanel;
    public TextMeshProUGUI guideText;

    [Header("--- 키 슬롯 모드 ---")]
    public Image wideSingleKeySlot;     // 단일 와이드, 대형 키 슬롯 UI Image
    public Image leftKeySlot;           // 왼쪽 키 슬롯 UI Image
    public Image rightKeySlot;          // 2분할 중 오른쪽 키 슬롯 UI Image

    [Header("--- 기믹별 가이드 데이터 리스트 ---")]
    public List<TutorialGuideData> guideDataList = new List<TutorialGuideData>();

    [Header("--- 튜토리얼 상태 플래그 ---")]
    public bool isCutscenePlaying = false;          // 컷씬, 카메라 안내 연출 진행 중 여부 

    [Header("--- 튜토리얼 체크포인트(리스폰) 설정 ---")]
    public Transform currentRespawnPoint;   // 플레이어가 사망 시 돌아갈 최신 Zone 위치

    [Header("--- 튜토리얼 입력 허용 플래그 ---")]
    public bool canMove = false;
    public bool canJump = false;
    public bool canClimb = false;
    public bool canHang = false;
    public bool canShoot = false;
    public bool canShowGun = false;
    public bool canShowItem = false;
    public bool canMonsterMove = false;
    public bool canUseSkill = false;
    public bool canOpenDoor = false;

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
        canUseSkill = false;
        canOpenDoor = false;
        HideTutorialMessage();
    }

    public void ShowGuideUI(TutorialZone.TutorialStep step, string customMessage = "")
    {
        if (autoHideCoroutine != null) StopCoroutine(autoHideCoroutine);

        TutorialGuideData data = guideDataList.Find(x => x.step == step);

        if (data != null)
        {
            if (tutorialPanel != null) tutorialPanel.SetActive(true);

            // 한줄 텍스트
            if (guideText != null)
            {
                guideText.text = !string.IsNullOrEmpty(customMessage) ? customMessage : data.guideText;
            }

            // 키 슬롯 모드 분기
            if (data.isWideSingleKey)
            {
                // 와이드 단일 키 켜기 & 2분할 키 끄기
                if (wideSingleKeySlot != null && data.keySprites != null && data.keySprites.Length > 0)
                {
                    wideSingleKeySlot.sprite = data.keySprites[0];
                    wideSingleKeySlot.preserveAspect = true;
                    wideSingleKeySlot.gameObject.SetActive(true);
                }

                if (leftKeySlot != null) leftKeySlot.gameObject.SetActive(false);
                if (rightKeySlot != null) rightKeySlot.gameObject.SetActive(false);
            }
            else
            {
                // 와이드 단일 키 끄기 & 2분할 키 켜기
                if (wideSingleKeySlot != null) wideSingleKeySlot.gameObject.SetActive(false);

                // 왼쪽 키
                if (leftKeySlot != null)
                {
                    bool hasLeft = data.keySprites != null && data.keySprites.Length > 0 && data.keySprites[0] != null;
                    leftKeySlot.gameObject.SetActive(hasLeft);
                    if (hasLeft)
                    {
                        leftKeySlot.sprite = data.keySprites[0];
                        leftKeySlot.preserveAspect = true;
                    }
                }

                // 오른쪽 키
                if (rightKeySlot != null)
                {
                    bool hasRight = data.keySprites != null && data.keySprites.Length > 1 && data.keySprites[1] != null;
                    rightKeySlot.gameObject.SetActive(hasRight);
                    if (hasRight)
                    {
                        rightKeySlot.sprite = data.keySprites[1];
                        rightKeySlot.preserveAspect = true;
                    }
                }
            }

            // N초 후 자동 감춤
            if (data.autoHideDelay > 0)
            {
                autoHideCoroutine = StartCoroutine(AutoHideRoutine(data.autoHideDelay));
            }
        }
    }

    // N초 후 자동 닫기 코루틴
    private IEnumerator AutoHideRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideTutorialMessage();
    }

    // UI 가이드 메시지 및 애니메이션 숨기기
    public void HideTutorialMessage()
    {
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

    public void OnClickRestartTutorial()
    {
        Time.timeScale = 1.0f;  // 혹시 멈춰있을 게임 속도 복구
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnClickSkipTutorial(string nextSceneName = "ProtoScene")
    {
        Time.timeScale = 1.0f;
        SceneManager.LoadScene(nextSceneName);
    }
}
