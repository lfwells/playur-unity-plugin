using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlayUR
{
    public abstract class PlayURBehaviour : MonoBehaviour
    {
        #region Helper Properties
        /// <summary>
        /// Helper: The singleton instance of the <see cref="PlayURPlugin"/>.
        /// </summary>
        public PlayURPlugin PlayURPlugin => PlayURPlugin.instance;

        /// <summary>
        /// Helper: The current configuration of the plugin. Contains the current experiment, experiment group, and active elements and parameters.
        /// However it is recommended to use the <see cref="CurrentExperiment"/>, <see cref="CurrentExperimentGroup"/>, <see cref="CurrentElements"/> and <see cref="CurrentParameters"/> properties instead.
        /// </summary>
        public Configuration Configuration => PlayURPlugin.Configuration;

        /// <summary>Used to determine if the plugin is ready for normal use (i.e. the user has logged in, and a configuration has been obtained.</summary>
        /// <value><c>true</c> if the user has logged in, and the configuration has been set.
        /// <c>false</c> if the user has not logged in yet, or a configuration could not be found.</value>
        public bool IsReady => PlayURPlugin.IsReady;


        /// <summary>Currently running experiment.</summary>
        /// <exception cref="ConfigurationNotReadyException">thrown if configuration is not previously obtained</exception>
        public Experiment CurrentExperiment => PlayURPlugin.CurrentExperiment;


        /// <summary>Currently running experiment group.</summary>
        /// <exception cref="ConfigurationNotReadyException">thrown if configuration is not previously obtained</exception>
        public ExperimentGroup CurrentExperimentGroup => PlayURPlugin.CurrentExperimentGroup;

        /// <summary>Gets all enabled Game Elements from the current configuration.</summary>
        /// <returns>a list of the active Game Elements.</returns>
        /// <exception cref="ConfigurationNotReadyException">thrown if configuration is not previously obtained</exception>
        public List<Element> Elements => PlayURPlugin.ListElements();

        /// <summary>
        /// Return a list of all parameters defined in the <see cref="Configuration"/>.
        /// </summary>
        /// <returns>The parameters in Dictionary format, keys are parameter names, values are the associated values.</returns>
        /// <exception cref="ConfigurationNotReadyException"></exception>
        public Dictionary<string, string> Parameters => PlayURPlugin.ListParameters();
        #endregion

        #region Startup
        /// <summary>
        /// The standard Unity Start callback function.
        /// If overriding, must call base.Start() if you wish to use <see cref="OnReady"/>.
        /// </summary>
        public virtual void Start()
        {
            if (PlayURPlugin.available)
            {
                if (PlayURPlugin.IsReady)
                {
                    OnReady();
                }
                else
                {
                    PlayURPlugin.instance.OnReady.AddListener(OnReady);
                }
            }
        }

        /// <summary>
        /// Override this method to do something when the plugin is ready.
        /// Implementation is that this function is the registered callback to <see cref="PlayURPlugin.instance.OnReady"/>.
        /// This registration takes place in <see cref="Start"/>.
        /// </summary>
        public virtual void OnReady()
        {

        }
        #endregion
    }
}