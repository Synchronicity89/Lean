
using AddReference = clr.AddReference;

using PythonData = QuantConnect.Python.PythonData;

using np = numpy;

using datetime = datetime.datetime;

using json;

using System;

public static class LiveFeaturesAlgorithm {
    
    static LiveFeaturesAlgorithm() {
        AddReference("System.Core");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class LiveTradingFeaturesAlgorithm
        : QCAlgorithm {
        
        //## Initialize the Algorithm and Prepare Required Data
        public virtual object Initialize() {
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 10, 11);
            this.SetCash(25000);
            //#Equity Data for US Markets
            this.AddSecurity(SecurityType.Equity, "IBM", Resolution.Second);
            //#FOREX Data for Weekends: 24/6
            this.AddSecurity(SecurityType.Forex, "EURUSD", Resolution.Minute);
            //#Custom/Bitcoin Live Data: 24/7
            this.AddData(Bitcoin, "BTC", Resolution.Second, TimeZones.Utc);
        }
        
        //## New Bitcoin Data Event
        public static object OnData(object Bitcoin, object data) {
            if (this.LiveMode) {
                this.SetRuntimeStatistic("BTC", data.Close.ToString());
            }
            if (!this.Portfolio.HoldStock) {
                this.MarketOrder("BTC", 100);
                //#Send a notification email/SMS/web request on events:
                this.Notify.Email("myemail@gmail.com", "Test", "Test Body", "test attachment");
                this.Notify.Sms("+11233456789", data.Time.ToString() + ">> Test message from live BTC server.");
                this.Notify.Web("http://api.quantconnect.com", data.Time.ToString() + ">> Test data packet posted from live BTC server.");
            }
        }
        
        //## Raises the data event
        public virtual object OnData(object data) {
            if (!this.Portfolio["IBM"].HoldStock && data.ContainsKey("IBM")) {
                var quantity = Convert.ToInt32(np.floor(this.Portfolio.MarginRemaining / data["IBM"].Close));
                this.MarketOrder("IBM", quantity);
                this.Debug("Purchased IBM on " + this.Time.strftime("%m/%d/%Y").ToString());
                this.Notify.Email("myemail@gmail.com", "Test", "Test Body", "test attachment");
            }
        }
    }
    
    public class Bitcoin
        : PythonData {
        
        public virtual object GetSource(object config, object date, object isLiveMode) {
            if (isLiveMode) {
                return SubscriptionDataSource("https://www.bitstamp.net/api/ticker/", SubscriptionTransportMedium.Rest);
            }
            return SubscriptionDataSource("https://www.quandl.com/api/v3/datasets/BCHARTS/BITSTAMPUSD.csv?order=asc", SubscriptionTransportMedium.RemoteFile);
        }
        
        public virtual object Reader(object config, object line, object date, object isLiveMode) {
            var coin = new Bitcoin();
            coin.Symbol = config.Symbol;
            if (isLiveMode) {
                // Example Line Format:
                // {"high": "441.00", "last": "421.86", "timestamp": "1411606877", "bid": "421.96", "vwap": "428.58", "volume": "14120.40683975", "low": "418.83", "ask": "421.99"}
                try {
                    var liveBTC = json.loads(line);
                    // If value is zero, return None
                    var value = liveBTC["last"];
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
                coin.Time = datetime.strptime(data[0], "%Y-%m-%d");
                coin.Value = float(data[4]);
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
