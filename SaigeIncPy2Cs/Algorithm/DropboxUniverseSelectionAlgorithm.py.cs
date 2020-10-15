
using AddReference = clr.AddReference;

using QCAlgorithm = QuantConnect.Algorithm.QCAlgorithm;

using base64;

using System.Collections.Generic;

public static class DropboxUniverseSelectionAlgorithm {
    
    static DropboxUniverseSelectionAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class DropboxUniverseSelectionAlgorithm
        : QCAlgorithm {
        
        public Dictionary<object, object> backtestSymbolsPerDay;
        
        public None changes;
        
        public object current_universe;
        
        public virtual object Initialize() {
            this.SetStartDate(2013, 1, 1);
            this.SetEndDate(2013, 12, 31);
            this.backtestSymbolsPerDay = new Dictionary<object, object> {
            };
            this.current_universe = new List<object>();
            this.UniverseSettings.Resolution = Resolution.Daily;
            this.AddUniverse("my-dropbox-universe", this.selector);
        }
        
        public virtual object selector(object date) {
            object str;
            // handle live mode file format
            if (this.LiveMode) {
                // fetch the file from dropbox
                str = this.Download("https://www.dropbox.com/s/2az14r5xbx4w5j6/daily-stock-picker-live.csv?dl=1");
                // if we have a file for today, return symbols, else leave universe unchanged
                this.current_universe = str.Count > 0 ? str.split(",") : this.current_universe;
                return this.current_universe;
            }
            // backtest - first cache the entire file
            if (this.backtestSymbolsPerDay.Count == 0) {
                // No need for headers for authorization with dropbox, these two lines are for example purposes 
                var byteKey = base64.b64encode("UserName:Password".encode("ASCII"));
                // The headers must be passed to the Download method as dictionary
                var headers = new Dictionary<object, object> {
                    {
                        "Authorization",
                        "Basic ({byteKey.decode(\"ASCII\")})"}};
                str = this.Download("https://www.dropbox.com/s/rmiiktz0ntpff3a/daily-stock-picker-backtest.csv?dl=1", headers);
                foreach (var line in str.splitlines()) {
                    var data = line.split(",");
                    this.backtestSymbolsPerDay[data[0]] = data[1];
                }
            }
            var index = date.strftime("%Y%m%d");
            this.current_universe = this.backtestSymbolsPerDay.get(index, this.current_universe);
            return this.current_universe;
        }
        
        public virtual object OnData(object slice) {
            if (slice.Bars.Count == 0) {
                return;
            }
            if (this.changes == null) {
                return;
            }
            // start fresh
            this.Liquidate();
            var percentage = 1 / slice.Bars.Count;
            foreach (var tradeBar in slice.Bars.Values) {
                this.SetHoldings(tradeBar.Symbol, percentage);
            }
            // reset changes
            this.changes = null;
        }
        
        public virtual object OnSecuritiesChanged(object changes) {
            this.changes = changes;
        }
    }
}
