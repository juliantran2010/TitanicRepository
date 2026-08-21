using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	public class StarterAssetsInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;

		[Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

		private bool isGameplayState = true; // Track the current game state

        private void OnEnable()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged += HandleStateChanged;
            }
        }

        private void OnDisable()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(GameState state)
        {
            isGameplayState = (state == GameState.Gameplay);

            if (!isGameplayState)
            {
                // Input-Werte zurücksetzen & Blick-Input deaktivieren
                move = Vector2.zero;
                look = Vector2.zero;
                jump = false;
                sprint = false;

                cursorInputForLook = false;
                SetCursorState(false);
            }
            else
            {
                cursorInputForLook = true;
                SetCursorState(true);
            }
        }


#if ENABLE_INPUT_SYSTEM
        public void OnMove(InputValue value)
		{
			if (!isGameplayState) return;
			MoveInput(value.Get<Vector2>());
		}

		public void OnLook(InputValue value)
		{
			if(isGameplayState && cursorInputForLook)
			{
				LookInput(value.Get<Vector2>());
			}
			else
			{
				look = Vector2.zero; // Reset look input when not in gameplay state
            }
		}

		public void OnJump(InputValue value)
		{
			if (!isGameplayState) return;
			JumpInput(value.isPressed);
		}

		public void OnSprint(InputValue value)
		{
			if (!isGameplayState) return;
			SprintInput(value.isPressed);
		}
#endif


		public void MoveInput(Vector2 newMoveDirection)
		{
			move = isGameplayState ? newMoveDirection : Vector2.zero;
		} 

		public void LookInput(Vector2 newLookDirection)
		{
			look = isGameplayState ? newLookDirection : Vector2.zero;
		}

		public void JumpInput(bool newJumpState)
		{
			jump = isGameplayState && newJumpState;
		}

		public void SprintInput(bool newSprintState)
		{
			sprint = isGameplayState && newSprintState;
		}
		
		private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(isGameplayState ? cursorLocked : false);
		}

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
			Cursor.visible = !newState;
        }
	}
	
}