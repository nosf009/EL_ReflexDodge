// Assets/_HCFW/FW/Games/KiqqiTicTacToeCell.cs
using UnityEngine;
using UnityEngine.UI;

namespace Kiqqi.Framework
{
    [RequireComponent(typeof(Button))]
    public class KiqqiTicTacToeCell : MonoBehaviour
    {
        [HideInInspector] public int col;
        [HideInInspector] public int row;

        private Button button;
        private KiqqiTicTacToeManager manager;

        public void Init(KiqqiTicTacToeManager mgr, int c, int r)
        {
            manager = mgr;
            col = c;
            row = r;

            if (!button) button = GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                manager.HandleCellPressed(col, row);
            });
        }
    }
}
