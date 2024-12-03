using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using UnityEngine.EventSystems;

namespace Mkey
{
    public enum SpinTypes { Single, Auto }
    public class StartButtonBehavior : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Text autoText;
        private SpinTypes spinType = SpinTypes.Single;
        private SpinTypes SpinType
        {
            get { return spinType; }
            set { spinType = value; ChangeStateDelegate?.Invoke(value == SpinTypes.Auto); SetButtonText(); }
        }

        public Action<bool> ChangeStateDelegate;
        public Action ClickDelegate;

        bool up = true;
        float downTime = 0;

        public void OnPointerDown(PointerEventData eventData)
        {
            up = false;
         //   Debug.Log(gameObject.name + " Was down.");
            if (SpinType == SpinTypes.Auto)
            {
                SpinType = SpinTypes.Single;
                return;
            }
            downTime = Time.time;
            StartCoroutine(CheckAuto());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            up = true;
          // Debug.Log(this.gameObject.name + " Was exit.");
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            up = true;
            Debug.Log(gameObject.name + " Was up. SpinType: " + SpinType);
            ClickDelegate?.Invoke();
        }

        IEnumerator CheckAuto()
        {
            bool cancel = false;
            WaitForEndOfFrame wef = new WaitForEndOfFrame();
            float dTime;
            while (!up && !cancel)
            {
                dTime = Time.time - downTime;
                if (dTime > 2.0f)
                {
                    SpinType = SpinTypes.Auto;
                    cancel = true;
                }
                yield return wef;
            }
        }

        internal void ResetAuto()
        {
            SpinType = SpinTypes.Single;
        }

        private void SetButtonText()
        {
          if(autoText)  autoText.text = (SpinType == SpinTypes.Single) ? "Hold to AutoSpin" : "AUTO";
        }
    }
}