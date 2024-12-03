using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine.Events;

namespace Mkey
{
    public enum WinLineFlashing {All, Sequenced, None}
    public enum JackPotType { None, Mini, Maxi, Mega }
    public enum JackPotIncType { Const, Percent } // add const value or percent of start value

    public class SlotController : MonoBehaviour
    {
        #region main reference
        [SerializeField]
        private SlotMenuController menuController;
        [SerializeField]
        private SlotControls controls;
        [SerializeField]
        private WinController winController;
        [SerializeField]
        private AudioClip spinSound;
        [SerializeField]
        private AudioClip looseSound;
        [SerializeField]
        private AudioClip winCoinsSound;
        [SerializeField]
        private AudioClip winFreeSpinSound;
        #endregion main reference

        #region icons
        [SerializeField, ArrayElementTitle("iconSprite"), NonReorderable]
        public SlotIcon[] slotIcons;

        [Space(8)]
        [SerializeField]
        public WinSymbolBehavior[] winSymbolBehaviors;
        #endregion icons

        #region payTable
        public List<PayLine> payTable;
        internal List<PayLine> payTableFull; // extended  if useWild
        #endregion payTable

        #region special major
        public int scatter_id;
        public int wild_id;
        public bool useWild;
        public bool useScatter;
        #endregion special major

        #region scatter paytable
        public List<ScatterPay> scatterPayTable;
        #endregion scatter paytable

        #region prefabs
        public GameObject tilePrefab;
        public GameObject particlesStars;
        [SerializeField]
        private WarningMessController BigWinPrefab;
        #endregion prefabs

        #region slotGroups
        [NonReorderable]
        public SlotGroupBehavior[] slotGroupsBeh;
        #endregion slotGroups

        #region tweenTargets
        public Transform bottomJumpTarget;
        public Transform topJumpTarget;
        #endregion tweenTargets

        #region spin options
        [SerializeField]
        private EaseAnim inRotType = EaseAnim.EaseLinear; // in rotation part
        [SerializeField]
        [Tooltip("Time in rotation part, 0-1 sec")]
        private float inRotTime = 0.3f;
        [SerializeField]
        [Tooltip("In rotation part angle, 0-10 deg")]
        private float inRotAngle = 7;

        [Space(16, order = 0)]
        [SerializeField]
        private EaseAnim outRotType = EaseAnim.EaseLinear;   // out rotation part
        [SerializeField]
        [Tooltip("Time out rotation part, 0-1 sec")]
        private float outRotTime = 0.3f;
        [SerializeField]
        [Tooltip("Out rotation part angle, 0-10 deg")]
        private float outRotAngle = 7;

        [Space(16, order = 0)]
        [SerializeField]
        private EaseAnim mainRotateType = EaseAnim.EaseLinear;   // main rotation part
        [SerializeField]
        [Tooltip("Time main rotation part, sec")]
        private float mainRotateTime = 4f;
        [Tooltip("min 0% - max 20%, change rotateTime")]
        [SerializeField]
        private int mainRotateTimeRandomize = 10;
        #endregion spin options

        #region options
        public WinLineFlashing winLineFlashing = WinLineFlashing.Sequenced;
        public bool winSymbolParticles = true;
        public RNGType RandomGenerator = RNGType.Unity;
        [SerializeField]
        [Tooltip("Multiply win coins by bet multiplier")]
        public bool useLineBetMultiplier = true;
        [SerializeField]
        [Tooltip("Multiply win spins by bet multiplier")]
        public bool useLineBetFreeSpinMultiplier = true;
        [SerializeField]
        [Tooltip("Debug to console predicted symbols")]
        private bool debugPredictSymbols = false;
        #endregion options 

        #region jack pots
        [Space(8)]
        public int jp_symbol_id=-1;
        public bool useMiniJacPot = false;
        [Tooltip("Count identical symbols on screen")]
        public int miniJackPotCount = 7;
        public bool useMaxiJacPot = false;
        [Tooltip("Count identical symbols on screen")]
        public int maxiJackPotCount = 9;
        public bool useMegaJacPot = false;
        [Tooltip("Count identical symbols on screen")]
        public int megaJackPotCount = 10;
        private JackPotIncType jackPotIncType = JackPotIncType.Const;
        public int jackPotIncValue = 1;
        public JackPotController jpController;
        #endregion jack pots 

        #region levelprogress
        [SerializeField]
        [Tooltip("Multiply level progress by bet multiplier")]
        public bool useLineBetProgressMultiplier = true;
        [SerializeField]
        [Tooltip("Player level progress for loose spin")]
        public float loseSpinLevelProgress = 0.5f;
        [SerializeField]
        [Tooltip("Player level progress for win spin per win line")]
        public float winSpinLevelProgress = 2.0f;
        #endregion level progress

        #region temp vars
        private int slotTilesCount = 30;
        private WaitForSeconds wfs1_0;
        private WaitForSeconds wfs0_2;
        private WaitForSeconds wfs0_1;
        private RNG rng; // random numbers generator

        private uint spinCount = 0;
        private bool slotsRunned = false;
        private bool playFreeSpins = false;
        private GameObject miniGame;
        private bool logStatistic = true;

        private SoundMaster MSound { get { return SoundMaster.Instance; } }
        private SlotPlayer MPlayer { get { return SlotPlayer.Instance; } }
        private GuiController MGUI {get { return GuiController.Instance; } }
        private JackPotType jackPotType = JackPotType.None;
        private int jackPotWinCoins = 0;
        private bool infiniteSpinFlag = false;
        private bool sideWinFlag = false; // can be set by some event related to the game situation. you need to raise this flag in a game event: EndWinSearchEvent
        #endregion temp vars

        #region events
        public Action SpinPressEvent;           // 1.10 - 266 SpinPress()

