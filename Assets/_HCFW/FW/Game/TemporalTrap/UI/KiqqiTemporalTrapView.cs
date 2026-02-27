
using UnityEngine;
using UnityEngine.UI;

namespace Kiqqi.Framework
{
    /// <summary>
    /// Main gameplay view for TemporalTrap.
    /// Displays clock hand, handles OK/NOK button input.
    /// </summary>
    public class KiqqiTemporalTrapView : KiqqiGameViewBase
    {
        [Header("Clock Visuals")]
        public RectTransform handTransform;
        public int totalPositions = 12;

        [Header("Controls")]
        public Button okButton;
        public Button nokButton;

        private KiqqiTemporalTrapManager manager;

        public void BindManager(KiqqiTemporalTrapManager m) => manager = m;

        public override void OnShow()
        {
            base.OnShow();
            ShowButtons(false);

            if (okButton)
            {
                okButton.onClick.RemoveAllListeners();
                okButton.onClick.AddListener(() => manager?.OnOkPressed());
            }
            if (nokButton)
            {
                nokButton.onClick.RemoveAllListeners();
                nokButton.onClick.AddListener(() => manager?.OnNokPressed());
            }
        }

        protected override void OnCountdownFinished()
        {
            base.OnCountdownFinished();

            var gm = KiqqiAppManager.Instance.Game;
            if (gm?.currentMiniGame is KiqqiTemporalTrapManager m)
            {
                manager = m;
                m.StartMiniGame();
            }
        }

        public void UpdateClockVisual(int position)
        {
            if (!handTransform) return;
            float anglePerStep = 360f / totalPositions;
            float angle = -anglePerStep * position;
            handTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        public void ShowButtons(bool state)
        {
            if (okButton) okButton.interactable = state;
            if (nokButton) nokButton.interactable = state;
        }

        protected override void OnTimeUp()
        {
            base.OnTimeUp();
            manager?.NotifyTimeUp();
        }

        public void RefreshScoreUI()
        {
            // Call the protected base method from KiqqiGameViewBase safely
            UpdateScoreUI();
        }

    }
}
