namespace Benchmarks {
    
    using AddReference = clr.AddReference;
    
    using System.Collections.Generic;
    
    using System.Collections;
    
    public static class SmartInsiderEventBenchmarkAlgorithm {
        
        static SmartInsiderEventBenchmarkAlgorithm() {
            AddReference("System");
            AddReference("QuantConnect.Algorithm");
            AddReference("QuantConnect.Common");
        }
        
        public class SmartInsiderEventBenchmarkAlgorithm
            : QCAlgorithm {
            
            public List<object> customSymbols;
            
            public List<object> securities;
            
            public virtual object Initialize() {
                // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
                this.SetStartDate(2010, 1, 1);
                this.SetEndDate(2019, 1, 1);
                var tickers = new HashSet({
                    "AAPL"}, {
                    "AMZN"}, {
                    "MSFT"}, {
                    "IBM"}, {
                    "FB"}, {
                    "QQQ"}, {
                    "IWM"}, {
                    "BAC"}, {
                    "BNO"}, {
                    "AIG"}, {
                    "UW"}, {
                    "WM"});
                this.securities = new List<object>();
                this.customSymbols = new List<object>();
                foreach (var ticker in tickers) {
                    var security = this.AddEquity(ticker, Resolution.Hour);
                    this.securities.append(security);
                    var intetion = this.AddData(SmartInsiderIntention, security.Symbol, Resolution.Daily);
                    var transaction = this.AddData(SmartInsiderTransaction, security.Symbol, Resolution.Daily);
                    this.customSymbols.append(intetion.Symbol);
                    this.customSymbols.append(transaction.Symbol);
                }
                this.Schedule.On(this.DateRules.EveryDay(), this.TimeRules.At(16, 0), this.DailyRebalance);
            }
            
            public virtual object OnData(object slice) {
                var intentions = slice.Get(SmartInsiderIntention);
                var transactions = slice.Get(SmartInsiderTransaction);
            }
            
            public virtual object DailyRebalance() {
                var history = this.History(this.customSymbols, timedelta(5));
                var historySymbolCount = history.index.Count;
                foreach (var security in this.securities) {
                    var intention = security.Data.Get(SmartInsiderIntention);
                    var transaction = security.Data.Get(SmartInsiderTransaction);
                    if (!security.HoldStock && intention != null && transaction != null) {
                        this.SetHoldings(security.Symbol, 1 / this.securities.Count);
                    }
                }
            }
        }
    }
}
