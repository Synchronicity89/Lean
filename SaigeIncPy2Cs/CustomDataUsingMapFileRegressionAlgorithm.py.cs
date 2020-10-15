
using AddReference = clr.AddReference;

using datetime = datetime.datetime;

using System.Collections.Generic;

public static class CustomDataUsingMapFileRegressionAlgorithm {
    
    static CustomDataUsingMapFileRegressionAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class CustomDataUsingMapFileRegressionAlgorithm
        : QCAlgorithm {
        
        public bool executionMapping;
        
        public object foxa;
        
        public bool initialMapping;
        
        public object symbol;
        
        public virtual object Initialize() {
            // Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
            this.SetStartDate(2013, 6, 27);
            this.SetEndDate(2013, 7, 2);
            this.initialMapping = false;
            this.executionMapping = false;
            this.foxa = Symbol.Create("FOXA", SecurityType.Equity, Market.USA);
            this.symbol = this.AddData(CustomDataUsingMapping, this.foxa).Symbol;
            foreach (var config in this.SubscriptionManager.SubscriptionDataConfigService.GetSubscriptionDataConfigs(this.symbol)) {
                if (config.Resolution != Resolution.Minute) {
                    throw new ValueError("Expected resolution to be set to Minute");
                }
            }
        }
        
        public virtual object OnData(object slice) {
            var date = this.Time.date();
            if (slice.SymbolChangedEvents.ContainsKey(this.symbol)) {
                var mappingEvent = slice.SymbolChangedEvents[this.symbol];
                this.Log("{0} - Ticker changed from: {1} to {2}".format(this.Time.ToString(), mappingEvent.OldSymbol, mappingEvent.NewSymbol));
                if (date == new datetime(2013, 6, 27).date()) {
                    // we should Not receive the initial mapping event
                    if (mappingEvent.NewSymbol != "NWSA" || mappingEvent.OldSymbol != "FOXA") {
                        throw new Exception("Unexpected mapping event mappingEvent");
                    }
                    this.initialMapping = true;
                }
                if (date == new datetime(2013, 6, 29).date()) {
                    if (mappingEvent.NewSymbol != "FOXA" || mappingEvent.OldSymbol != "NWSA") {
                        throw new Exception("Unexpected mapping event mappingEvent");
                    }
                    this.SetHoldings(this.symbol, 1);
                    this.executionMapping = true;
                }
            }
        }
        
        public virtual object OnEndOfAlgorithm() {
            if (this.initialMapping) {
                throw new Exception("The ticker generated the initial rename event");
            }
            if (!this.executionMapping) {
                throw new Exception("The ticker did not rename throughout the course of its life even though it should have");
            }
        }
    }
    
    // Test example custom data showing how to enable the use of mapping.
    //     Implemented as a wrapper of existing NWSA->FOXA equity
    public class CustomDataUsingMapping
        : PythonData {
        
        public virtual object GetSource(object config, object date, object isLiveMode) {
            return TradeBar().GetSource(SubscriptionDataConfig(config, CustomDataUsingMapping, Symbol.Create(config.MappedSymbol, SecurityType.Equity, config.Market)), date, isLiveMode);
        }
        
        public virtual object Reader(object config, object line, object date, object isLiveMode) {
            return TradeBar.ParseEquity(config, line, date);
        }
        
        // True indicates mapping should be done
        public virtual object RequiresMapping() {
            return true;
        }
        
        // Indicates that the data set is expected to be sparse
        public virtual object IsSparseData() {
            return true;
        }
        
        // Gets the default resolution for this data and security type
        public virtual object DefaultResolution() {
            return Resolution.Minute;
        }
        
        // Gets the supported resolution for this data and security type
        public virtual object SupportedResolutions() {
            return new List<object> {
                Resolution.Minute
            };
        }
    }
}
