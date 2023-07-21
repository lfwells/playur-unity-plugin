// Uses code taken from http://redframe-game.com/blog/global-managers-with-generic-singletons/
// The link provides an explanation for some of the code

using UnityEngine;

namespace PlayUR
{
    /*
     * This class creates a Singleton GameObject that will either be lazily initialized when it is referenced for the first time or,
     * grabbed from the scene if an instance already exists.
     * It derives from MonoBehaviour allowing for all of the usual Unity systems to be used.
     * The GameObject is persistent and will not be destroyed when a new scene is loaded.
     *
     * Subclasses represent a particular game manager (eg. a player manager).
     *
     * NOTE: A subclasses must pass in its own Type as the T parameter, this is so the singleton
     * can typecast the instance member variable to the corrent class.
     */

    /// <summary>
    ///<para>This class creates a Singleton GameObject that will either be lazily initialized when it is referenced for the first time or,
    ///grabbed from the scene if an instance already exists.</para>
    ///<para>It derives from MonoBehaviour allowing for all of the usual Unity systems to be used.</para>
    ///<para>The GameObject is NOT persistent and WILL be destroyed when a new scene is loaded.</para>
    ///
    ///<para>Subclasses represent a particular game manager (eg. a player manager).</para>
    ///
    ///<para>NOTE: A subclasses must pass in its own Type as the T parameter, this is so the singleton
    ///can typecast the instance member variable to the corrent class.</para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class UnitySingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        protected static T _instance;
        private static bool _isquitting = false;

        /// <summary>
        /// The current type that belongs to this singleton. Alias of <code>typeof(T)</code>.
        /// </summary>
        public static System.Type type { get { return typeof(T); } }

        /// <summary>
        /// Define what happens if another instance is attempted to be created.
        /// </summary>
        static bool AllowCreation(System.Type T) {
            if (T == typeof(PlayUR.Core.PlayURLoginCanvas)) return true;
            return false;
        }

        /// <summary>
        /// The singleton instance of the type. Will create a new object with type if it is not available within the scene.
        /// </summary>    
        public static T instance
        {
            get
            {

                //Util.Assert(Object.FindObjectsOfType<T>().Length <= 1); // there should not be more than one instance of any manager component

                if (!available)
                {
                    if (_isquitting)
                        Debug.LogError("Creating a new instance of " + type + " while a OnApplicationQuit has been detected.");

                    //We do not have one available, lets create it as a new gameobject.
                    if (Application.isPlaying && AllowCreation(typeof(T)))
                    {
                        GameObject obj = new GameObject(type + " Instance");
                        _instance = obj.AddComponent<T>();
                        Debug.LogError("Singleton " + type + " does not exist. A new instance has been created instead.", _instance);
                    }
                    else 
                    {
                        Debug.LogError("Attempted to create a new instance of " + type);
                    }
                }

                //Return the instance.
                return _instance;
            }

            set
            {
                Debug.LogError("Someone is trying to manually set the singleton for " + type, _instance);
                _instance = value;
            }
        }


        /// <summary>
        /// The singleton instance of the type. Similar to <see cref="instance"/>, however null will be returned if the instance does not exist instead of trying to create a new gameobject. It will still try to find the gameobject in the scene.
        /// </summary>
        public static T available
        {
            get
            {
                //We do not have a instance yet, try and find one in the scene.
                if (_instance == null)
                {
                    //Get all objects of the type
                    var objects = FindObjectsOfType<T>();

                    //There is no object of this type, so return null.
                    if (objects.Length == 0)
                    {
                        var disableds = (T[])Resources.FindObjectsOfTypeAll(type);

                        if (disableds.Length == 0)
                        {
                            _instance = null;
                            //Debug.LogError("Singleton " + typeof(T).ToString() + " has zero instances. A new instance will have to be created.", _instance);
                        }
                        else
                        {
                            foreach (var disabled in disableds)
                            {
                                if (disabled.enabled && !disabled.isActiveAndEnabled)
                                {
                                    _instance = disabled;
                                    Debug.LogError("Singleton " + type + " connected to disabled GameObject. This is a very BAD idea!", _instance);
                                    //Debug.Break();
                                }
                            }
                        }
                    }
                    else
                    {
                        //assign the object to the first element
                        _instance = objects[0];

                        //Make sure we only got one result
                        if (objects.Length > 1)
                        {
                            Debug.LogError("Singleton " + type + " has multiple instances. This can cause issues further down the line!", _instance);
                            Debug.Break();
                        }
                    }
                }

                //Return what ever we found, even if we found nothing.
                return _instance;
            }
        }

        /// <summary>
        /// Has the instance been assigned? Will calling <see cref="instance"/> or <see cref="available"/> call a FindObjectOfType?
        /// <para>
        /// See also <see cref="available"/> for a nullable instance that does not create any objects.
        /// </para>
        /// </summary>
        /// <returns>true if the instance exists in the world.</returns>
        [System.Obsolete("Functionality not obvious. Use referenced or exists property instead.")]
        public static bool InstanceExists()
        {
            return referenced;
        }

        /// <summary>
        /// Has the instance been assigned / referenced? Will calling <see cref="instance"/> or <see cref="available"/> call a FindObjectOfType? Use this to check the validity of the instance before creating one or even locating one.
        /// <para>
        /// See also <see cref="available"/> for a nullable instance that does not create any objects.
        /// </para>
        /// </summary>
        public static bool referenced { get { return _instance != null; } }

        /// <summary>
        /// Does an instance of this singleton exist in the world? Use this to check the validity of the instance before creating one.  Short hand for available != null.
        /// <para>
        /// See also <see cref="available"/> for a nullable instance that does not create any objects.
        /// </para>
        /// </summary>
        public static bool exists { get { return available != null; } }

        /// <summary>
        /// When Unity quits, it destroys objects in a random order.
        /// In principle, a Singleton is only destroyed when application quits.
        /// If any script calls Instance after it have been destroyed, 
        ///   it will create a buggy ghost object that will stay on the Editor scene
        ///   even after stopping playing the Application. Really bad!
        /// So, this was made to be sure we're not creating that buggy ghost object.
        /// </summary>
        public virtual void OnApplicationQuit()
        {
            _isquitting = true;
        }
    }
}