        public Action StartFreeGamesEvent;      // 2.10 - 311 RunSlots()
        public Action StartSpinEvent;           // 2.20 - 333 RunSlotsAsyncC()
        public Action EndSpinEvent;             // 2.30 - 356 RunSlotsAsyncC()

        public Action BeginWinCalcEvent;        // 3.10 - 360 RunSlotsAsyncC()
        public Action EndWinSearchEvent;
        public Action AnyWinEvent;              // 3.20 - if win 371 RunSlotsAsyncC()
        public Action<int> WinCoinsEvent;       // 3.22 - if win
        public Action<int> WinSpinsEvent;       // 3.24 - if win
        public Action EndWinCalcEvent;          // 3.30 - 433, 477 RunSlotsAsyncC()

        public Action LooseEvent;               // 3.40 - 489 RunSlotsAsyncC()
        public Action EndFreeGamesEvent;        // 3.50 - 491 RunSlotsAsyncC() - перенес

        public Action StartWinShowEvent;
        public Action EndWinShowEvent_1;
        public Action EndWinShowEvent_2;
        public Action EndWinShowEvent_3;
        #endregion events

        #region dev
        public string payTableJsonString;
        #endregion dev

        public static SlotController CurrentSlot { get; private set; }
        public bool IsFreeSpin { get; private set; }

        public bool useWildInFirstPosition = false;

        #region regular
        private void OnValidate()
        {
            Validate();
        }

        void Validate()
        {
            mainRotateTimeRandomize = (int)Mathf.Clamp(mainRotateTimeRandomize, 0, 20);

            inRotTime = Mathf.Clamp(inRotTime, 0, 1f);
            inRotAngle = Mathf.Clamp(inRotAngle, 0, 10);

            outRotTime = Mathf.Clamp(outRotTime, 0, 1f);
            outRotAngle = Mathf.Clamp(outRotAngle, 0, 10);
            jackPotIncValue = Mathf.Max(0, jackPotIncValue);

            miniJackPotCount = Mathf.Max(1, miniJackPotCount);
            maxiJackPotCount = Mathf.Max( (useMiniJacPot) ? miniJackPotCount + 1 : 1, maxiJackPotCount);
            megaJackPotCount = Mathf.Max((useMaxiJacPot) ? maxiJackPotCount + 1 : 1, megaJackPotCount);
            if (scatterPayTable != null)
            {
                foreach (var item in scatterPayTable)
                {
                    if (item!=null)
                    {
                        item.payMult = Mathf.Max(1, item.payMult);
                    }
                }
            }
        }
      
        void Start()
        {
            wfs1_0 = new WaitForSeconds(1.0f);
            wfs0_2 = new WaitForSeconds(0.2f);
            wfs0_1 = new WaitForSeconds(0.1f);

            // create reels
            int slotsGrCount = slotGroupsBeh.Length;
            ReelData[] reelsData = new ReelData[slotsGrCount];
            ReelData reelData;
            int i = 0;
            foreach (SlotGroupBehavior sGB in slotGroupsBeh)
            {
                reelData = new ReelData(sGB.symbOrder);
                reelsData[i++] = reelData;
                sGB.CreateSlotCylinder(slotIcons, slotTilesCount, tilePrefab);
            }

            CreateFullPaytable();
            rng = new RNG(RNGType.Unity, reelsData);
            SetInputActivity(true);
            CurrentSlot = this;
        }

        void Update()
        {
            rng.Update();
        }

        private void OnDestroy()
        {
           
        }
        #endregion regular

        /// <summary>
        /// Run slots when you press the button
        /// </summary>
        internal void SpinPress()
        {
            SpinPressEvent?.Invoke();
            RunSlots();
        }

        internal void SetInfiniteSpinFlag()
        {
            infiniteSpinFlag = true;
        }

        internal void ForceStop()
        {
            foreach (var item in slotGroupsBeh)
            {
                item.ForceStopInfiniteSpin();
            }
        }

        #region nudge
        private int nudge = -1;
        private bool nudgeUp = false;
        public bool IsNudge => nudge >= 0;
        public void ReelNudgeDown(int nudge)
        {
            Debug.Log("nudge down: " + nudge);
            this.nudge = nudge; // reel number
            nudgeUp = false;
            RunSlots();
        }

        public void ReelNudgeUp(int nudge)
        {
            Debug.Log("nudge up: " + nudge);
            this.nudge = nudge; // reel number
            nudgeUp = true;
            RunSlots();
        }
        #endregion nudge

        private void RunSlots()
        {
            if (slotsRunned) return;
            winController.WinEffectsShow(false, false);
            winController.WinShowCancel();

            winController.ResetLineWinning();
            controls.JPWinCancel();

            StopCoroutine(RunSlotsAsyncC());

            if (!controls.AnyLineSelected)
            {
                MGUI.ShowMessage(null, "Please select a any line.", 1.5f, null);
                controls.ResetAutoSpinsMode();
                return;
            }

            if (controls.ApllyFreeSpin())
            {
                if (!IsFreeSpin) StartFreeGamesEvent?.Invoke();
                IsFreeSpin = true;
                StartCoroutine(RunSlotsAsyncC());
                return;
            }
            else
            {
                IsFreeSpin = false;
            }

            if (!controls.ApplyBet())
            {
                MGUI.ShowMessage(null, "You have no money.", 1.5f, null);
                controls.ResetAutoSpinsMode();
                return;
            }

            StartCoroutine(RunSlotsAsyncC());
        }
       
