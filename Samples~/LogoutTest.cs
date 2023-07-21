using UnityEngine;

namespace PlayUR.Samples
{
    public class LogoutTest : MonoBehaviour
    {
        public void Logout()
        {
            PlayURPlugin.instance.Logout();
        }
    }
}
