using System.Collections;
using PlayUR.Core;
using PlayUR.DetachedMode;
using System.Linq;
using System.Collections.Generic;

namespace PlayUR
{
    public partial class PlayURPlugin : UnitySingletonPersistent<PlayURPlugin>
    {
        public bool IsDetachedMode => Settings.detachedMode;
        public PlayURConfigurationObject DetchedConfiguration => Settings.detachedModeConfiguration;
        DetachedModeProxyHandler DetachedModeProxy => new();

        public class DetachedModeProxyHandler
        {
            public IEnumerator GetConfiguration(PlayURPlugin plugin)
            {
                var c = new Configuration
                {
                    experiment = plugin.DetchedConfiguration.experiment,
                    experimentID = (int)plugin.DetchedConfiguration.experiment,
                    experimentGroup = plugin.DetchedConfiguration.experimentGroup,
                    experimentGroupID = (int)plugin.DetchedConfiguration.experimentGroup,
                    parameters = new Dictionary<string,string>(plugin.DetchedConfiguration.parameterValues.Select(p => new KeyValuePair<string,string>(p.key, p.value))),
                };
                plugin.configuration = c;
                yield return 0;
            }
        }
    }
}
