using UnityEngine;

namespace PlayUR.Samples
{
    public class PopupTest : MonoBehaviour
    {
        public Sprite sprite;
        public void DoPopup()
        {
            PlayURPlugin.instance.ShowCloseablePopup("Here is a test!", sprite);

            //PlayURPlugin.instance.MarkMTurkComplete();
        }
    }
}