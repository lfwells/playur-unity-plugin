using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayUR.Samples
{
    public class SaveGameTester : MonoBehaviour
    {
        public int someValue = 0;
        // Start is called before the first frame update
        void Start()
        {
            someValue = PlayerPrefs.GetInt("someValue", -1);
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyUp(KeyCode.S))
                PlayerPrefs.Save();
            if (Input.GetKeyUp(KeyCode.L))
                PlayerPrefs.Load();
            if (Input.GetKeyUp(KeyCode.A))
            {
                someValue++;
                PlayerPrefs.SetInt("someValue", someValue);
                PlayURPlugin.Log("attempted to save someValue = " + someValue);
            }


            if (Input.GetKeyUp(KeyCode.G))
            {
                PlayURPlugin.Log("someValue = "+PlayerPrefs.GetInt("someValue", -1));
            }
        }
    }
}