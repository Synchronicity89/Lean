using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using System;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp
{
    public class CryingBlueBear : QCAlgorithm
    {
        FuturesContract contract = null;
        private const string RootSP500 = Futures.Indices.SP500EMini;
        public Symbol SP500 = QuantConnect.Symbol.Create(RootSP500, SecurityType.Future, Market.CME);
        public override void Initialize()
        {
            var self = this;
            SetStartDate(2019, 10, 29);    //Set Start Date
            SetCash(1000000);                        //Set Strategy Cash

            AddSecurity(RootSP500);
            var futureSP500 = AddFuture(RootSP500, Resolution.Minute);
            futureSP500.SetFilter(0, 180);
            // AddEquity("BND", Resolution.Minute);
            // AddEquity("AAPL", Resolution.Minute);
            //SetDefaultTimeZone(DateTimeZone.Pacific)

            self.Schedule.On(self.DateRules.EveryDay(), self.TimeRules.At(19, 57), () => { PlaceOrder(0.1); });
            self.Schedule.On(self.DateRules.EveryDay(), self.TimeRules.At(21, 26), () => { PlaceOrder(0.0); });
            //self.Schedule.On(self.DateRules.EveryDay(), self.TimeRules.At(14, 05), () => { PlaceOrder(-0.1); });
            //self.Schedule.On(self.DateRules.EveryDay(), self.TimeRules.At(14, 55), () => { PlaceOrder(0.0); });

            // def Rebalance(self):
            // if self.Time.Day != 15: return
            // if self.Time.Day != 25: return

        }
        bool marketOpen = false;
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// Slice object keyed by symbol containing the stock data
        public override void OnData(Slice data)
        {
            if (contract == null)
            {
                SelectContract(data);
            }

            if (contract != null && IsMarketOpen(contract.Symbol) == true)
            {
                if (marketOpen == false)
                {
                    DebugLog("UtcTime: " + UtcTime.ToString() + " Symbol: " + contract.Symbol.ToString() + " Market Open");

                    marketOpen = true;
                }

            }
            if (contract != null && IsMarketOpen(contract.Symbol) == false)
            {
                if (marketOpen == true)
                {
                    DebugLog("UtcTime: " + UtcTime.ToString() + " Symbol: " + contract.Symbol.ToString() + " Market Close");
                    marketOpen = false;
                }
            }


            if (contract != null && Time.TimeOfDay == TimeSpan.FromHours(9.5))
            {
                SelectContract(data);
                DebugLog("UtcTime: " + UtcTime.ToString() + " Symbol: " + contract.Symbol.ToString() + " Regular Market Starts");
            }
        }

        private void SelectContract(Slice data)
        {
            foreach (var chain in data.FutureChains)
            {
                var contra = (from futuresContract in chain.Value.OrderBy(x => x.Expiry)
                              where futuresContract.Expiry > Time.Date.AddDays(90)
                              select futuresContract
                        ).FirstOrDefault();
                if (contra != null && contra.Equals(contract) == false)
                {
                    contract = contra;
                    break;
                }
            }
        }

        int logged = 0;
        public void PlaceOrder(double fraction)
        {
            if (contract != null && IsMarketOpen(contract.Symbol) == true)
            {
                SetHoldings(contract.Symbol, fraction);
                string message = "UtcTime: " + UtcTime.ToString() + " Symbol: " + contract.Symbol.ToString() + " Fraction: " + fraction.ToString();
                DebugLog(message);
            }
        }

        private void DebugLog(string message)
        {
            if (logged < 59)
            {
                Debug(message);
                logged++;
            }
        }
    }
}
