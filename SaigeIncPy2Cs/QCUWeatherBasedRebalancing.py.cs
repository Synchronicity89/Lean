
using AddReference = clr.AddReference;

using SubscriptionDataSource = QuantConnect.Data.SubscriptionDataSource;

using PythonData = QuantConnect.Python.PythonData;

using datetime = datetime.datetime;

using timedelta = datetime.timedelta;

public static class QCUWeatherBasedRebalancing {
    
    static QCUWeatherBasedRebalancing() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class QCUWeatherBasedRebalancing
        : QCAlgorithm {
        
        public int rebalanceFrequency;
        
        public object symbol;
        
        public int tradingDayCount;
        
        public object weather;
        
        public virtual object Initialize() {
            this.SetStartDate(2013, 1, 1);
            this.SetEndDate(2016, 1, 1);
            this.SetCash(25000);
            this.AddEquity("SPY", Resolution.Daily);
            this.symbol = this.Securities["SPY"].Symbol;
            // KNYC is NYC Central Park. Find other locations at
            // https://www.wunderground.com/history/
            this.AddData(Weather, "KNYC", Resolution.Minute);
            this.weather = this.Securities["KNYC"].Symbol;
            this.tradingDayCount = 0;
            this.rebalanceFrequency = 10;
        }
        
        // When we have a new event trigger, buy some stock:
        public virtual object OnData(object data) {
            if (!data.ContainsKey(this.weather)) {
                return;
            }
            // Scale from -5C to +25C :: -5C == 100%, +25C = 0% invested
            var fraction = data.Contains(this.weather) ? -data[this.weather].MinC + 5 / 30 : 0;
            //self.Debug("Faction {0}".format(faction))
            // Rebalance every 10 days:
            if (this.tradingDayCount >= this.rebalanceFrequency) {
                this.SetHoldings(this.symbol, fraction);
                this.tradingDayCount = 0;
            }
        }
        
        public virtual object OnEndOfDay() {
            this.tradingDayCount += 1;
        }
    }
    
    //  Weather based rebalancing
    public class Weather
        : PythonData {
        
        public virtual object GetSource(object config, object date, object isLive) {
            var source = "https://dl.dropboxusercontent.com/u/44311500/KNYC.csv";
            source = "https://www.wunderground.com/history/airport/{0}/{1}/1/1/CustomHistory.html?dayend=31&monthend=12&yearend={1}&format=1".format(config.Symbol, date.year);
            return SubscriptionDataSource(source, SubscriptionTransportMedium.RemoteFile);
        }
        
        public virtual object Reader(object config, object line, object date, object isLive) {
            // If first character is not digit, pass
            if (!(line.strip() && line[0].isdigit())) {
                return null;
            }
            var data = line.split(",");
            var weather = new Weather();
            weather.Symbol = config.Symbol;
            weather.Time = datetime.strptime(data[0], "%Y-%m-%d") + new timedelta(hours: 20);
            // If the second column is an invalid value (empty string), return None. The algorithm will discard it.
            if (!data[2]) {
                return null;
            }
            weather.Value = data[2];
            weather["Max.C"] = float(data[1]);
            weather["Min.C"] = float(data[3]);
            return weather;
        }
    }
}
