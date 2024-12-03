using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MkeyFW
{
	public class WheelStarter : MonoBehaviour
	{
        [SerializeField]
        private WheelController wheelController;

        #region temp vars
        private Mkey.SlotPlayer MPlayer { get { return Mkey.SlotPlayer.Instance; } }
        #endregion temp vars

        #region regular
        private void Start()
		{
            wheelController.SetBlocked(false, false);
            wheelController.SpinResultEvent = (coins, isBigWin) => 
            {
                MPlayer.AddCoins(coins);
                wheelController.SetBlocked(false, false);
            };
        }
		#endregion regular
	}
}
