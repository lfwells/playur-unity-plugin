using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace PlayUR
{
    public class InputNavigator : MonoBehaviour
    {
        EventSystem system;

        void Start()
        {
            system = EventSystem.current;// EventSystemManager.currentSystem;
        
        }

        void Update()
        {
            bool isPressed;

#if USE_INPUT_SYSTEM
            isPressed = UnityEngine.InputSystem.Keyboard.current.tabKey.wasPressedThisFrame;
#else
            isPressed = Input.GetKeyDown(KeyCode.Tab);
#endif

            if (isPressed)
            {
                Selectable next = system.currentSelectedGameObject.GetComponent<Selectable>().FindSelectableOnDown();
            
                if (next != null)
                {
                
                    InputField inputfield = next.GetComponent<InputField>();
                    if (inputfield != null)
                        inputfield.OnPointerClick(new PointerEventData(system));  //if it's an input field, also set the text caret
                
                    system.SetSelectedGameObject(next.gameObject, new BaseEventData(system));
                }
                //else Debug.Log("next nagivation element not found");
            
            }
        }
    }
}