        private IEnumerator RunSlotsAsyncC()
        {
            StartSpinEvent?.Invoke();

            jackPotWinCoins = 0;
            jackPotType = JackPotType.None;

            slotsRunned = true;
            if(controls.Auto && !IsFreeSpin) controls.IncAutoSpinsCounter();
            Debug.Log("Spins count from game start: " + (++spinCount));

            MPlayer.SetWinCoinsCount(0); 

            //1 ---------------start preparation-------------------------------
            SetInputActivity(false);
            winController.HideAllLines();

            //1a ------------- sound -----------------------------------------
            MSound.StopAllClip(false); // stop all clips with background musik
            MSound.PlayClip(0f, true, spinSound);

            //2 --------start rotating ----------------------------------------
            bool fullRotated = false;
            RotateSlots(() => { MSound.StopClips(spinSound); fullRotated = true; });
            while (!fullRotated) yield return wfs0_2;  // wait 
            EndSpinEvent?.Invoke();

            //3 --------check result-------------------------------------------
            BeginWinCalcEvent?.Invoke();
            winController.SearchWinSymbols();
            bool hasLineWin = false;
            bool hasScatterWin = false;
            EndWinSearchEvent?.Invoke();
            bool bigWin = false;

            // 3a ----- increase jackpots ----
            IncreaseJackPots();

            if (winController.HasAnyWinn(ref hasLineWin, ref hasScatterWin, ref jackPotType) || sideWinFlag)
            {
                sideWinFlag = false;    // reset flag

                // 3b0 win events
                AnyWinEvent?.Invoke();
                StartWinShowEvent?.Invoke();
                yield return StartCoroutine(WaitActivity());

                //3b1 ---- show particles, line flashing  -----------
                winController.WinEffectsShow(winLineFlashing == WinLineFlashing.All, winSymbolParticles);
                yield return StartCoroutine(WaitActivity());

                //3b2 --------- check Jack pot -------------
                jackPotWinCoins = controls.GetJackPotCoins(jackPotType);
                if (jackPotType != JackPotType.None && jackPotWinCoins > 0)
                {
                    controls.SetWinInfo(jackPotWinCoins);
                    MPlayer.AddCoins(jackPotWinCoins);

                    if (controls.HasFreeSpin || controls.Auto)
                    {
                        controls.JPWinShow(jackPotWinCoins, jackPotType);
                        yield return new WaitForSeconds(5.0f); // delay
                        controls.JPWinCancel(); 
                    }
                    else
                    {
                        controls.JPWinShow(jackPotWinCoins, jackPotType);
                        yield return new WaitForSeconds(3.0f);// delay
                    }
                    controls.SetJackPotCount(0, jackPotType); // reset jack pot amount
                }
                yield return StartCoroutine(WaitActivity());

                //3c0 -----------------calc coins -------------------
                int winCoins = winController.GetWinCoins();
                int payMultiplier = winController.GetPayMultiplier();
                winCoins *= payMultiplier;
                if (useLineBetMultiplier) winCoins *= controls.LineBet;
                controls.SetWinInfo(jackPotWinCoins + winCoins);
                MPlayer.SetWinCoinsCount(winCoins);
                MPlayer.AddCoins(winCoins);
                if (winCoins > 0)
                {
                    WinCoinsEvent?.Invoke(winCoins);
                    bigWin = (winCoins >= MPlayer.MinWin && MPlayer.UseBigWinCongratulation);
                    if (!bigWin) MSound.PlayClip(0, winCoinsSound);
                    else
                    {
                        MGUI.ShowMessage(BigWinPrefab, winCoins.ToString(),"", 3f, null);
                    }
                }
                yield return StartCoroutine(WaitActivity());

                //3c1 ----------- calc free spins ----------------
                int winSpins = winController.GetWinSpins();
                int freeSpinsMultiplier = winController.GetFreeSpinsMultiplier();
                winSpins *= freeSpinsMultiplier;
                int winLinesCount = winController.GetWinLinesCount();
                if (useLineBetFreeSpinMultiplier) winSpins *= controls.LineBet;
                // if (winSpins > 0) MSound.PlayClip((winCoins > 0 || jackPotWinCoins > 0) ? 1.5f : 0, winFreeSpinSound);
                if (winSpins > 0)
                {
                    WinSpinsEvent?.Invoke(winSpins);
                }
                yield return StartCoroutine(WaitActivity());
                controls.AddFreeSpins(winSpins);
                playFreeSpins = (controls.AutoPlayFreeSpins && controls.HasFreeSpin);

                //3d0 ----- invoke scatter win event -----------
                if (winController.scatterWin != null && winController.scatterWin.WinEvent != null) winController.scatterWin.WinEvent.Invoke();
                SlotStatistic.Add(new StatisticData(controls.TotalBet, winCoins, IsFreeSpin, winController.GetWinLines(), winController.scatterWin, winController.jpWinSymbols, jackPotWinCoins));
                EndWinCalcEvent?.Invoke();
                yield return StartCoroutine(WaitActivity());

                // 3d1 -------- add levelprogress --------------
                MPlayer.AddLevelProgress( (useLineBetProgressMultiplier)? winSpinLevelProgress * winLinesCount * controls.LineBet : winSpinLevelProgress * winLinesCount); // for each win line

                // 3d2 ------------ start line events ----------
                winController.StartLineEvents();
                yield return StartCoroutine(WaitActivity());

                //3e ---- ENABLE player interaction -----------
                slotsRunned = false;
                if (!playFreeSpins)
                {
                    SetInputActivity(true);
                }
				MSound.PlayCurrentMusic();

                //3f ----------- show line effects, sccater win, jp win symbols events can be interrupted by player----------------
                bool showEnd = true;
                if (hasLineWin || hasScatterWin || jackPotType != JackPotType.None)
                {
                    showEnd = false;
                    winController.WinSymbolShow(winLineFlashing,
                       (windata) => //linewin
                       {
                           //event can be interrupted by player
                           if (windata != null) Debug.Log("lineWin : " + windata.ToString());
                       },
                       () => //scatter win
                       {
                           //event can be interrupted by player
                       },
                       () => //jack pot 
                       {
                           //event can be interrupted by player
                       },
                       () =>
                       {
                           showEnd = true;
                       }
                       );
                }

                while (!showEnd) yield return null;  // wait for show end
                yield return StartCoroutine(WaitActivity());

                EndWinShowEvent_1?.Invoke();
                yield return StartCoroutine(WaitActivity());
                EndWinShowEvent_2?.Invoke();
                yield return StartCoroutine(WaitActivity());
                EndWinShowEvent_3?.Invoke();
                yield return StartCoroutine(WaitActivity());
            } // end win
            else // lose
            {
                EndWinCalcEvent?.Invoke();
                SlotStatistic.Add(new StatisticData(controls.TotalBet, IsFreeSpin));
                MSound.PlayClip(0, looseSound);

                MPlayer.AddLevelProgress(loseSpinLevelProgress);

                //3e ---- ENABLE player interaction -----------
                slotsRunned = false;
                SetInputActivity(true);
				MSound.PlayCurrentMusic();
                LooseEvent?.Invoke();
            }

            playFreeSpins = (controls.AutoPlayFreeSpins && controls.HasFreeSpin);
            if (IsFreeSpin && !playFreeSpins) EndFreeGamesEvent?.Invoke();
            if (logStatistic) SlotStatistic.PrintStatistic();

            yield return StartCoroutine(WaitActivity());

            playFreeSpins = (controls.AutoPlayFreeSpins && controls.HasFreeSpin);
            if (controls.Auto && controls.AutoSpinsCounter >= controls.AutoSpinCount)
            {
                controls.ResetAutoSpinsMode();
            }

            if (controls.Auto || playFreeSpins)
            {
                RunSlots();
            }
        }

