using UnityEngine;

namespace PlayUR.Samples
{
    public class LogoutSample : MonoBehaviour
    {
        public void Logout()
        {
            PlayURPlugin.instance.Logout();
        }
    }
}
