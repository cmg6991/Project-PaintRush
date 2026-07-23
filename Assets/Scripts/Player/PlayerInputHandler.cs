using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Player
{
    public class PlayerInputHandler : MonoBehaviour
    {
        // Move Machine Data
        public Vector2 MoveInput { get; private set; }
        public bool JumpTriggered { get; private set; }

        public void OnMove(InputValue value)
        {
            // 컷씬/카메라 연출 중이거나, 이동이 아직 해금되지 않은 경우 이동 차단
            if (TutorialManager.Instance != null && (TutorialManager.Instance.isCutscenePlaying || !TutorialManager.Instance.canMove))
            {
                MoveInput = Vector2.zero;
                return;
            }

            SoundManager.Instance.PlaySFX(SFXType.PlayerWalk);
            MoveInput = value.Get<Vector2>();
        }

        public void OnJump(InputValue value)
        {
            // 컷씬, 카메라 연출 중이거나, 점프가 아직 해금되지 않은 경우 점프 차단
            if (TutorialManager.Instance != null && (TutorialManager.Instance.isCutscenePlaying || !TutorialManager.Instance.canJump))
            {
                return;
            }

            SoundManager.Instance.PlaySFX(SFXType.PlayerJump);
            if (value.isPressed) JumpTriggered = true;
        }

        private void LateUpdate()
        {
            JumpTriggered = false;
        }
    }
}