        private IEnumerator WaitActivity()
        {
            while (!MGUI.HasNoPopUp) yield return wfs0_1;  // wait for the closin all popups
            while (SlotEvents.Instance && SlotEvents.Instance.MiniGameStarted) yield return wfs0_1;  // wait for the mini game to close
        }

        private void IncreaseJackPots()
        {
            if (useMiniJacPot) controls.AddMiniJackPot((jackPotIncType == JackPotIncType.Const) ?
                     jackPotIncValue : (int)((float)controls.MiniJackPotStart * (float)jackPotIncValue / 100f));
            if (useMaxiJacPot) controls.AddMaxiJackPot((jackPotIncType == JackPotIncType.Const) ?
                  jackPotIncValue : (int)((float)controls.MaxiJackPotStart * (float)jackPotIncValue / 100f));
            if (useMegaJacPot) controls.AddMegaJackPot((jackPotIncType == JackPotIncType.Const) ?
                  jackPotIncValue : (int)((float)controls.MegaJackPotStart * (float)jackPotIncValue / 100f));
        }

        private void RotateSlots(Action rotCallBack)
        {
            ParallelTween pT = new ParallelTween();
            if (IsNudge)
            {
                {
                    pT.Add((callBack) =>
                    {
                        slotGroupsBeh[nudge].ReelNudge(mainRotateType, nudgeUp, callBack);
                        nudge = -1; // reset nudge
                    });
                }
                pT.Start(rotCallBack);
                return;
            }

            int[] rands = rng.GetRandSymbols(); // next symbols for reel (bottom raycaster) or -1
            if (infiniteSpinFlag)
            {
                for (int i = 0; i < rands.Length; i++) rands[i] = -1;
                infiniteSpinFlag = false;
            }

            //hold feature
            HoldFeature hold = controls.Hold;
            bool[] holdReels = null;
            if (controls.UseHold && hold && hold.Length == rands.Length)
            {
                holdReels = hold.GetHoldReels();

                for (int i = 0; i < rands.Length; i++)
                {
                    rands[i] = (holdReels[i]) ? slotGroupsBeh[i].CurrOrderPosition : rands[i]; // hold position
                }
            }

            #region prediction visible symbols on reels
            if (debugPredictSymbols)
            for (int i = 0; i < rands.Length; i++)
            {
                    Debug.Log("------- Reel: " + i +" ------- (down up)");
                    for (int r = 0; r < slotGroupsBeh[i].RayCasters.Length; r++)
                    {
                        int sO = (int)Mathf.Repeat(rands[i] + r, slotGroupsBeh[i].symbOrder.Count);
                        int sID =  slotGroupsBeh[i].symbOrder[sO]; 
                        string sName = slotIcons[sID].iconSprite.name; 
                        Debug.Log("NextSymb ID: " + sID + " ;name : " + sName);
                    }
            }
            #endregion prediction

            for (int i = 0; i < slotGroupsBeh.Length; i++)
            {
                int n = i;
                int r = rands[i];

                if (holdReels == null || (holdReels != null && !holdReels[i]))
                {
                    pT.Add((callBack) =>
                    {
                        slotGroupsBeh[n].NextRotateCylinderEase(mainRotateType, inRotType, outRotType,
                            mainRotateTime, mainRotateTimeRandomize / 100f,
                            inRotTime, outRotTime, inRotAngle, outRotAngle,
                            r, callBack);
                    });
                }
            }

            pT.Start(rotCallBack);
        }

        /// <summary>
        /// Set touch activity for game and gui elements of slot scene
        /// </summary>
        private void SetInputActivity(bool activity)
        {
            if (activity)
            {
                if (controls.HasFreeSpin)
                {
                    menuController.SetControlActivity(false); // preserve bet change if free spin available
                    controls.SetControlActivity(false, true);
                }
                else
                {
                    menuController.SetControlActivity(activity);
                    controls.SetControlActivity(true, true);
                }
            }
            else
            {
                menuController.SetControlActivity(activity); 
                controls.SetControlActivity(activity, controls.Auto); 
            }
        }

