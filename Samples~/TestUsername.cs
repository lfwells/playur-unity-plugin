using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PlayUR.Samples
{
    /// <summary>
    /// Simple MonoBehaviour which displays the username (from <see cref="PlayURPlugin.instance.user.name"/>) on a <see cref="Text"/>.
    /// </summary>
    public class TestUsername : MonoBehaviour
    {
        void Start()
        {
            GetComponent<Text>().text = PlayURPlugin.instance.user.name;
        }
    }
}