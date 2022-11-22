using QLNet;
using QuantConnect.Securities;
using QuantConnect.Util;
using RDotNet;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Securities.Option;
using Option = QuantConnect.Securities.Option.Option;
using QuantConnect.Data;
using Newtonsoft.Json.Linq;

namespace QuantConnect.Algorithm.CSharp.S89
{
    public class BasicOptions : QCAlgorithm
    {
        private Symbol symbol;
        private Symbol option_symbol;
        private bool order;
        private const decimal counterInit = 600.0M;
        private decimal counter = counterInit;
        private DateTime? nextTradingDay = null;
        private string symbolInit = "SPX";
        private bool isIndex = true;

        public override void Initialize()
        {
            this.SetStartDate(2022, 8, 8);  // Set Start Date
            this.SetEndDate(2022, 10, 18);
            this.SetCash(100000);  // Set Strategy Cash
            if (isIndex)
            {
                this.symbol = this.AddIndex(symbolInit, Resolution.Minute).Symbol;
            }
            else
            {
                this.symbol = this.AddEquity(symbolInit, Resolution.Minute).Symbol;
            }
            Option option = this.AddOption(symbolInit);
            this.option_symbol = option.Symbol;
            this.SetWarmUp(timeSpan: new TimeSpan(30,0,0,0,0));
            this.order = true;
            this.SetSecurityInitializer(s => SecurityInitializer(s));

            //base.Initialize();
        }
        public void SecurityInitializer(Security security)
        {
            security.SetMarketPrice(this.GetLastKnownPrice(security));
            security.SetDataNormalizationMode(DataNormalizationMode.Raw);
        }


        public override void OnData(Slice slice)
        {
            if (this.IsWarmingUp)
                return;


            if(!this.Portfolio.Invested && this.order && (nextTradingDay == null || Time > nextTradingDay))
            {
                IEnumerable<Symbol> contract_symbols = this.OptionChainProvider.GetOptionContractList(this.option_symbol, this.Time).OrderBy(s => s.ID.Date);
                var first = contract_symbols.FirstOrDefault(); if (first == null) return;
                DateTime? expiry = first?.ID.Date;
                var filtered_symbolsCall = contract_symbols.Where(cs => cs.ID.Date == expiry && cs.ID.OptionRight == OptionRight.Call);
                var filtered_symbolsPut = contract_symbols.Where(cs => cs.ID.Date == expiry && cs.ID.OptionRight == OptionRight.Put);
                Symbol contract_symbolCall = filtered_symbolsCall.OrderBy(fsc => fsc.ID.StrikePrice).FirstOrDefault(); if (contract_symbolCall == null) return;
                Symbol contract_symbolPut = filtered_symbolsPut.OrderByDescending(fsc => fsc.ID.StrikePrice).FirstOrDefault(); if (contract_symbolPut == null) return;
                Option valueCall = this.AddOptionContract(contract_symbolCall);
                Option valuePut = this.AddOptionContract(contract_symbolPut);
                if (valueCall.AskPrice == 0.0M || valuePut.AskPrice == 0.0M) return;
                int shares_to_buyCall = (int)(this.Portfolio.Cash / (200.0M * valueCall.AskPrice));
                int shares_to_buyPut = (int)(this.Portfolio.Cash / (200.0M * valuePut.AskPrice));
                if(shares_to_buyCall == 0 || shares_to_buyPut == 0) return;
                //var nextTradingDays = QuantConnect.Time.EachTradeableDay(Securities[contract_symbolCall], Time, Time + TimeSpan.FromDays(5));
                nextTradingDay = Time + TimeSpan.FromHours(8);
                //Log("KJB Test: " + nextTradingDays.FirstOrDefault().ToString());
                var ticket = this.MarketOrder(contract_symbolCall, shares_to_buyCall);
                this.MarketOrder(contract_symbolPut, shares_to_buyPut);
                

                this.order = false;
            }
            else
            {
                if((this.Portfolio.TotalUnrealisedProfit) > (Portfolio.TotalPortfolioValue * 0.01M * counter / counterInit))
                {
                    this.Liquidate();
                    order = true;
                    counter = counterInit;
                }
                else
                { 
                    if(order == false) counter -= counterInit / 20.0M;
                }
            }
        }




    }
}
