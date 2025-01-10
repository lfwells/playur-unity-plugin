using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PlayUR
{
    public partial class PlayURPlugin : UnitySingletonPersistent<PlayURPlugin>
    {
        /// <summary>
        /// Uses Rot13 to encode an arbitrary string, which can later be decoded on the PlayUR Dashboard.
        /// </summary>
        /// <param name="text">The text to encode into a completion code</param>
        /// <returns></returns>
        public static string GenerateCompletionCode(string text)
        {
            return Rot13.Transform(Base64Encode(text));
        }

        /// <summary>
        /// Takes a text box and populates it with a completion code that is encoded and can be copied to the clipboard by clicking the box.
        /// </summary>
        /// <param name="textBox">The text box to run the operation on</param>
        /// <param name="text">The text to encode into a completion code (unless textIsAlreadyEncoded is set to true, in which case this will be just passed through as-is</param>
        /// <param name="textIsAlreadyEncoded">Set this to true if you have already generated the code text with <see cref="PlayURPlugin.GenerateCompletionCode(string)"/></param>
        public static void PopulateTextBoxWithCopyableCompletionCode(TMPro.TMP_InputField textBox, string text, bool textIsAlreadyEncoded = false)
        {
            if (!textIsAlreadyEncoded)
            {
                textBox.text = GenerateCompletionCode(text);
            }
            else
            {
                textBox.text = text;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            textBox.gameObject.AddComponent<WebGLCopyAndPaste>();
#endif
            textBox.onSelect.AddListener((s) => {
                selectedTextBox = textBox;
                ClickEvent(s);
            });
        }

        /// <summary>
        /// Takes a text box and populates it with a completion code that is encoded and can be copied to the clipboard by clicking the box (legacy version).
        /// </summary>
        /// <param name="textBox">The text box to run the operation on</param>
        /// <param name="text">The text to encode into a completion code (unless textIsAlreadyEncoded is set to true, in which case this will be just passed through as-is</param>
        /// <param name="textIsAlreadyEncoded">Set this to true if you have already generated the code text with <see cref="PlayURPlugin.GenerateCompletionCode(string)"/></param>
        public static void PopulateTextBoxWithCopyableCompletionCode(UnityEngine.UI.InputField textBox, string text, bool textIsAlreadyEncoded = false)
        {
            if (!textIsAlreadyEncoded)
            {
                textBox.text = GenerateCompletionCode(text);
            }
            else
            {
                textBox.text = text;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            textBox.AddComponent<WebGLCopyAndPaste>();
#endif
            var et = textBox.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            var ett = new EventTrigger.TriggerEvent();
            ett.AddListener((e) =>
            {
                var s = textBox.text;
                selectedTextBox = textBox;
                ClickEvent(s);
            });
            et.triggers.Add(new EventTrigger.Entry() { eventID = EventTriggerType.PointerClick, callback = ett });
        }

        static MonoBehaviour selectedTextBox;
        static void ClickEvent(string s)
        {
            selectedTextBox.StartCoroutine(ClickEventRoutine(s));
        }
        static IEnumerator ClickEventRoutine(string s)
        {
            Screen.fullScreen = false;
            yield return new WaitForSecondsRealtime(1f);
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLCopyAndPasteAPI.PassCopyToBrowser(s);
            yield return new WaitForSecondsRealtime(1);
#else
            TextEditor te = new TextEditor();
            te.text = s;
            te.SelectAll();
            te.Copy();
#endif
            if (PlayURPlugin.instance.HasMTurkID)
                PlayURPlugin.instance.mTurkCompletionCodeCopiedMessage();
            else if (PlayURPlugin.instance.HasProlificID)
                PlayURPlugin.instance.prolificCompletionCodeCopiedMessage();
            yield break;
        }

        static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        static class Rot13
        {
            /// <summary>
            /// Performs the ROT13 character rotation.
            /// </summary>
            public static string Transform(string value)
            {
                char[] array = value.ToCharArray();
                for (int i = 0; i < array.Length; i++)
                {
                    int number = (int)array[i];

                    if (number >= 'a' && number <= 'z')
                    {
                        if (number > 'm')
                        {
                            number -= 13;
                        }
                        else
                        {
                            number += 13;
                        }
                    }
                    else if (number >= 'A' && number <= 'Z')
                    {
                        if (number > 'M')
                        {
                            number -= 13;
                        }
                        else
                        {
                            number += 13;
                        }
                    }
                    array[i] = (char)number;
                }
                return new string(array);
            }
        }

        class WebGLCopyAndPasteAPI
        {
#if UNITY_WEBGL
            [DllImport("__Internal")]
            private static extern void initWebGLCopyAndPaste(string objectName, string cutCopyCallbackFuncName, string pasteCallbackFuncName);
            [DllImport("__Internal")]
            private static extern void passCopyToBrowser(string str);
#endif

            static public void Init(string objectName, string cutCopyCallbackFuncName, string pasteCallbackFuncName)
            {
#if UNITY_WEBGL
            initWebGLCopyAndPaste(objectName, cutCopyCallbackFuncName, pasteCallbackFuncName);
#endif
            }

            static public void PassCopyToBrowser(string str)
            {
#if UNITY_WEBGL && !UNITY_EDITOR
            passCopyToBrowser(str);
#endif
            }
        }

        public class WebGLCopyAndPaste : MonoBehaviour
        {
            void Start()
            {
                if (!Application.isEditor)
                {
                    WebGLCopyAndPasteAPI.Init(this.name, "GetClipboard", "ReceivePaste");
                }
            }

            private void SendKey(string baseKey)
            {
                string appleKey = "%" + baseKey;
                string naturalKey = "^" + baseKey;

                var currentObj = EventSystem.current.currentSelectedGameObject;
                if (currentObj == null)
                {
                    return;
                }
                {
                    var input = currentObj.GetComponent<UnityEngine.UI.InputField>();
                    if (input != null)
                    {
                        // I don't know what's going on here. The code in InputField
                        // is looking for ctrl-c but that fails on Mac Chrome/Firefox
                        input.ProcessEvent(Event.KeyboardEvent(naturalKey));
                        input.ProcessEvent(Event.KeyboardEvent(appleKey));
                        // so let's hope one of these is basically a noop
                        return;
                    }
                }
                {
                    var input = currentObj.GetComponent<TMPro.TMP_InputField>();
                    if (input != null) {
                        // I don't know what's going on here. The code in InputField
                        // is looking for ctrl-c but that fails on Mac Chrome/Firefox
                        // so let's hope one of these is basically a noop
                        input.ProcessEvent(Event.KeyboardEvent(naturalKey));
                        input.ProcessEvent(Event.KeyboardEvent(appleKey));
                        return;
                    }
                }
            }

            public void GetClipboard(string key)
            {
                SendKey(key);
                WebGLCopyAndPasteAPI.PassCopyToBrowser(GUIUtility.systemCopyBuffer);
            }

            public void ReceivePaste(string str)
            {
                GUIUtility.systemCopyBuffer = str;
            }

        }
    }
}