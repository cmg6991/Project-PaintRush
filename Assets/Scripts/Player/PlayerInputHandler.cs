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
            if (TutorialManager.Instance != null && !TutorialManager.Instance.canMove)
            {
                MoveInput = Vector2.zero;
                return;
            }

            SoundManager.Instance.PlaySFX(SFXType.PlayerWalk);
            MoveInput = value.Get<Vector2>();
        }

        public void OnJump(InputValue value)
        {
            if (TutorialManager.Instance != null && !TutorialManager.Instance.canJump)
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