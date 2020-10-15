
using AddReference = clr.AddReference;

using SubscriptionDataSource = QuantConnect.Data.SubscriptionDataSource;

using PythonData = QuantConnect.Python.PythonData;

using date = datetime.date;

using timedelta = datetime.timedelta;

using datetime = datetime.datetime;

using List = System.Collections.Generic.List;

using QCAlgorithm = QuantConnect.Algorithm.QCAlgorithm;

using np = numpy;

using math;

using json;

using System.Collections.Generic;

public static class DropboxBaseDataUniverseSelectionAlgorithm {
    
    static DropboxBaseDataUniverseSelectionAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class DropboxBaseDataUniverseSelectionAlgorithm
        : QCAlgorithm {
        
        public object _changes;
        
        public virtual object Initialize() {
            this.UniverseSettings.Resolution = Resolution.Daily;
            this.SetStartDate(2013, 1, 1);
            this.SetEndDate(2013, 12, 31);
            this.AddUniverse(StockDataSource, "my-stock-data-source", this.stockDataSource);
        }
        
        public virtual object stockDataSource(object data) {
            var list = new List<object>();
            foreach (var item in data) {
                foreach (var symbol in item["Symbols"]) {
                    list.append(symbol);
                }
            }
            return list;
        }
        
        public virtual object OnData(object slice) {
            if (slice.Bars.Count == 0) {
                return;
            }
            if (this._changes == null) {
                return;
            }
            // start fresh
            this.Liquidate();
            var percentage = 1 / slice.Bars.Count;
            foreach (var tradeBar in slice.Bars.Values) {
                this.SetHoldings(tradeBar.Symbol, percentage);
            }
            // reset changes
            this._changes = null;
        }
        
        public virtual object OnSecuritiesChanged(object changes) {
            this._changes = changes;
        }
    }
    
    public class StockDataSource
        : PythonData {
        
        public virtual object GetSource(object config, object date, object isLiveMode) {
            var url = isLiveMode ? "https://www.dropbox.com/s/2az14r5xbx4w5j6/daily-stock-picker-live.csv?dl=1" : "https://www.dropbox.com/s/rmiiktz0ntpff3a/daily-stock-picker-backtest.csv?dl=1";
            return SubscriptionDataSource(url, SubscriptionTransportMedium.RemoteFile);
        }
        
        public virtual object Reader(object config, object line, object date, object isLiveMode) {
            if (!(line.strip() && line[0].isdigit())) {
                return null;
            }
            var stocks = new StockDataSource();
            stocks.Symbol = config.Symbol;
            var csv = line.split(",");
            if (isLiveMode) {
                stocks.Time = date;
                stocks["Symbols"] = csv;
            } else {
                stocks.Time = datetime.strptime(csv[0], "%Y%m%d");
                stocks["Symbols"] = csv[1];
            }
            return stocks;
        }
    }
}
