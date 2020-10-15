
using AddReference = clr.AddReference;

using SubscriptionDataSource = QuantConnect.Data.SubscriptionDataSource;

using PythonData = QuantConnect.Python.PythonData;

using date = datetime.date;

using timedelta = datetime.timedelta;

using datetime = datetime.datetime;

using np = numpy;

using json;

public static class CustomDataBitcoinAlgorithm {
    
    static CustomDataBitcoinAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class CustomDataBitcoinAlgorithm
        : QCAlgorithm {
        
        public virtual object Initialize() {
            this.SetStartDate(2011, 9, 13);
            this.SetEndDate(datetime.now().date() - new timedelta(1));
            this.SetCash(100000);
            // Define the symbol and "type" of our generic data:
            this.AddData(Bitcoin, "BTC");
        }
        
        public virtual object OnData(object data) {
            if (!data.ContainsKey("BTC")) {
                return;
            }
            var close = data["BTC"].Close;
            // If we don't have any weather "SHARES" -- invest"
            if (!this.Portfolio.Invested) {
                // Weather used as a tradable asset, like stocks, futures etc.
                this.SetHoldings("BTC", 1);
                this.Debug("Buying BTC 'Shares': BTC: {0}".format(close));
            }
            this.Debug("Time: {0} {1}".format(datetime.now(), close));
        }
    }
    
    // Custom Data Type: Bitcoin data from Quandl - http://www.quandl.com/help/api-for-bitcoin-data
    public class Bitcoin
        : PythonData {
        
        public virtual object GetSource(object config, object date, object isLiveMode) {
            if (isLiveMode) {
                return SubscriptionDataSource("https://www.bitstamp.net/api/ticker/", SubscriptionTransportMedium.Rest);
            }
            //return "http://my-ftp-server.com/futures-data-" + date.ToString("Ymd") + ".zip";
            // OR simply return a fixed small data file. Large files will slow down your backtest
            return SubscriptionDataSource("https://www.quandl.com/api/v3/datasets/BCHARTS/BITSTAMPUSD.csv?order=asc", SubscriptionTransportMedium.RemoteFile);
        }
        
        public virtual object Reader(object config, object line, object date, object isLiveMode) {
            object value;
            var coin = new Bitcoin();
            coin.Symbol = config.Symbol;
            if (isLiveMode) {
                // Example Line Format:
                // {"high": "441.00", "last": "421.86", "timestamp": "1411606877", "bid": "421.96", "vwap": "428.58", "volume": "14120.40683975", "low": "418.83", "ask": "421.99"}
                try {
                    var liveBTC = json.loads(line);
                    // If value is zero, return None
                    value = liveBTC["last"];
                    if (value == 0) {
                        return null;
                    }
                    coin.Time = datetime.now();
                    coin.Value = value;
                    coin["Open"] = float(liveBTC["open"]);
                    coin["High"] = float(liveBTC["high"]);
                    coin["Low"] = float(liveBTC["low"]);
                    coin["Close"] = float(liveBTC["last"]);
                    coin["Ask"] = float(liveBTC["ask"]);
                    coin["Bid"] = float(liveBTC["bid"]);
                    coin["VolumeBTC"] = float(liveBTC["volume"]);
                    coin["WeightedPrice"] = float(liveBTC["vwap"]);
                    return coin;
                } catch (ValueError) {
                    // Do nothing, possible error in json decoding
                    return null;
                }
            }
            // Example Line Format:
            // Date      Open   High    Low     Close   Volume (BTC)    Volume (Currency)   Weighted Price
            // 2011-09-13 5.8    6.0     5.65    5.97    58.37138238,    346.0973893944      5.929230648356
            if (!(line.strip() && line[0].isdigit())) {
                return null;
            }
            try {
                var data = line.split(",");
                // If value is zero, return None
                value = data[4];
                if (value == 0) {
                    return null;
                }
                coin.Time = datetime.strptime(data[0], "%Y-%m-%d");
                coin.Value = value;
                coin["Open"] = float(data[1]);
                coin["High"] = float(data[2]);
                coin["Low"] = float(data[3]);
                coin["Close"] = float(data[4]);
                coin["VolumeBTC"] = float(data[5]);
                coin["VolumeUSD"] = float(data[6]);
                coin["WeightedPrice"] = float(data[7]);
                return coin;
            } catch (ValueError) {
                // Do nothing, possible error in json decoding
                return null;
            }
        }
    }
}
