
using AddReference = clr.AddReference;

using BaseData = QuantConnect.Data.BaseData;

public static class CustomSecurityInitializerAlgorithm {
    
    static CustomSecurityInitializerAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class CustomSecurityInitializerAlgorithm
        : QCAlgorithm {
        
        public virtual object Initialize() {
            // set our initializer to our custom type
            this.SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage);
            var func_security_seeder = FuncSecuritySeeder(Func[Security,BaseData](this.custom_seed_function));
            this.SetSecurityInitializer(new CustomSecurityInitializer(this.BrokerageModel, func_security_seeder, DataNormalizationMode.Raw));
            this.SetStartDate(2013, 10, 1);
            this.SetEndDate(2013, 11, 1);
            this.AddEquity("SPY", Resolution.Hour);
        }
        
        public virtual object OnData(object data) {
            if (!this.Portfolio.Invested) {
                this.SetHoldings("SPY", 1);
            }
        }
        
        public virtual object custom_seed_function(object security) {
            var resolution = Resolution.Hour;
            var df = this.History(security.Symbol, 1, resolution);
            if (df.empty) {
                return null;
            }
            var last_bar = df.unstack(level: 0).iloc[-1];
            var date_time = last_bar.name.to_pydatetime();
            var open = last_bar.open.values[0];
            var high = last_bar.high.values[0];
            var low = last_bar.low.values[0];
            var close = last_bar.close.values[0];
            var volume = last_bar.volume.values[0];
            return TradeBar(date_time, security.Symbol, open, high, low, close, volume, Extensions.ToTimeSpan(resolution));
        }
    }
    
    // Our custom initializer that will set the data normalization mode.
    //     We sub-class the BrokerageModelSecurityInitializer so we can also
    //     take advantage of the default model/leverage setting behaviors
    public class CustomSecurityInitializer
        : BrokerageModelSecurityInitializer {
        
        public object @base;
        
        public object dataNormalizationMode;
        
        public CustomSecurityInitializer(object brokerageModel, object securitySeeder, object dataNormalizationMode) {
            this.@base = BrokerageModelSecurityInitializer(brokerageModel, securitySeeder);
            this.dataNormalizationMode = dataNormalizationMode;
        }
        
        // Initializes the specified security by setting up the models
        //         security -- The security to be initialized
        //         seedSecurity -- True to seed the security, false otherwise
        public virtual object Initialize(object security) {
            // first call the default implementation
            this.@base.Initialize(security);
            // now apply our data normalization mode
            security.SetDataNormalizationMode(this.dataNormalizationMode);
        }
    }
}
