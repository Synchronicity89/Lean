using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaigeIncCmdLine
{


    public class TwelveMonthCycle
        : QCAlgorithm
    {

        public IEnumerable<FineFundamental> filtered_fine;

        public bool monthly_rebalance;
        private readonly RollingWindow<IndicatorDataPoint> _window;
        public Dictionary<DateTime, double> Returns => _window.ToDictionary(x => x.EndTime, x => (double)x.Value);

        public override void Initialize()
        {
            this.SetStartDate(2013, 1, 1);
            this.SetEndDate(2018, 8, 1);
            this.SetCash(100000);
            this.AddEquity("SPY", Resolution.Daily);
            this.Schedule.On(this.DateRules.MonthStart("SPY"), this.TimeRules.AfterMarketOpen("SPY"), this.Rebalance);
            this.monthly_rebalance = false;
            this.filtered_fine = null;
            this.UniverseSettings.Resolution = Resolution.Daily;
            this.AddUniverse(this.CoarseSelectionFunction, this.FineSelectionFunction);
        }

        public virtual IEnumerable<Symbol> CoarseSelectionFunction(IEnumerable<CoarseFundamental> coarse)
        {
            if (this.monthly_rebalance)
            {//rebalancing return only usa market symbols that have fundemental data
                coarse = (from x in coarse
                          where x.HasFundamentalData && x.Market == "usa"
                          select x).ToList();
                return (from i in coarse
                        select i.Symbol).ToList();
            }
            else
            {
                return new List<Symbol>();
            }
        }

        public virtual IEnumerable<Symbol> FineSelectionFunction(IEnumerable<FineFundamental> fine)
        {
            if (this.monthly_rebalance)
            {
                fine = (from i in fine
                        where i.SecurityReference.ExchangeId == "NYS" || i.SecurityReference.ExchangeId == "ASE"
                        select i).ToList();
                this.filtered_fine = new List<FineFundamental>();
                foreach (var i in fine)
                {
                    i.CompanyProfile.MarketCap = (long)(i.EarningReports.BasicAverageShares.TwelveMonths * (i.EarningReports.BasicEPS.TwelveMonths * i.ValuationRatios.PERatio));
                    var history_start = this.History(new List<Symbol> {
                        i.Symbol
                    }, TimeSpan.FromDays(365));
                    var history_end = this.History(new List<Symbol> {
                        i.Symbol
                    }, TimeSpan.FromDays(335));
                    if (history_start.Count() != 0 && history_end.Count() != 0)
                    {
                        i.OperationRatios.ROA. = (double)(history_end.FirstOrDefault()["close"] - history_start.FirstOrDefault()["close"]);
                        this.filtered_fine.Add(i);
                    }
                }
                var size = Convert.ToInt32(fine.Count() * 0.3);
                this.filtered_fine = this.filtered_fine.OrderByDescending(x => x.CompanyProfile.MarketCap);
                this.filtered_fine = this.filtered_fine.Take(size);//Actually want only the last one
                this.filtered_fine = this.filtered_fine.OrderByDescending(x => x.).ToList();
                var symbols = (from i in this.filtered_fine
                               select i.Symbol).ToList();
                this.filtered_fine = symbols;
                return symbols;
            }
            else
            {
                return new List<Symbol>();
            }
        }

        public virtual void Rebalance()
        {
            this.monthly_rebalance = true;
        }

        public virtual void  OnData(object data)
        {
            if (!this.monthly_rebalance)
            {
                return;
            }
            if (filtered_fine == null || this.filtered_fine.Count == 0)
            {
                return;
            }
            this.monthly_rebalance = false;
            var portfolio_size = Convert.ToInt32(this.filtered_fine.Count / 10);
            var short_stocks = this.filtered_fine[-portfolio_size];
            var long_stocks = this.filtered_fine[::portfolio_size];
            var stocks_invested = (from x in this.Portfolio
                                   select x.Key).ToList();
            foreach (var i in stocks_invested)
            {
                //liquidate the stocks not in the filtered balance sheet accrual list
                if (!this.filtered_fine.Contains(i))
                {
                    this.Liquidate(i);
                }
                else if (long_stocks.Contains(i))
                {
                    //long the stocks in the list
                    this.SetHoldings(i, 1 / (portfolio_size * 2));
                }
                else if (short_stocks.Contains(i))
                {
                    //short the stocks in the list
                    this.SetHoldings(i, -1 / (portfolio_size * 2));
                }
            }
        }
    }

}
