using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using UnityEngine;
using UnityEngine.EventSystems;
using Org.BouncyCastle.OpenSsl;

namespace PlayUR
{
    public partial class PlayURPlugin : UnitySingletonPersistent<PlayURPlugin>
    {
        /// <summary>
        /// Uses Rot13 to encode an arbitrary string, which can later be decoded on the PlayUR Dashboard.
        /// </summary>
        /// <param name="text">The text to encode into a completion code</param>
        /// <returns></returns>
        public static string GenerateCompletionCode(string text, bool encrypt = true)
        {
            if (encrypt)
            {
                text = "___RSA" + RSAHelper.EncryptData(text);
            }
            return Rot13.Transform(Base64Encode(text));
        }

        /// <summary>
        /// Takes a text box and populates it with a completion code that is encoded and can be copied to the clipboard by clicking the box.
        /// </summary>
        /// <param name="textBox">The text box to run the operation on</param>
        /// <param name="text">The text to encode into a completion code (unless textIsAlreadyEncoded is set to true, in which case this will be just passed through as-is</param>
        /// <param name="textIsAlreadyEncoded">Set this to true if you have already generated the code text with <see cref="PlayURPlugin.GenerateCompletionCode(string)"/></param>
        public static string PopulateTextBoxWithCopyableCompletionCode(TMPro.TMP_InputField textBox, string text, bool textIsAlreadyEncoded = false)
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

            return textBox.text;
        }


        public delegate void OnClickCompletionCodeButtonCallback(string s = null);

