// Taken from http://redframe-game.com/blog/global-managers-with-generic-singletons/

using UnityEngine;

namespace PlayUR
{ 
/* 
 * This class is a Singleton GameObject that will be lazily initialized when it is referenced for the first time.
 * It derives from MonoBehaviour allowing for all of the usual Unity systems to be used.
 * The GameObject is persistent and will not be destroyed when a new scene is loaded.
 * 
 * See the link above for more information and an explanation.
 * 
 * NOTE: A subclasses must pass in its own Type as the T parameter, this is so the singleton
 * can typecast the instance member variable to the corrent class.
 */
/// <summary>
///<para>This class is a Singleton GameObject that will be lazily initialized when it is referenced for the first time.</para>
///<para>It derives from MonoBehaviour allowing for all of the usual Unity systems to be used.</para>
///<para>The GameObject is persistent and will not be destroyed when a new scene is loaded.</para>
///
///<para>Subclasses represent a particular game manager (eg. a player manager).</para>
///
///<para>NOTE: A subclasses must pass in its own Type as the T parameter, this is so the singleton
///can typecast the instance member variable to the corrent class.</para>
/// </summary>
/// <typeparam name="T"></typeparam>
public class UnitySingletonPersistent <T> : UnitySingleton<T> where T : MonoBehaviour
{
    public virtual void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }
}
}