using UnityEngine;

namespace Mkey
{
    public class ManualStopBehaviorRealistic : MonoBehaviour
    {
        public SlotController slotController;
        public SceneButton spinButton;
        public SceneButton stopButton;
        public bool infiniteSpin = false;

        void Start()
        {
            if (slotController)
            {
                slotController.StartSpinEvent += StartSpinHandler;
                slotController.EndSpinEvent += EndSpinHandler;
            }
            if (spinButton) spinButton.gameObject.SetActive(true);
        }

        private void OnDestroy()
        {
            if (slotController)
            {
                slotController.StartSpinEvent -= StartSpinHandler;
                slotController.EndSpinEvent -= EndSpinHandler;
            }
        }

        private void StartSpinHandler()
        {
            if (slotController.IsFreeSpin) return;
            if (spinButton && stopButton) { spinButton.gameObject.SetActive(false); stopButton.gameObject.SetActive(true); }
        }

        private void EndSpinHandler()
        {
            if (spinButton && stopButton) { spinButton.gameObject.SetActive(true); stopButton.gameObject.SetActive(false); }
        }

        public void SpinClickHandler()
        {
            if (isActiveAndEnabled && infiniteSpin && slotController) slotController.SetInfiniteSpinFlag();
        }
    }
}