        /// <summary>
        /// Takes a button and populates it with a completion code that sends an encoded hitcode to a rediect URL
        /// </summary>
        /// <param name="textBox">The text box to run the operation on</param>
        /// <param name="text">The text to encode into a completion code (unless textIsAlreadyEncoded is set to true, in which case this will be just passed through as-is</param>
        /// <param name="textIsAlreadyEncoded">Set this to true if you have already generated the code text with <see cref="PlayURPlugin.GenerateCompletionCode(string)"/></param>
        /// <param name="urlFormat">The url to redirect to, with {0} being replaced with a URL-encoded version of code</param>
        /// <param name="buttonText">The text to put on the button (with {0} being replaced with the code/ If null, doesn't change the button's text</param>
        /// <param name="onClickCallback">Optional additional code to run on the button click in *addition* to the redirect</param>
        public static string PopulateButtonWithCompletionCodeURLRedirect(UnityEngine.UI.Button button, string text, string urlFormat = "https://app.prolific.com/submissions/complete?cc={0}", string buttonText = null, bool textIsAlreadyEncoded = false, OnClickCompletionCodeButtonCallback onClickCallback = null)
        {
            if (!textIsAlreadyEncoded)
            {
                text = GenerateCompletionCode(text);
            }

            if (buttonText != null)
            {
                if (button.gameObject.TryGetTextComponentAndSetText(string.Format(buttonText, text)))
                {
                    button.gameObject.SetActive(true);
                }
            }
            button.onClick.AddListener(() => {
                Application.OpenURL(string.Format(urlFormat, HttpUtility.UrlEncode(text)));
                if (onClickCallback!= null) onClickCallback();
            });

            return text;
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

        static class RSAHelper
        {
            private static string _publicKeyPEM = "-----BEGIN PUBLIC KEY-----\r\n" +
                "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAo/Ta5gPccLblo3VmzeOM\r\n" +
                "0l5yb1s1+hCqVSQv4pzejHf1zYiU90yp+F7TKGSAief2N7HqkQV7YGxUno4uO5We\r\n" +
                "J+a6V4MCGDGJe7iAhbqPaFKPBmGhUQGADb3v7yJX0x00rPawRU82hD0xIQa6ubOo\r\n" +
                "uDIqdPduU8dUBmm0iD4OVNVX5JYwNfODUiXAfjCT7JAgASRLmz1Br9U1WYXCbE6q\r\n" +
                "HhVF7jKpmHdxLu2rb17CbOxOyobvIZrENH4okjfl7OFOxBO2V13SCzGvdGyrcSXx\r\n" +
                "cRS1TfzKrWSj3QKi+XHsroaKPYngAdPp/OhFyZg3jlUqVasv7JPj8hpQzB87mdt6\r\n" +
                "hQIDAQAB\r\n" +
                "-----END PUBLIC KEY-----\r\n";
            public static string EncryptData(string plaintext)
            {
                byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);

                // Generate AES key
                byte[] aesKey = GenerateAESKey();
                byte[] iv = GenerateIV(); // Generate a unique IV for each encryption

                // Encrypt the data with AES
                byte[] encryptedData = EncryptAES(plaintextBytes, aesKey, iv);

                // Encrypt the AES key with RSA public key
                byte[] encryptedAESKey = EncryptRSA(aesKey, _publicKeyPEM);
                byte[] encryptedIV = EncryptRSA(iv, _publicKeyPEM); // Encrypt the IV as well

                // Concatenate the encrypted data, encrypted AES key, and encrypted IV
                // Use a delimiter that is unlikely to appear in the base64 encoded data
                string result = Convert.ToBase64String(encryptedData) + "||" + Convert.ToBase64String(encryptedAESKey) + "||" + Convert.ToBase64String(encryptedIV);
                return result;
            }



            private static byte[] GenerateAESKey()
            {
                using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
                {
                    aes.GenerateKey();
                    return aes.Key;
                }
            }

            private static byte[] GenerateIV()
            {
                using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
                {
                    aes.GenerateIV();
                    return aes.IV;
                }
            }

            private static byte[] EncryptAES(byte[] data, byte[] key, byte[] iv)
            {
                using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC; // Use CBC mode, which requires an IV.
                    aes.Padding = PaddingMode.PKCS7;

                    using (MemoryStream ms = new MemoryStream())
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(data, 0, data.Length);
                        cs.FlushFinalBlock();
                        return ms.ToArray();
                    }
                }
            }

            private static byte[] EncryptRSA(byte[] data, string publicKeyPEM)
            {
                using (TextReader reader = new StringReader(publicKeyPEM))
                {
                    PemReader pemReader = new PemReader(reader);
                    AsymmetricKeyParameter publicKey = (AsymmetricKeyParameter)pemReader.ReadObject();
                    RsaKeyParameters rsaPublicKey = (RsaKeyParameters)publicKey;

                    IAsymmetricBlockCipher cipher = new Pkcs1Encoding(new RsaEngine());
                    cipher.Init(true, rsaPublicKey); // true for encryption
                    return cipher.ProcessBlock(data, 0, data.Length);
                }
            }


            public static string DecryptData(string encryptedData, string privateKeyPEM)
            {
                // Placeholder for C# decryption (normally done in PHP)
                // In a real application, this would be in a separate C# program or library.
                // The privateKeyPEM is needed here for completeness.  This method
                //  is not called in the main flow of this C# program.
                try
                {
                    string[] parts = encryptedData.Split(new string[] { "||" }, StringSplitOptions.None);
                    if (parts.Length != 3)
                    {
                        throw new Exception("Invalid encrypted data format.");
                    }

                    byte[] encryptedDataBytes = Convert.FromBase64String(parts[0]);
                    byte[] encryptedAESKeyBytes = Convert.FromBase64String(parts[1]);
                    byte[] encryptedIVBytes = Convert.FromBase64String(parts[2]);


                    // Decrypt the AES key
                    byte[] aesKey = DecryptRSA(encryptedAESKeyBytes, privateKeyPEM);
                    byte[] iv = DecryptRSA(encryptedIVBytes, privateKeyPEM);


                    // Decrypt the data
                    byte[] decryptedData = DecryptAES(encryptedDataBytes, aesKey, iv);
                    return Encoding.UTF8.GetString(decryptedData);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during C# decryption: {ex.Message}");
                    return null; // Or throw, depending on your error handling policy
                }
            }

            private static byte[] DecryptAES(byte[] data, byte[] key, byte[] iv)
            {
                using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (MemoryStream ms = new MemoryStream(data))
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        using (MemoryStream output = new MemoryStream())
                        {
                            byte[] buffer = new byte[1024];
                            int read;
                            while ((read = cs.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                output.Write(buffer, 0, read);
                            }
                            return output.ToArray();
                        }
                    }
                }
            }

            private static byte[] DecryptRSA(byte[] data, string privateKeyPEM)
            {
                using (TextReader reader = new StringReader(privateKeyPEM))
                {
                    PemReader pemReader = new PemReader(reader);
                    AsymmetricCipherKeyPair keyPair = (AsymmetricCipherKeyPair)pemReader.ReadObject();
                    RsaKeyParameters privateKey = (RsaKeyParameters)keyPair.Private;

                    IAsymmetricBlockCipher cipher = new Pkcs1Encoding(new RsaEngine());
                    cipher.Init(false, privateKey); // false for decryption
                    return cipher.ProcessBlock(data, 0, data.Length);
                }
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