        /// <summary>
        /// Calculate propabilities
        /// </summary>
        /// <returns></returns>
        public string[,] CreatePropabilityTable()
        {
            List<string> rowList = new List<string>();
            string[] iconNames = GetIconNames(false);
            int length = slotGroupsBeh.Length;
            string[,] table = new string[length + 1, iconNames.Length + 1];

            rowList.Add("reel / icon");
            rowList.AddRange(iconNames);
            SetRow(table, rowList, 0, 0);

            for (int i = 1; i <= length; i++)
            {
                table[i, 0] = "reel #" + i.ToString();
                SetRow(table, new List<float>(slotGroupsBeh[i - 1].GetReelSymbHitPropabilities(slotIcons)), 1, i);
            }
            return table;
        }

        /// <summary>
        /// Calculate propabilities
        /// </summary>
        /// <returns></returns>
        public string[,] CreatePayTable(out float sumPayOut, out float sumPayoutFreeSpins)
        {
            List<string> row = new List<string>();
            List<float[]> reelSymbHitPropabilities = new List<float[]>();
            string[] iconNames = GetIconNames(false);

            sumPayOut = 0;
            CreateFullPaytable();
            int rCount = payTableFull.Count + 1;
            int cCount = slotGroupsBeh.Length + 3;
            string[,] table = new string[rCount, cCount];
            row.Add("PayLine / reel");
            for (int i = 0; i < slotGroupsBeh.Length; i++)
            {
                row.Add("reel #" + (i + 1).ToString());
            }
            row.Add("Payout");
            row.Add("Payout, %");
            SetRow(table, row, 0, 0);

            PayLine pL;
            List<PayLine> freeSpinsPL = new List<PayLine>();  // paylines with free spins

            for (int i = 0; i < payTableFull.Count; i++) 
            {
                pL = payTableFull[i];
                table[i + 1, 0] = "Payline #" + (i + 1).ToString();
                table[i + 1, cCount - 2] = pL.pay.ToString();
                float pOut = pL.GetPayOutProb(this);
                sumPayOut += pOut;
                table[i + 1, cCount - 1] = pOut.ToString("F6");
                // Debug.Log(i);
                SetRow(table, new List<string>(pL.Names(slotIcons, slotGroupsBeh.Length)), 1, i + 1);
                if (pL.freeSpins > 0) freeSpinsPL.Add(pL);
            }

            Debug.Log("sum (without free spins) % = " + sumPayOut);

            sumPayoutFreeSpins = sumPayOut;
            foreach (var item in freeSpinsPL)
            {
                sumPayoutFreeSpins += sumPayOut * item.GetProbability(this) * item.freeSpins;
            }
            Debug.Log("sum (with free spins) % = " + sumPayoutFreeSpins);

            return table;
        }

        private void SetRow<T>(string[,] table, List<T> row, int beginColumn, int rowNumber)
        {
            if (rowNumber >= table.GetLongLength(0)) return;

            for (int i = 0; i < row.Count; i++)
            {
                // Debug.Log("sr"+i);
                if (i + beginColumn < table.GetLongLength(1)) table[rowNumber, i + beginColumn] = row[i].ToString();
            }
        }

        public string[] GetIconNames(bool addAny)
        {
            if (slotIcons == null || slotIcons.Length == 0) return null;
            int length = (addAny) ? slotIcons.Length + 1 : slotIcons.Length;
            string[] sName = new string[length];
            if (addAny) sName[0] = "any";
            int addN = (addAny) ? 1 : 0;
            for (int i = addN; i < length; i++)
            {
                if (slotIcons[i - addN] != null && slotIcons[i - addN].iconSprite != null)
                {
                    sName[i] = slotIcons[i - addN].iconSprite.name;
                }
                else
                {
                    sName[i] = (i - addN).ToString();
                }
            }
            return sName;
        }

        internal WinSymbolBehavior GetWinPrefab(string tag)
        {
            if (winSymbolBehaviors == null || winSymbolBehaviors.Length == 0) return null;
            foreach (var item in winSymbolBehaviors)
            {
                if (item.WinTag.Contains(tag))
                {
                    return item;
                }
            }
            return null;
        }

        private void CreateFullPaytable()
        {
            payTableFull = new List<PayLine>();
            for (int j = 0; j < payTable.Count; j++)
            {
                payTable[j].ClampLine(slotGroupsBeh.Length);
                payTableFull.Add(payTable[j]);
                if (useWild) payTableFull.AddRange(payTable[j].GetWildLines(this));
            }
        }

        public void SetSideWinFlag()
        {
            sideWinFlag = true;
        }

        #region dev
        public void RebuildLines()
        {
            foreach (var item in payTable)
            {
                int[] line = new int[slotGroupsBeh.Length];

                for (int i = 0; i < line.Length; i++)
                {
                    if (i < item.line.Length) line[i] = item.line[i];
                    else line[i] = -1;
                }
                item.line = line;
            }
        }

        public string PaytableToJsonString()
        {
            string res = "";
            ListWrapper<PayLine> lW = new ListWrapper<PayLine>(payTable);
            res = JsonUtility.ToJson(lW);
            return res;
        }

        public void SetPayTableFromJson()
        {
            Debug.Log("Json viewer - " + "http://jsonviewer.stack.hu/");
            Debug.Log("old paytable json: " + PaytableToJsonString());

            if (string.IsNullOrEmpty(payTableJsonString))
            {
                Debug.Log("payTableJsonString : empty");
                return;
            }

            ListWrapper<PayLine> lWPB = JsonUtility.FromJson<ListWrapper<PayLine>>(payTableJsonString);
            if (lWPB != null && lWPB.list != null && lWPB.list.Count > 0)
            {
                payTable = lWPB.list;
            }
        }
        #endregion dev
    }

