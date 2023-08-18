using TMPro;
using PlayUR;

/// <summary>
/// Sample class that shows how to read in the current user, and also do different behaviour if the player is an owner.
/// Also shows how to get the build id.
/// </summary>
public class ReadUserAndBuildInfo : PlayURBehaviour
{
    public TMP_Text txtUsername, txtBuild;

    public override void OnReady()
    {
        txtUsername.text = PlayURPlugin.instance.user.name+"\n"+PlayURPlugin.browserInfo;
        txtBuild.text = $"{PlayURPlugin.instance.CurrentBuildID} (branch {PlayURPlugin.instance.CurrentBuildBranch})";
    }
}
