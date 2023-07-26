using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if USE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PlayUR
{
    /// <summary>
    /// A high-score table which can be used to display a leaderboard.
    /// Data is stored using PlayUR
    /// This MonoBehaviour should be placed on a high-score table prefab. This script is already included in the provided highscore table prefab in PlayURPlugin/HighScores.
    /// </summary>
    public class HighScoreTable : MonoBehaviour
    {
        /// <summary>
        /// The id of the leaderboard to handle all data, this id is unique within a game, but doesn't need to be unique across the entire PlayUR platform.
        /// </summary>
        public string leaderboardID;

        //TODO: should these be private serialized, so as to not be documented?
        [HideInInspector] public int highlightRowID = -1;
        [HideInInspector] public float height = -1;

        /// <summary>
        /// The configuration for this leaderboard, this is used to determine how to display the data.
        /// </summary>
        public PlayURPlugin.LeaderboardConfiguration configuration;

        /// <summary>
        /// Should we automatically initialise the leaderboard on the Unity Start message? If false, you will need to call <see cref="Init"/> manually.
        /// </summary>
        public bool initOnStart = false;

        /// <summary>
        /// Should we show a close button on the leaderboard? If true, the leaderboard will close when the close button is clicked.
        /// </summary>
        public bool showCloseButton = true;

        /// <summary>
        /// What prefab should be instantiated for each row of the leaderboard? This prefab should have a <see cref="HighScoreRow"/> component on it.
        /// The provided prefab in PlayURPlugin/HighScores can be linked for this.
        /// </summary>
        public GameObject rowPrefab;

        protected ScrollRect scrollRect;
        protected RectTransform contentPanel;


        [System.Serializable]  
        /// <summary>
        /// Used internally by PlayUR.
        /// </summary>
        public class TableData
        {
            [System.Serializable]  
            public class RowData
            {
                public int id;
                public List<string> cells = new List<string>();
            }
            public List<RowData> rows = new List<RowData>();

            public static TableData FromServer(JSONNode data, PlayURPlugin.LeaderboardConfiguration configuration)
            {
                var table = new TableData();
                foreach (var rowData in data["records"].Values)
                {
                    var row = new TableData.RowData { id = int.Parse(rowData["id"]) };

                    var user = rowData["user"];
                    var name = "";
                    switch (configuration.nameDisplayType)
                    {
                        case PlayURPlugin.LeaderboardConfiguration.NameDisplayType.FirstName:
                            name = user["fname"]; 
                            break; 
                        
                        case PlayURPlugin.LeaderboardConfiguration.NameDisplayType.Username:
                            name = user["username"]; 
                            break;
                            
                        case PlayURPlugin.LeaderboardConfiguration.NameDisplayType.CustomName:
                            name = string.IsNullOrEmpty(rowData["customName"]) ? configuration.anonymousCustomNameValue : rowData["customName"].Value; 
                            break;
                    }
                    row.cells.Add(name);

                    row.cells.Add(rowData["score"]);
                    var extra = rowData["extra"].Value;
                    if (!string.IsNullOrEmpty(extra))
                    {
                        foreach (var extraData in extra.Split(','))
                        {
                            row.cells.Add(extraData);
                        }
                    }

                    table.rows.Add(row);
                }
                return table;
            }
        }  
        public TableData fakeData;

        public TextOrTMP title;
        public RectTransform rowsParent;
        public GameObject loadingObject;
        public GameObject closeButton;

        public bool autoHeight = true;
        public KeyCode useKeyCodeForClose = KeyCode.None;
        
#if USE_INPUT_SYSTEM
        private Key useKeyForClose;
#endif

        bool loading = true;

        public delegate void CloseCallback();
        public CloseCallback closeCallback;

        // Start is called before the first frame update
        void Start()
        {
#if USE_INPUT_SYSTEM
            var keyCodeName = useKeyCodeForClose.ToString();
            if (!System.Enum.TryParse<Key>(keyCodeName, out useKeyForClose))
                Debug.LogError("Failed to find appropriate key for closing. Could not convert " + keyCodeName);
#endif

            scrollRect = GetComponentInChildren<ScrollRect>();
            contentPanel = rowsParent;

            if (height == -1)
                height = Mathf.Min(800, (GetComponentInParent<Canvas>().transform as RectTransform).sizeDelta.y * 0.9f);

            if (autoHeight)
            {
                var s = (transform as RectTransform).sizeDelta;
                s.y = height;
                (transform as RectTransform).sizeDelta = s;
            }

            if (showCloseButton == false)
            {
                closeButton.SetActive(false);
                (scrollRect.transform as RectTransform).offsetMin = new Vector2((scrollRect.transform as RectTransform).offsetMin.x, 0);
            }

            rowsParent.gameObject.SetActive(false);
            if (PlayURPlugin.instance.IsReady && initOnStart)
                Init();
        }
        public void Init()
        {
            title.text = configuration.title;

            loading = true;
            loadingObject.SetActive(true);
            rowsParent.gameObject.SetActive(false);            
                
            PlayURPlugin.instance.GetLeaderboardEntries(leaderboardID, configuration, (success, data) =>
            {
                if (success)
                {
                    var fakeData = TableData.FromServer(data, configuration);
                    ClearTable();
                    CreateTable(fakeData);
                    //CreateTable(fakeData);

                    loading = false;
                    loadingObject.SetActive(false);
                    rowsParent.gameObject.SetActive(true);
                }
            });
        }

        void CreateTable(TableData table)
        {
            RectTransform highlightedRowTransform = null;
            foreach (var row in table.rows)
            {
                bool isHighlightedRow = row.id == highlightRowID;
                bool editRow = isHighlightedRow && configuration.nameDisplayType == PlayURPlugin.LeaderboardConfiguration.NameDisplayType.CustomName;

                var go = Instantiate(rowPrefab, rowsParent ?? transform);
                var rowScript = go.GetComponent<HighScoreTableRow>();
                rowScript.CreateRow(row, configuration, isHighlightedRow, editRow);
                if (isHighlightedRow)
                {
                    highlightedRowTransform = go.GetComponent<RectTransform>();
                }
            }
            if (highlightedRowTransform != null && configuration.autoScrollToHighlightedRow)
            {
                SnapToRow(highlightedRowTransform);
            }
        }

        public void ClearTable()
        {
            for (var i = rowsParent.transform.childCount- 1; i >= 0; i--)
                Destroy(rowsParent.transform.GetChild(i).gameObject);
        }

        public void Close()
        {
            if (inputText != null)
            {
                UpdateCustomName();
            }
            Destroy(gameObject);

            if (closeCallback != null)
                closeCallback();
        }

        void SnapToRow(RectTransform target)
        {
            StartCoroutine(WaitAFrameAndSnapToRow(target));
        }
        IEnumerator WaitAFrameAndSnapToRow(RectTransform target)
        {
            Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();

            if (target == null) yield break;

            contentPanel.anchoredPosition =
                (Vector2)scrollRect.transform.InverseTransformPoint(contentPanel.position)
                - (Vector2)scrollRect.transform.InverseTransformPoint(target.position)
                - (scrollRect.transform as RectTransform).rect.height * Vector2.up;

            contentPanel.offsetMin = new Vector2(0, contentPanel.offsetMin.y);
            contentPanel.offsetMax = new Vector2(0, contentPanel.offsetMax.y);
            
            //also attempt to highlight the input text if there is one
            inputText = GetComponentInChildren<InputField>();
            if (inputText)
            {
                inputText.Select();
            }
        }

        void Update()
        {
            if (loading)
            {
                loadingObject.transform.Rotate(0,0,-180f * Time.deltaTime);
            }
            else
            {
#if USE_INPUT_SYSTEM
                if (Keyboard.current[useKeyForClose].wasPressedThisFrame)
#else
                if (Input.GetKeyDown(useKeyCodeForClose))
#endif
                {
                    Close();
                }
            }
        }

        InputField inputText;
        void UpdateCustomName()
        {
            loading = true;
            var name = string.IsNullOrEmpty(inputText.text) ? "Anonymous" : inputText.text;
            PlayURPlugin.instance.UpdateLeaderboardEntryName(highlightRowID, name, callback:delegate(bool succ, JSONNode result)
            {
                loading = false;
            });
        }
    }
}