    public enum RNGType { Unity, MersenneTwister }
    public class RNG
    {
        private int[] randSymb;
        private RNGType rngType;
        private Action UpdateRNGAction;
        private ReelData[] reelsData;
        private RandomMT randomMT;

        public RNG(RNGType rngType, ReelData[] reelsData)
        {
            randSymb = new int[reelsData.Length];
            this.rngType = rngType;
            this.reelsData = reelsData;
            switch (rngType)
            {
                case RNGType.Unity:
                    UpdateRNGAction = UnityRNGUpdate;
                    break;
                case RNGType.MersenneTwister:
                    randomMT = new RandomMT();
                    UpdateRNGAction = MTRNGUpdate;
                    break;
                default:
                    UpdateRNGAction = UnityRNGUpdate;
                    break;
            }
        }

        public void Update()
        {
            UpdateRNGAction();
        }

        public int[] GetRandSymbols()
        {
            return randSymb;
        }

        int rand;
        private void UnityRNGUpdate()
        {
            for (int i = 0; i < randSymb.Length; i++)
            {
                rand = UnityEngine.Random.Range(0, reelsData[i].Length);
                randSymb[i] = rand;
            }
        }

        private void MTRNGUpdate()
        {
            for (int i = 0; i < randSymb.Length; i++)
            {
                rand = randomMT.RandomRange(0, reelsData[i].Length-1);
                randSymb[i] = rand;
            }
        }
    }

    [Serializable]
    public class ReelData
    {
        public List<int> symbOrder;
        public ReelData(List<int> symbOrder)
        {
            this.symbOrder = symbOrder;
        }
        public int Length
        {
            get { return (symbOrder == null) ? 0 : symbOrder.Count; }
        }
        public int GetSymbolAtPos(int position)
        {
            return (symbOrder == null || position >= symbOrder.Count) ? 0 : symbOrder.Count;
        }
    }

    /// <summary>
	/// Summary description for RandomMT.https://www.codeproject.com/Articles/5147/A-C-Mersenne-Twister-class
	/// </summary>
	public class RandomMT
    {
        private const int N = 624;
        private const int M = 397;
        private const uint K = 0x9908B0DFU;
        private const uint DEFAULT_SEED = 4357;

        private ulong[] state = new ulong[N + 1];
        private int next = 0;
        private ulong seedValue;


        public RandomMT()
        {
            SeedMT(DEFAULT_SEED);
        }
        public RandomMT(ulong _seed)
        {
            seedValue = _seed;
            SeedMT(seedValue);
        }

        public ulong RandomInt()
        {
            ulong y;

            if ((next + 1) > N)
                return (ReloadMT());

            y = state[next++];
            y ^= (y >> 11);
            y ^= (y << 7) & 0x9D2C5680U;
            y ^= (y << 15) & 0xEFC60000U;
            return (y ^ (y >> 18));
        }

        private void SeedMT(ulong _seed)
        {
            ulong x = (_seed | 1U) & 0xFFFFFFFFU;
            int j = N;

            for (j = N; j >= 0; j--)
            {
                state[j] = (x *= 69069U) & 0xFFFFFFFFU;
            }
            next = 0;
        }

        public int RandomRange(int lo, int hi)
        {
            return (Math.Abs((int)RandomInt() % (hi - lo + 1)) + lo);
        }

        public int RollDice(int face, int number_of_dice)
        {
            int roll = 0;
            for (int loop = 0; loop < number_of_dice; loop++)
            {
                roll += (RandomRange(1, face));
            }
            return roll;
        }

        public int HeadsOrTails() { return ((int)(RandomInt()) % 2); }

        public int D6(int die_count) { return RollDice(6, die_count); }
        public int D8(int die_count) { return RollDice(8, die_count); }
        public int D10(int die_count) { return RollDice(10, die_count); }
        public int D12(int die_count) { return RollDice(12, die_count); }
        public int D20(int die_count) { return RollDice(20, die_count); }
        public int D25(int die_count) { return RollDice(25, die_count); }


        private ulong ReloadMT()
        {
            ulong[] p0 = state;
            int p0pos = 0;
            ulong[] p2 = state;
            int p2pos = 2;
            ulong[] pM = state;
            int pMpos = M;
            ulong s0;
            ulong s1;

            int j;

            if ((next + 1) > N)
                SeedMT(seedValue);

            for (s0 = state[0], s1 = state[1], j = N - M + 1; --j > 0; s0 = s1, s1 = p2[p2pos++])
                p0[p0pos++] = pM[pMpos++] ^ (mixBits(s0, s1) >> 1) ^ (loBit(s1) != 0 ? K : 0U);


            for (pM[0] = state[0], pMpos = 0, j = M; --j > 0; s0 = s1, s1 = p2[p2pos++])
                p0[p0pos++] = pM[pMpos++] ^ (mixBits(s0, s1) >> 1) ^ (loBit(s1) != 0 ? K : 0U);


            s1 = state[0];
            p0[p0pos] = pM[pMpos] ^ (mixBits(s0, s1) >> 1) ^ (loBit(s1) != 0 ? K : 0U);
            s1 ^= (s1 >> 11);
            s1 ^= (s1 << 7) & 0x9D2C5680U;
            s1 ^= (s1 << 15) & 0xEFC60000U;
            return (s1 ^ (s1 >> 18));
        }

        private ulong hiBit(ulong _u)
        {
            return ((_u) & 0x80000000U);
        }
        private ulong loBit(ulong _u)
        {
            return ((_u) & 0x00000001U);
        }
        private ulong loBits(ulong _u)
        {
            return ((_u) & 0x7FFFFFFFU);
        }
        private ulong mixBits(ulong _u, ulong _v)
        {
            return (hiBit(_u) | loBits(_v));

        }
    }

