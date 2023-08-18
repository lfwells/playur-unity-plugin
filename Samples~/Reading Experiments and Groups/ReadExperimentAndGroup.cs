using TMPro;
using PlayUR;

/// <summary>
/// Sample class that shows how to read what the current experiment and group is. This information comes in as an enum.
/// </summary>
public class ReadExperimentAndGroup : PlayURBehaviour
{
    public TMP_Text txtExperiment, txtGroup;

    public override void OnReady()
    {
        txtExperiment.text = PlayURPlugin.instance.CurrentExperiment.ToString();
        txtGroup.text = PlayURPlugin.instance.CurrentExperimentGroup.ToString();
    }
}
