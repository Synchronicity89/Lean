namespace AltData {
    
    using AddReference = clr.AddReference;
    
    using datetime = datetime.datetime;
    
    using timedelta = datetime.timedelta;
    
    public static class CachedAlternativeDataAlgorithm {
        
        static CachedAlternativeDataAlgorithm() {
            AddReference("System");
            AddReference("QuantConnect.Algorithm");
            AddReference("QuantConnect.Common");
        }
        
        public class CachedAlternativeDataAlgorithm
            : QCAlgorithm {
            
            public object cboeVix;
            
            public object fredPeakToTrough;
            
            public object usEnergy;
            
            public virtual object Initialize() {
                this.SetStartDate(2003, 1, 1);
                this.SetEndDate(2019, 10, 11);
                this.SetCash(100000);
                // QuantConnect caches a small subset of alternative data for easy consumption for the community.
                // You can use this in your algorithm as demonstrated below:
                this.cboeVix = this.AddData(CBOE, "VIX", Resolution.Daily).Symbol;
                // United States EIA data: https://eia.gov/
                this.usEnergy = this.AddData(USEnergy, USEnergy.Petroleum.UnitedStates.WeeklyGrossInputsIntoRefineries, Resolution.Daily).Symbol;
                // FRED data
                this.fredPeakToTrough = this.AddData(Fred, Fred.OECDRecessionIndicators.UnitedStatesFromPeakThroughTheTrough, Resolution.Daily).Symbol;
            }
            
            public virtual object OnData(object data) {
                if (data.ContainsKey(this.cboeVix)) {
                    var vix = data.Get(CBOE, this.cboeVix);
                    this.Log("VIX: {vix}");
                }
                if (data.ContainsKey(this.usEnergy)) {
                    var inputIntoRefineries = data.Get(USEnergy, this.usEnergy);
                    this.Log("U.S. Input Into Refineries: {inputIntoRefineries}");
                }
                if (data.ContainsKey(this.fredPeakToTrough)) {
                    var peakToTrough = data.Get(Fred, this.fredPeakToTrough);
                    this.Log("OECD based Recession Indicator for the United States from the Peak through the Trough: {peakToTrough}");
                }
            }
        }
    }
}
