using UnityEngine;
using UnityEngine.UI;

namespace Kiqqi.Framework
{
    [RequireComponent(typeof(Button))]
    public class KiqqiGridSwipeCell : MonoBehaviour
    {
        public int col;
        public int row;

        private Button button;
        private KiqqiGridSwipeManager manager;

        public void Init(KiqqiGridSwipeManager mgr, int c, int r)
        {
            manager = mgr;
            col = c;
            row = r;

            if (!button) button = GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => manager.HandleCellPressed(col, row));
        }
    }
}
