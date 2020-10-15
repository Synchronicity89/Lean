namespace Benchmarks {
    
    using AddReference = clr.AddReference;
    
    using System.Collections.Generic;
    
    using System.Collections;
    
    public static class SECReportBenchmarkAlgorithm {
        
        static SECReportBenchmarkAlgorithm() {
            AddReference("System");
            AddReference("QuantConnect.Algorithm");
            AddReference("QuantConnect.Common");
        }
        
        public class SECReportBenchmarksAlgorithm
            : QCAlgorithm {
            
            public List<object> securities;
            
            public virtual object Initialize() {
                // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
                this.SetStartDate(2018, 1, 1);
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
                foreach (var ticker in tickers) {
                    var security = this.AddEquity(ticker);
                    this.securities.append(security);
                    this.AddData(SECReport10K, security.Symbol, Resolution.Daily);
                    this.AddData(SECReport8K, security.Symbol, Resolution.Daily);
                }
            }
            
            public virtual object OnData(object slice) {
                foreach (var security in this.securities) {
                    var report8K = security.Data.Get(SECReport8K);
                    var report10K = security.Data.Get(SECReport10K);
                    if (!security.HoldStock && report8K != null && report10K != null) {
                        this.SetHoldings(security.Symbol, 1 / this.securities.Count);
                    }
                }
            }
        }
    }
}
