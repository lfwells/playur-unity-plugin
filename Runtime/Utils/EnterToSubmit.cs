using UnityEngine;
using UnityEngine.UI;

namespace PlayUR
{
    /// <summary>
    /// Used by the login screen to allow the user to press enter to submit the login form.
    /// </summary>
    public class EnterToSubmit : MonoBehaviour
    {
        Button button;
        void Start()
        {
            button = GetComponent<Button>();
        }
        void Update()
        {
            bool isPressed;

#if USE_INPUT_SYSTEM
            isPressed = UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame || 
                UnityEngine.InputSystem.Keyboard.current.numpadEnterKey.wasPressedThisFrame;
#else
            isPressed = Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return);
#endif

            if (isPressed)
                button.onClick.Invoke();
        }
    }
}