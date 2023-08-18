using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayUR
{
    /// <summary>
    /// A useful MonoBehaviour which (on startup) sets a gameobject active or inactive depending on if the current user is an admin/owner of the game.
    /// </summary>
    public class OnlyShowIfIsOwner : PlayURBehaviour
    {
        public override void Start()
        {
            gameObject.SetActive(false);

            base.Start();
        }
        public override void OnReady()
        {
            gameObject.SetActive(PlayURPlugin.instance.user.IsGameOwner);
        }
    }
}