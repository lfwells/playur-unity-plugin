using UnityEngine;

namespace PlayUR.Samples
{
    /// <summary>
	/// Sample that shows how to open up a popup in the bottom corner with custom text and an image.
	/// Useful for notifications, and custom achievement systems.
	/// </summary>
    public class PopupSample : MonoBehaviour
    {
        public Sprite sprite;
        public void DoPopup()
        {
            PlayURPlugin.instance.ShowCloseablePopup("Here is a test!", sprite);
        }
    }
}