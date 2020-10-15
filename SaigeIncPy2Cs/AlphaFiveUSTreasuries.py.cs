
using System.Collections.Generic;

using System.Linq;

public static class AlphaFiveUSTreasuries {
    
    public class AlphaFiveUSTreasuries
        : QCAlgorithm {
        
        public int factor;
        
        public object gas;
        
        public object nat;
        
        public object oli;
        
        public List<object> symbols;
        
        public virtual object Initialize() {
            //1. Required: Five years of backtest history
            this.SetStartDate(2014, 1, 1);
            //2. Required: Alpha Streams Models:
            this.SetBrokerageModel(BrokerageName.AlphaStreams);
            //3. Required: Significant AUM Capacity
            this.SetCash(1000000);
            //4. Required: Benchmark to SPY
            this.SetBenchmark("SPY");
            //5. Use InsightWeightingPCM since we will compute the weights
            this.SetPortfolioConstruction(InsightWeightingPortfolioConstructionModel());
            this.SetExecution(ImmediateExecutionModel());
            // Add TradingEconomicsCalendar for Energy Data
            var us = TradingEconomics.Calendar.UnitedStates;
            this.nat = this.AddData(TradingEconomicsCalendar, us.NaturalGasStocksChange).Symbol;
            this.oli = this.AddData(TradingEconomicsCalendar, us.ApiCrudeOilStockChange).Symbol;
            this.gas = this.AddData(TradingEconomicsCalendar, us.GasolineStocksChange).Symbol;
            // Energy Basket 
            var tickers = new List<string> {
                "XLE",
                "IYE",
                "VDE",
                "USO",
                "XES",
                "XOP",
                "UNG",
                "ICLN",
                "ERX",
                "ERY",
                "SCO",
                "UCO",
                "AMJ",
                "BNO",
                "AMLP",
                "OIH",
                "DGAZ",
                "UGAZ",
                "TAN"
            };
            // Add Equity ---------------------------------------------- 
            this.symbols = (from x in tickers
                select this.AddEquity(x).Symbol).ToList();
            this.factor = 0;
            // Emit insights 10 minutes after market open to
            // try to ensure all price data is from the current day
            this.Schedule.On(this.DateRules.EveryDay("XLE"), this.TimeRules.AfterMarketOpen("XLE", 10), this.EveryDayAfterMarketOpen);
        }
        
        public virtual object EveryDayAfterMarketOpen() {
            if (this.factor == 0) {
                return;
            }
            // The weight is factor normialized by the number of symbols
            var weight = this.factor / this.symbols.Count;
            this.factor = 0;
            // Emit Up Price insight
            this.EmitInsights((from x in this.symbols
                select Insight.Price(x, timedelta(15), InsightDirection.Up, null, null, null, weight)).ToList());
        }
        
        public virtual object OnData(object data) {
            // Discard updates before 10 to avoid EveryDayAfterMarketOpen running with today's data
            if (this.Time.hour < 10) {
                return;
            }
            // Compute the factor based on the Actual vs Forecast values
            foreach (var kvp in data.Get(TradingEconomicsCalendar)) {
                var calendar = kvp.Value;
                var actual = calendar.Actual;
                // The reference will be the Forecast, but if not available, use the Previous
                var reference = calendar.Forecast;
                if (reference == null || reference == 0) {
                    reference = calendar.Previous;
                }
                if (reference == null || reference == 0) {
                    reference = actual;
                }
                // Actual was worse than the reference.
                // Bad. Reduce all positions to a minimum
                if (actual < reference) {
                    this.factor = 0.1;
                    continue;
                }
                this.factor = max(0.1, min(1, 1 - actual / reference));
            }
        }
    }
}
