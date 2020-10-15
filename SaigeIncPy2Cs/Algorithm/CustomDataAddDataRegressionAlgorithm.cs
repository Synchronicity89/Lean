
using AddReference = clr.AddReference;

using np = numpy;

public static class CustomDataAddDataRegressionAlgorithm {
    
    static CustomDataAddDataRegressionAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class CustomDataAddDataRegressionAlgorithm
        : QCAlgorithm {
        
        public object googlEquity;
        
        public virtual object Initialize() {
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 10, 11);
            this.SetCash(100000);
            var twxEquity = this.AddEquity("TWX", Resolution.Daily).Symbol;
            var customTwxSymbol = this.AddData(SECReport8K, twxEquity, Resolution.Daily).Symbol;
            this.googlEquity = this.AddEquity("GOOGL", Resolution.Daily).Symbol;
            var customGooglSymbol = this.AddData(SECReport10K, "GOOGL", Resolution.Daily).Symbol;
            var usTreasury = this.AddData(USTreasuryYieldCurveRate, "GOOGL", Resolution.Daily).Symbol;
            var usTreasuryUnderlyingEquity = Symbol.Create("MSFT", SecurityType.Equity, Market.USA);
            var usTreasuryUnderlying = this.AddData(USTreasuryYieldCurveRate, usTreasuryUnderlyingEquity, Resolution.Daily).Symbol;
            var optionSymbol = this.AddOption("TWX", Resolution.Minute).Symbol;
            var customOptionSymbol = this.AddData(SECReport10K, optionSymbol, Resolution.Daily).Symbol;
            if (customTwxSymbol.Underlying != twxEquity) {
                throw new Exception("Underlying symbol for {customTwxSymbol} is not equal to TWX equity. Expected {twxEquity} got {customTwxSymbol.Underlying}");
            }
            if (customGooglSymbol.Underlying != this.googlEquity) {
                throw new Exception("Underlying symbol for {customGooglSymbol} is not equal to GOOGL equity. Expected {self.googlEquity} got {customGooglSymbol.Underlying}");
            }
            if (usTreasury.HasUnderlying) {
                throw new Exception("US Treasury yield curve (no underlying) has underlying when it shouldn't. Found {usTreasury.Underlying}");
            }
            if (!usTreasuryUnderlying.HasUnderlying) {
                throw new Exception("US Treasury yield curve (with underlying) has no underlying Symbol even though we added with Symbol");
            }
            if (usTreasuryUnderlying.Underlying != usTreasuryUnderlyingEquity) {
                throw new Exception("US Treasury yield curve underlying does not equal equity Symbol added. Expected {usTreasuryUnderlyingEquity} got {usTreasuryUnderlying.Underlying}");
            }
            if (customOptionSymbol.Underlying != optionSymbol) {
                throw new Exception("Option symbol not equal to custom underlying symbol. Expected {optionSymbol} got {customOptionSymbol.Underlying}");
            }
            try {
                var customDataNoCache = this.AddData(SECReport10Q, "AAPL", Resolution.Daily);
                throw new Exception("AAPL was found in the SymbolCache, though it should be missing");
            } catch (InvalidOperationException) {
                return;
            }
        }
        
        // OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        // 
        //         Arguments:
        //             data: Slice object keyed by symbol containing the stock data
        //         
        public virtual object OnData(object data) {
            if (!this.Portfolio.Invested && this.Transactions.GetOpenOrders().Count == 0) {
                this.SetHoldings(this.googlEquity, 0.5);
            }
        }
    }
}
