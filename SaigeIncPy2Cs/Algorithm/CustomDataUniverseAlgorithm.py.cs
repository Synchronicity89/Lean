
using AddReference = clr.AddReference;

using QCAlgorithm = QuantConnect.Algorithm.QCAlgorithm;

using SubscriptionDataSource = QuantConnect.Data.SubscriptionDataSource;

using PythonData = QuantConnect.Python.PythonData;

using date = datetime.date;

using timedelta = datetime.timedelta;

using datetime = datetime.datetime;

using System.Linq;

using System;

public static class CustomDataUniverseAlgorithm {
    
    static CustomDataUniverseAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class CustomDataUniverseAlgorithm
        : QCAlgorithm {
        
        public object _changes;
        
        public virtual object Initialize() {
            // Data ADDED via universe selection is added with Daily resolution.
            this.UniverseSettings.Resolution = Resolution.Daily;
            this.SetStartDate(2015, 1, 5);
            this.SetEndDate(2015, 7, 1);
            this.SetCash(100000);
            this.AddEquity("SPY", Resolution.Daily);
            this.SetBenchmark("SPY");
            // add a custom universe data source (defaults to usa-equity)
            this.AddUniverse(NyseTopGainers, "universe-nyse-top-gainers", Resolution.Daily, this.nyseTopGainers);
        }
        
        public virtual object nyseTopGainers(object data) {
            return (from x in data
                where x["TopGainersRank"] <= 2
                select x.Symbol).ToList();
        }
        
        public virtual object OnData(object slice) {
        }
        
        public virtual object OnSecuritiesChanged(object changes) {
            this._changes = changes;
            foreach (var security in changes.RemovedSecurities) {
                //  liquidate securities that have been removed
                if (security.Invested) {
                    this.Liquidate(security.Symbol);
                    this.Log("Exit {0} at {1}".format(security.Symbol, security.Close));
                }
            }
            foreach (var security in changes.AddedSecurities) {
                // enter short positions on new securities
                if (!security.Invested && security.Close != 0) {
                    var qty = this.CalculateOrderQuantity(security.Symbol, -0.25);
                    this.MarketOnOpenOrder(security.Symbol, qty);
                    this.Log("Enter {0} at {1}".format(security.Symbol, security.Close));
                }
            }
        }
    }
    
    public class NyseTopGainers
        : PythonData {
        
        public int count;
        
        public date last_date;
        
        public NyseTopGainers() {
            this.count = 0;
            this.last_date = datetime.min;
        }
        
        public virtual object GetSource(object config, object date, object isLiveMode) {
            var url = isLiveMode ? "http://www.wsj.com/mdc/public/page/2_3021-gainnyse-gainer.html" : "https://www.dropbox.com/s/vrn3p38qberw3df/nyse-gainers.csv?dl=1";
            return SubscriptionDataSource(url, SubscriptionTransportMedium.RemoteFile);
        }
        
        public virtual object Reader(object config, object line, object date, object isLiveMode) {
            object nyse;
            if (!isLiveMode) {
                // backtest gets data from csv file in dropbox
                if (!(line.strip() && line[0].isdigit())) {
                    return null;
                }
                var csv = line.split(",");
                nyse = new NyseTopGainers();
                nyse.Time = datetime.strptime(csv[0], "%Y%m%d");
                nyse.EndTime = nyse.Time + new timedelta(1);
                nyse.Symbol = Symbol.Create(csv[1], SecurityType.Equity, Market.USA);
                nyse["TopGainersRank"] = Convert.ToInt32(csv[2]);
                return nyse;
            }
            if (this.last_date != date) {
                // reset our counter for the new day
                this.last_date = date;
                this.count = 0;
            }
            // parse the html into a symbol
            if (!line.startswith("<a href=\"/public/quotes/main.html?symbol=")) {
                // we're only looking for lines that contain the symbols
                return null;
            }
            var last_close_paren = line.rfind(")");
            var last_open_paren = line.rfind("(");
            if (last_open_paren == -1 || last_close_paren == -1) {
                return null;
            }
            var symbol_string = line[(last_open_paren  +  1)::last_close_paren];
            nyse = new NyseTopGainers();
            nyse.Time = date;
            nyse.EndTime = nyse.Time + new timedelta(1);
            nyse.Symbol = Symbol.Create(symbol_string, SecurityType.Equity, Market.USA);
            nyse["TopGainersRank"] = this.count;
            this.count = this.count + 1;
            return nyse;
        }
    }
}
