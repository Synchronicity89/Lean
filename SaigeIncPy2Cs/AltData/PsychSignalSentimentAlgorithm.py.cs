namespace AltData {
    
    using AddReference = clr.AddReference;
    
    using datetime = datetime.datetime;
    
    using timedelta = datetime.timedelta;
    
    using System.Collections.Generic;
    
    using System.Linq;
    
    public static class PsychSignalSentimentAlgorithm {
        
        static PsychSignalSentimentAlgorithm() {
            AddReference("System");
            AddReference("QuantConnect.Algorithm");
            AddReference("QuantConnect.Common");
        }
        
        public class PsychSignalSentimentAlgorithm
            : QCAlgorithm {
            
            public object timeEntered;
            
            public virtual object Initialize() {
                this.SetStartDate(2018, 3, 1);
                this.SetEndDate(2018, 10, 1);
                this.SetCash(100000);
                this.AddUniverseSelection(CoarseFundamentalUniverseSelectionModel(this.CoarseUniverse));
                this.timeEntered = new datetime(1, 1, 1);
                // Request underlying equity data.
                var ibm = this.AddEquity("IBM", Resolution.Minute).Symbol;
                // Add sentiment data for the underlying IBM asset
                var psy = this.AddData(PsychSignalSentiment, ibm).Symbol;
                // Request 120 minutes of history with the PsychSignal IBM Custom Data Symbol
                var history = this.History(PsychSignalSentiment, psy, 120, Resolution.Minute);
                // Count the number of items we get from our history request
                this.Debug("We got {len(history)} items from our history request");
            }
            
            // You can use custom data with a universe of assets.
            public virtual object CoarseUniverse(object coarse) {
                if (this.Time - this.timeEntered <= new timedelta(days: 10)) {
                    return Universe.Unchanged;
                }
                // Ask for the universe like normal and then filter it
                var symbols = (from i in coarse
                    where i.HasFundamentalData && i.DollarVolume > 50000000
                    select i.Symbol).ToList()[::20];
                // Add the custom data to the underlying security.
                foreach (var symbol in symbols) {
                    this.AddData(PsychSignalSentiment, symbol);
                }
                return symbols;
            }
            
            public virtual object OnData(object data) {
                // Scan our last time traded to prevent churn.
                if (this.Time - this.timeEntered <= new timedelta(days: 10)) {
                    return;
                }
                // Fetch the PsychSignal data for the active securities and trade on any
                foreach (var security in this.ActiveSecurities.Values) {
                    var tweets = security.Data.GetAll(PsychSignalSentiment);
                    foreach (var sentiment in tweets) {
                        if (sentiment.BullIntensity > 2.0 && sentiment.BullScoredMessages > 3) {
                            this.SetHoldings(sentiment.Symbol.Underlying, 0.05);
                            this.timeEntered = this.Time;
                        }
                    }
                }
            }
            
            // When adding custom data from a universe we should also remove the data afterwards.
            public virtual object OnSecuritiesChanged(object changes) {
                foreach (var r in changes.RemovedSecurities) {
                    this.Liquidate(r.Symbol);
                    // Remove the custom data from our algorithm and collection
                    this.RemoveSecurity(Symbol.CreateBase(PsychSignalSentiment, r.Symbol, Market.USA));
                }
            }
        }
    }
}
