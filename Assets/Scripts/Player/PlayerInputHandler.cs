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
            MoveInput = value.Get<Vector2>();
        }

        public void OnJump(InputValue value)
        {
            if (value.isPressed) JumpTriggered = true;
        }

        private void LateUpdate()
        {
            JumpTriggered = false;
        }
    }
}