using UnityEngine;
using MkeyFW;

namespace Mkey
{
    public class SlotEvents : MonoBehaviour
    {
        [SerializeField]
        private PopUpsController chestsPrefab;
        public FortuneWheelInstantiator Instantiator;
        public bool autoStartMiniGame = true;
        [SerializeField]
        private AudioClip winCoinsSound;
        [SerializeField]
        private AudioClip bonusSound;
        public static SlotEvents Instance;

        public bool MiniGameStarted { get { return (Instantiator && Instantiator.MiniGame); } }

        #region temp vars
        private SlotPlayer MPlayer { get { return SlotPlayer.Instance; } }
        private SoundMaster MSound { get { return SoundMaster.Instance; } }
        private GuiController MGUI { get { return GuiController.Instance; } }
        #endregion temp vars

        private void Awake()
        {
            Instance = this; 
        }

        public void Bonus_5()
        {
            Debug.Log("-------------- Bonus 5 win --------------------");
            MSound.PlayClip(0, bonusSound);
            Instantiator.Create(autoStartMiniGame);
            Instantiator.MiniGame.SetBlocked(autoStartMiniGame, autoStartMiniGame);
            Instantiator.SpinResultEvent += (coins, isBigWin) => { MPlayer.AddCoins(coins); };
        }

        public void Scatter_5()
        {
            Debug.Log("-------------- Scatter 5 win --------------------");
            MPlayer.AddLevelProgress(100f);
            MSound.PlayClip(0, winCoinsSound);
        }

        public void Scatter_4()
        {
            Debug.Log("-------------- Scatter 4 win --------------------");
            MGUI.ShowPopUp(chestsPrefab);
        }

        public void LineEvent_AAA()
        {
            Debug.Log("-------------- AAA win --------------------");
        }
    }
}