    [Serializable]
    //Helper class for creating pay table
    public class PayTable
    {
        public int reelsCount;
        public List<PayLine> payLines;
        public void Rebuild()
        {
            if (payLines != null)
            {
                foreach (var item in payLines)
                {
                    if (item != null)
                    {
                        item.RebuildLine();
                    }
                }
            }
        }
    }

    [Serializable]
    public class PayLine
    {
        private const int maxLength = 5;
        public int[] line;
        public int pay;
        public int freeSpins;
        public bool showEvent = false;
        public UnityEvent LineEvent;
        [Tooltip("Payouts multiplier, default value = 1")]
        public int payMult = 1; // payout multiplier
        [Tooltip("Free Spins multiplier, default value = 1")]
        public int freeSpinsMult = 1; // payout multiplier

        bool useWildInFirstPosition = false;

        public PayLine()
        {
            line = new int[maxLength];
            for (int i = 0; i < line.Length; i++)
            {
                line[i] = -1;
            }
        }

        public PayLine(PayLine pLine)
        {
            if (pLine.line != null)
            {
                line = pLine.line;
                RebuildLine();
                pay = pLine.pay;
                freeSpins = pLine.freeSpins;
                LineEvent = pLine.LineEvent;
                payMult = pLine.payMult;
            }
            else
            {
                RebuildLine();
            }
        }

        public PayLine(int[] newLine, int pay, int freeSpins)
        {
            if (newLine != null)
            {
                this.line = newLine;
                this.pay = pay;
                this.freeSpins = freeSpins;
            }
            RebuildLine();
        }

        public string ToString(Sprite[] sprites, int length)
        {
            string res = "";
            if (line == null) return res;
            for (int i = 0; i < line.Length; i++)
            {
                if (i < length)
                {
                    if (line[i] >= 0)
                        res += sprites[line[i]].name;
                    else
                    {
                        res += "any";
                    }
                    if (i < line.Length - 1) res += ";";
                }
            }
            return res;
        }

        public string[] Names(SlotIcon[] sprites, int length)
        {
            if (line == null) return null;
            List<string> res = new List<string>();
            for (int i = 0; i < line.Length; i++)
            {
                if (i < length)
                {
                    if (line[i] >= 0)
                        res.Add((sprites[line[i]] != null && sprites[line[i]].iconSprite != null) ? sprites[line[i]].iconSprite.name : "failed");
                    else
                    {
                        res.Add("any");
                    }
                }
            }
            return res.ToArray();
        }

        public float GetPayOutProb(SlotController sC)
        {
            return GetProbability(sC) * 100f * pay;
        }

        public float GetProbability(SlotController sC)
        {
            float res = 0;
            if (!sC) return res;
            if (line == null || sC.slotGroupsBeh == null || sC.slotGroupsBeh.Length > line.Length) return res;
            float[] rP = sC.slotGroupsBeh[0].GetReelSymbHitPropabilities(sC.slotIcons);

            //avoid "any" symbol error in first position
            res = (line[0] >= 0) ? rP[line[0]] : 1; //  res = rP[line[0]];

            for (int i = 1; i < sC.slotGroupsBeh.Length; i++)
            {
                if (line[i] >= 0) // any.ID = -1
                {
                    rP = sC.slotGroupsBeh[i].GetReelSymbHitPropabilities(sC.slotIcons);
                    res *= rP[line[i]];
                }
                else
                {
                    // break;
                }
            }
            return res;
        }

        /// <summary>
        /// Create and return additional lines for this line with wild symbol,  only if symbol can be substitute with wild
        /// </summary>
        /// <returns></returns>
        public List<PayLine> GetWildLines(SlotController sC)
        {
            int workLength = sC.slotGroupsBeh.Length;
            List<PayLine> res = new List<PayLine>();
            if (!sC) return res; // return empty list
            if (!sC.useWild) return res; // return empty list

            int wild_id = sC.wild_id;
            useWildInFirstPosition = sC.useWildInFirstPosition;
            List<int> wPoss = GetPositionsForWild(wild_id, sC);
            int maxWildsCount = (useWildInFirstPosition) ? wPoss.Count - 1 : wPoss.Count;
            int minWildsCount = 1;
            ComboCounter cC = new ComboCounter(wPoss);
            while (cC.NextCombo())
            {
                List<int> combo = cC.combo; // 
                int comboSum = combo.Sum(); // count of wilds in combo

                if (comboSum >= minWildsCount && comboSum <= maxWildsCount)
                {
                    PayLine p = new PayLine(this);
                    for (int i = 0; i < wPoss.Count; i++)
                    {
                        int pos = wPoss[i];
                        if (combo[i] == 1)
                        {
                            p.line[pos] = wild_id;
                        }
                    }
                    if (!p.IsEqual(this, workLength) && !ContainEqualLine(res, p, workLength)) res.Add(p);
                }
            }

            return res;
        }

