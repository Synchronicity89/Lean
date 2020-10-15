
using AddReference = clr.AddReference;

using PsychSignalSentiment = QuantConnect.Data.Custom.PsychSignal.PsychSignalSentiment;

public static class PsychSignalSentimentRegressionAlgorithm {
    
    static PsychSignalSentimentRegressionAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class PsychSignalSentimentRegressionAlgorithm
        : QCAlgorithm {
        
        public object symbol;
        
        public string ticker;
        
        // Initialize the algorithm with our custom data
        public virtual object Initialize() {
            this.SetStartDate(2019, 6, 3);
            this.SetEndDate(2019, 6, 9);
            this.SetCash(100000);
            this.ticker = "AAPL";
            this.symbol = this.AddEquity(this.ticker).Symbol;
            this.AddData(PsychSignalSentiment, this.ticker);
        }
        
        // Loads each new data point into the algorithm. On sentiment data, we place orders depending on the sentiment
        public virtual object OnData(object slice) {
            foreach (var message in slice.Values) {
                // Price data can be lumped in with the values. We only want to work with
                // sentiment data, so we filter out any TradeBars that might make their way in here
                if (!(message is PsychSignalSentiment)) {
                    return;
                }
                if (!this.Portfolio.Invested && this.Transactions.GetOpenOrders().Count == 0 && slice.ContainsKey(this.symbol) && message.BullIntensity > 1.5 && message.BullScoredMessages > 3.0) {
                    this.SetHoldings(this.symbol, 0.25);
                } else if (this.Portfolio.Invested && message.BearIntensity > 1.5 && message.BearScoredMessages > 3.0) {
                    this.Liquidate(this.symbol);
                }
            }
        }
    }
}
