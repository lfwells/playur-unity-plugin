using UnityEngine.Events;

namespace PlayUR
{
    /// <summary>
    /// Internal class to wrap up UnityEvent so that we can handle AddListener when the plugin is already ready.
    /// </summary>
    public class PlayURReadyEvent : UnityEvent
    {
        public new void AddListener(UnityAction action)
        {
            base.AddListener(action);

            if (PlayURPlugin.IsReady)
                action.Invoke();
        }
    }
}