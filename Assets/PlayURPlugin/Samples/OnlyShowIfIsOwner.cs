using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayUR
{
    /// <summary>
    /// A useful MonoBehaviour which (on startup) sets a gameobject active or inactive depending on if the current user is an admin/owner of the game.
    /// </summary>
    public class OnlyShowIfIsOwner : MonoBehaviour
    {
        void Start()
        {
            gameObject.SetActive(false);
            PlayURPlugin.instance.OnReady.AddListener(() =>
            {
                if (PlayURPlugin.instance.user.IsGameOwner)
                    gameObject.SetActive(true);
                else
                    gameObject.SetActive(false);
            });
        }
    }
}