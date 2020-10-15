
using AddReference = clr.AddReference;

public static class USTreasuryYieldCurveDataAlgorithm {
    
    static USTreasuryYieldCurveDataAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    public class USTreasuryYieldCurveDataAlgorithm
        : QCAlgorithm {
        
        public object symbol;
        
        public virtual object Initialize() {
            this.SetStartDate(2017, 1, 1);
            this.SetEndDate(2019, 6, 30);
            this.SetCash(100000);
            // Define the symbol and "type" of our generic data:
            this.symbol = this.AddData(USTreasuryYieldCurveRate, "USTYC", Resolution.Daily).Symbol;
        }
        
        public virtual object OnData(object slice) {
            if (!slice.ContainsKey(this.symbol)) {
                return;
            }
            var curve = slice[this.symbol];
            this.Log("{self.Time} - 1M: {curve.OneMonth}, 2M: {curve.TwoMonth}, 3M: {curve.ThreeMonth}, 6M: {curve.SixMonth}, 1Y: {curve.OneYear}, 2Y: {curve.TwoYear}, 3Y: {curve.ThreeYear}, 5Y: {curve.FiveYear}, 10Y: {curve.TenYear}, 20Y: {curve.TwentyYear}, 30Y: {curve.ThirtyYear}");
        }
    }
}
