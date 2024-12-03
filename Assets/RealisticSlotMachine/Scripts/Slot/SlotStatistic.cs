using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mkey
{
    public static class SlotStatistic
    {
        static bool loaded = false;
        static string saveKey = "mk_slot_statistic";
        static int maxCount = 10000;
        public static List<StatisticData> statisticData;
        static bool save;
        public static double initBalance = 0;
        private static bool init = false;

        private static void Load()
        {
            if (save && PlayerPrefs.HasKey(saveKey))
            {
                string json = PlayerPrefs.GetString(saveKey);
                ListWrapper<StatisticData> sd = JsonUtility.FromJson<ListWrapper<StatisticData>>(json);
                if (sd != null) statisticData = sd.list;
            }
            else
            {
                statisticData = new List<StatisticData>();
            }
            loaded = true;
        }

        private static void Save()
        {
            if (statisticData == null) return;
            ListWrapper<StatisticData> sd = new ListWrapper<StatisticData>(statisticData);
            string json = JsonUtility.ToJson(sd);
            PlayerPrefs.SetString(saveKey, json);
        }

        private static void Clear()
        {
            PlayerPrefs.DeleteKey(saveKey);
            statisticData = new List<StatisticData>();
        }

        public static void Add(StatisticData sD)
        {
            if (!init)
            {
                init = true;
                initBalance = SlotPlayer.Instance.Coins;
            }
            if (!loaded) Load();
            statisticData.Add(sD);
            if (statisticData.Count > maxCount) statisticData.RemoveAt(0);
            if(save) Save();
            //Debug.Log("add statistic");
        }

        public static List<StatisticData> Get()
        {
            if (!loaded) Load();
            return statisticData;
        }

        public static void PrintStatistic()
        {
            double sumBet = 0;
            double sumPay = 0;
            int spins = 0;
            if(statisticData!=null && statisticData.Count > 0)
            {
                spins = statisticData.Count;
                foreach (var item in statisticData)
                {
                    sumBet = (item.isFreeSpin) ? sumBet : sumBet + item.bet;
                    sumPay += item.winCoins;
                }
            }
            double payOut = sumPay / sumBet * 100.0;

            Debug.Log("Statistic, balance :" + SlotPlayer.Instance.Coins + " ; sum bet: " + sumBet + " ;sum payout: " + sumPay + " ;spins: " + spins +" ; payout %: " +payOut);
        }
    }

    [System.Serializable]
    public class StatisticData
    {
        public double bet;
        public double winCoins;
        public bool isFreeSpin;
        public List<WinData> lineWins;
        public WinData scatterWin;
        public List<WinData> jpWins;

        public StatisticData(double bet, bool isFreeSpin)
        {
            this.bet = bet;
            this.isFreeSpin = isFreeSpin;
            winCoins = 0;
            lineWins = null;
            scatterWin = null;
            jpWins = null;
        }

        public  StatisticData(double bet, double winCoins, bool isFreeSpin, List<LineBehavior> winLines, WinData scatterWin, List<SlotSymbol> jpWinSymbols, double jpCoins)
        {
            this.bet = bet;
            this.winCoins = winCoins;
            this.isFreeSpin = isFreeSpin;

            lineWins = new List<WinData>();

            if (winLines != null && winLines.Count > 0)
            {
                foreach (var item in winLines)
                {
                    lineWins.Add(new WinData(item.win));
                }
            }

            if (scatterWin != null) this.scatterWin = new WinData(scatterWin);

            jpWins = new List<WinData>();

            if (jpWinSymbols != null && jpCoins > 0)
            {
                    jpWins.Add(new WinData(jpWinSymbols, 0, (int)jpCoins, 0, 0, null));
            }
        }

        public override string ToString()
        {
            return "Bet: " + bet + "; Win Coins: " + winCoins;
        }
    }
}