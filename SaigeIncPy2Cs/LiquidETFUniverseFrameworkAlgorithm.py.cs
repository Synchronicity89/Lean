
using AddReference = clr.AddReference;

using timedelta = datetime.timedelta;

using System.Collections.Generic;

using System.Linq;

public static class LiquidETFUniverseFrameworkAlgorithm {
    
    static LiquidETFUniverseFrameworkAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Algorithm.Framework");
        AddReference("QuantConnect.Common");
    }
    
    // Basic template framework algorithm uses framework components to define the algorithm.
    public class LiquidETFUniverseFrameworkAlgorithm
        : QCAlgorithm {
        
        public List<object> symbols;
        
        public virtual object Initialize() {
            // Set Start Date so that backtest has 5+ years of data
            this.SetStartDate(2014, 11, 1);
            // No need to set End Date as the final submission will be tested
            // up until the review date
            // Set $1m Strategy Cash to trade significant AUM
            this.SetCash(1000000);
            // Add a relevant benchmark, with the default being SPY
            this.SetBenchmark("SPY");
            // Use the Alpha Streams Brokerage Model, developed in conjunction with
            // funds to model their actual fees, costs, etc.
            // Please do not add any additional reality modelling, such as Slippage, Fees, Buying Power, etc.
            this.SetBrokerageModel(AlphaStreamsBrokerageModel());
            // Use the LiquidETFUniverse with minute-resolution data
            this.UniverseSettings.Resolution = Resolution.Minute;
            this.SetUniverseSelection(LiquidETFUniverse());
            // Optional
            this.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel());
            this.SetExecution(ImmediateExecutionModel());
            // List of symbols we want to trade. Set it in OnSecuritiesChanged
            this.symbols = new List<object>();
        }
        
        public virtual object OnData(object slice) {
            if (all((from x in this.symbols
                select this.Portfolio[x].Invested).ToList())) {
                return;
            }
            // Emit insights
            var insights = (from x in this.symbols
                where this.Securities[x].Price > 0
                select Insight.Price(x, new timedelta(1), InsightDirection.Up)).ToList();
            if (insights.Count > 0) {
                this.EmitInsights(insights);
            }
        }
        
        public virtual object OnSecuritiesChanged(object changes) {
            // Set symbols as the Inverse Energy ETFs
            foreach (var security in changes.AddedSecurities) {
                if (LiquidETFUniverse.Energy.Inverse.Contains(security.Symbol)) {
                    this.symbols.append(security.Symbol);
                }
            }
            // Print out the information about the groups
            this.Log("Energy: {LiquidETFUniverse.Energy}");
            this.Log("Metals: {LiquidETFUniverse.Metals}");
            this.Log("Technology: {LiquidETFUniverse.Technology}");
            this.Log("Treasuries: {LiquidETFUniverse.Treasuries}");
            this.Log("Volatility: {LiquidETFUniverse.Volatility}");
            this.Log("SP500Sectors: {LiquidETFUniverse.SP500Sectors}");
        }
    }
}
