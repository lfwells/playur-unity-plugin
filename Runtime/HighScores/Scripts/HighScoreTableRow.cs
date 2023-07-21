using UnityEngine;

namespace PlayUR
{
    /// <summary>
    /// Represents a single row of a highscore table. This is a container for the cells.
    /// This MonoBehaviour should be placed on a high-score table row prefab. This script is already included in the provided highscore table row prefab in PlayURPlugin/HighScores.
    /// </summary>
    public class HighScoreTableRow : MonoBehaviour
    {
        /// <summary>
        /// What prefab should be instantiated for each cell of this row? A cell is a column of the row, i.e. the name, and the score value.
        /// The prfab should have a HighScoreTableCell script on it.
        /// The provided prefab in PlayURPlugin/HighScores is may be used for this.
        /// </summary>
        public GameObject cellPrefab;

        /// <summary>
        /// What prefab should be instantiated for an editable cell of this row? Used for entering in a new high score entry. 
        /// </summary>
        public GameObject editCellPrefab;

        /// <summary>
        /// The parent transform to instantiate the cells into. If null, will use the transform of this GameObject.
        /// This is already set on the provided prefab.
        /// </summary>
        public Transform cellsParent;

        /// <summary>
        /// Represents the visuals to be enabled for if this row is "highlighted" (i.e. it is a newly created entry).
        /// This is already set on the provided prefab.
        /// </summary>
        public GameObject highlightObject;

        /// <summary>
        /// Used internally by PlayUR.
        /// </summary>
        public void CreateRow(HighScoreTable.TableData.RowData row, PlayUR.PlayURPlugin.LeaderboardConfiguration configuration, bool highlight = false, bool edit = false)
        {
            for (var i = cellsParent.transform.childCount- 1; i >= 0; i--)
            {
                var g = cellsParent.transform.GetChild(i).gameObject;
                if (g != highlightObject)
                    Destroy(g);
            }
            
            var j = 0;
            foreach (var cell in row.cells)
            {
                var nameEntry = j == 0 && edit;
                var isValue = j == 1;
                var prefab = nameEntry ? editCellPrefab : cellPrefab;

                var go = Instantiate(prefab, cellsParent ?? transform);
                var cellScript = go.GetComponent<HighScoreTableCell>();
                cellScript.CreateCell(cell, configuration, nameEntry, isValue);

                j++;
            }

            highlightObject.SetActive(highlight);
        }
    }
}
