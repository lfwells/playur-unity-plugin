using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayUR;

namespace PlayUR.Samples{

    public class TestHighScoreTable : MonoBehaviour
    {
        public string score { get; set; } = "5";

        PlayURPlugin.LeaderboardConfiguration config;
        void Start()
        {
            config = new PlayURPlugin.LeaderboardConfiguration
            {
                //descending = false,
                title = "Whatevers",
                //oneEntryPerPlayer = true
                //maxItems = 5,
                nameDisplayType = PlayURPlugin.LeaderboardConfiguration.NameDisplayType.CustomName,
                //forceNameEntryDefaultValue = "Fred"
            };
        }
        public void Test()
        {
            //callback approach
            PlayURPlugin.instance.AddLeaderboardEntryAndShowHighScoreTable("test", int.Parse(score), config, closeCallback:() => 
            { 
                print("done!"); 
            });
        }

        //coroutine approaches
        public void TestRoutine()
        {
            StartCoroutine(Routine());
        }
        IEnumerator Routine()
        {
            yield return StartCoroutine(PlayURPlugin.instance.AddLeaderboardEntryAndShowHighScoreTableRoutine("test", int.Parse(score), config, height: 200, keyCodeForClose:KeyCode.Escape));

            print("done routine!");
        }

        public void TestDisplay3Seconds()
        {
            StartCoroutine(Routine2());
        }
        IEnumerator Routine2()
        {
            yield return StartCoroutine(PlayURPlugin.instance.ShowHighScoreTableFor(seconds:3, "test", config, showCloseButton:false));

            print("done routine!");
        }
        public void TestDisplayUntilClose()
        {
            StartCoroutine(Routine3());
        }
        IEnumerator Routine3()
        {
            yield return StartCoroutine(PlayURPlugin.instance.ShowHighScoreTableRoutine("test", config, keyCodeForClose:KeyCode.Escape));

            print("done routine!");
        }
    }
}