        private bool IsEqual(PayLine pLine, int workLength)
        {
            if (pLine == null) return false;
            if (pLine.line == null) return false;
            if (line.Length != pLine.line.Length) return false;
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] != pLine.line[i]) return false;
            }
            return true;
        }

        private bool ContainEqualLine(List<PayLine> pList, PayLine pLine, int workLength)
        {
            if (pList == null) return false;
            if (pLine == null) return false;
            if (pLine.line == null) return false;

            foreach (var item in pList)
            {
                if (item.IsEqual(pLine, workLength)) return true;
            }
            return false;
        }

        /// <summary>
        /// return list position on line for wild symbols (0 - line.length -1)  
        /// </summary>
        /// <param name="wild_id"></param>
        /// <param name="sC"></param>
        /// <returns></returns>
        private List<int> GetPositionsForWild(int wild_id, SlotController sC)
        {
            List<int> wPoss = new List<int>();
            int counter = 0;
            int length = sC.slotGroupsBeh.Length;

            for (int i = 0; i < line.Length; i++)
            {
                if (i < length)
                {
                    if (line[i] != -1 && line[i] != wild_id)
                    {
                        if (!useWildInFirstPosition && counter == 0) // don't use first
                        {
                            counter++;
                        }
                        else
                        {
                            if (sC.slotIcons[line[i]].useWildSubstitute) wPoss.Add(i);
                            counter++;
                        }
                    }
                }
            }
            return wPoss;
        }

        public void RebuildLine()
        {
          // if (line.Length == maxLength) return;
            int[] lineT = new int[maxLength];
            for (int i = 0; i < maxLength; i++)
            {
                if (line != null && i < line.Length) lineT[i] = line[i];
                else lineT[i] = -1;
            }
            line = lineT;
        }

        public void ClampLine(int workLength)
        {
            RebuildLine();
            for (int i = 0; i < maxLength; i++)
            {
                if (i >= workLength) line[i] = -1;
            }
        }
    }

    [Serializable]
    public class ScatterPay
    {
        public int scattersCount;
        public int pay;
        public int freeSpins;
        public int payMult = 1;
        public int freeSpinsMult = 1;
        public UnityEvent WinEvent;

        public ScatterPay()
        {
            payMult = 1;
            freeSpinsMult = 1;
            scattersCount = 3;
            pay = 0;
            freeSpins = 0;
        }
    }

    static class ClassExt
    {
        public enum FieldAllign { Left, Right, Center}

        /// <summary>
        /// Return formatted string; (F2, N5, e, r, p, X, D12, C)
        /// </summary>
        /// <param name="fNumber"></param>
        /// <param name="format"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static string ToString(this float fNumber, string format, int field)
        {
            string form = "{0," + field.ToString() +":"+ format + "}";
            string res = String.Format(form, fNumber);
            return res;
        }

        /// <summary>
        /// Return formatted string; (F2, N5, e, r, p, X, D12, C)
        /// </summary>
        /// <param name="fNumber"></param>
        /// <param name="format"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static string ToString(this string s, int field)
        {
            string form = "{0," + field.ToString() +"}";
            string res = String.Format(form, s);
            return res;
        }

        /// <summary>
        /// Return formatted string; (F2, N5, e, r, p, X, D12, C)
        /// </summary>
        /// <param name="fNumber"></param>
        /// <param name="format"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static string ToString(this string s, int field, FieldAllign fAllign)
        {
            int length = s.Length;
            if (length >= field)
            {
                string form = "{0," + field.ToString() + "}";
                return String.Format(form, s);
            }
            else
            {
                if (fAllign == FieldAllign.Center)
                {
                    int lCount = (field - length) / 2;
                    int rCount = field - length - lCount;
                    string lSp = new string('*', lCount);
                    string rSp = new string('*', rCount);
                    return (lSp + s + rSp);
                }
                else if (fAllign == FieldAllign.Left)
                {
                    int lCount = (field - length);
                    string lSp = new string('*', lCount);
                    return (s+lSp);
                }
                else
                {
                    string form = "{0," + field.ToString() + "}";
                    return  String.Format(form, s);
                }
            }
        }

        private static string ToStrings<T>(T[] a)
        {
            string res = "";
            for (int i = 0; i < a.Length; i++)
            {
                res += a[i].ToString();
                res += " ";
            }
            return res;
        }

        private static string ToStrings(float[] a, string format, int field)
        {
            string res = "";
            for (int i = 0; i < a.Length; i++)
            {
                res += a[i].ToString(format, field);
                res += " ";
            }
            return res;
        }

        private static string ToStrings(string[] a, int field, ClassExt.FieldAllign allign)
        {
            string res = "";
            for (int i = 0; i < a.Length; i++)
            {
                res += a[i].ToString(field, allign);
                res += " ";
            }
            return res;
        }

        private static float[] Mul(float[] a, float[] b)
        {
            if (a.Length != b.Length) return null;
            float[] res = new float[a.Length];
            for (int i = 0; i < res.Length; i++)
            {
                res[i] = a[i] * b[i];
            }
            return res;
        }

    }

    /// <summary>
    /// Helper class to make combinations from symbols with wild
    /// </summary>
    public class ComboCounter
    {
        public List<int> combo;
        public List<int> positions;

        List<byte> counterSizes;

        public ComboCounter(List<int> positions)
        {
            this.positions = positions;
            counterSizes = GetComboCountsForSymbols();
            combo = new List<int>(counterSizes.Count);

            for (int i = 0; i < counterSizes.Count; i++) // create in counter first combination
            {
                combo.Add(0);
            }
        }

        /// <summary>
        /// get list with counts of combinations for each position
        /// </summary>
        /// <returns></returns>
        private List<byte> GetComboCountsForSymbols()
        {
            List<byte> res = new List<byte>();
            foreach (var item in positions)
            {
                res.Add((byte)(1)); // wild or symbol (0 or 1)
            }
            return res;
        }

        private bool Next()
        {
            for (int i = counterSizes.Count - 1; i >= 0; i--)
            {
                if (combo[i] < counterSizes[i])
                {
                    combo[i]++;
                    if (i != counterSizes.Count - 1) // reset low "bytes"
                    {
                        for (int j = i + 1; j < counterSizes.Count; j++)
                        {
                            combo[j] = 0;
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        public bool NextCombo()
        {
            if (Next())
            {
                return true;
            }
            return false;
        }
    }
}

