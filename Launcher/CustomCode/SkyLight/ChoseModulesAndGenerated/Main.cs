namespace QuantConnect.Algorithm.CSharp
{
    using QuantConnect.Algorithm;
    using QuantConnect.Algorithm.Framework.Alphas;
    using QuantConnect.Algorithm.Framework.Execution;
    using QuantConnect.Algorithm.Framework.Portfolio;
    using QuantConnect.Algorithm.Framework.Risk;
    using QuantConnect.Algorithm.Framework.Selection;
    using QuantConnect.Data;
    using QuantConnect.Data.Market;
    using QuantConnect.Data.UniverseSelection;
    using QuantConnect.Orders;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

	using QuantConnect.Algorithm.Framework.Selection;
    public class UncoupledCalibratedReplicator : QCAlgorithm
    {

        public override void Initialize()
        {
            SetStartDate(2014, 1, 1);  //Set Start Date
            SetCash(100000);             //Set Strategy Cash
            Func<DateTime, IEnumerable<Symbol>> selector = dt => new Symbol[] { };
            
            AddEquity("SPY", Resolution.Minute);

			AddAlpha(new CboeVixAlphaModel(this));

			SetExecution(new StandardDeviationExecutionModel(60, 2, Resolution.Minute));

			SetPortfolioConstruction(new InsightWeightingPortfolioConstructionModel());

			SetRiskManagement(new TrailingStopRiskManagementModel(0.03m));

			SetUniverseSelection(new OptionsUniverseSelectionModel(selector));
	

        }

        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// Slice object keyed by symbol containing the stock data
        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                SetHoldings("SPY", 1);
                Debug("Purchased Stock");
            }
        }
        
        
  
    }
    
    //public class OptionsUniverseSelectionModel : OptionUniverseSelectionModel
    //{
    //    //private OptionsUniverseSelectionModel() {}
    //    public OptionsUniverseSelectionModel(Func<DateTime, IEnumerable<Symbol>> select_option_chain_symbols) : base(TimeSpan.FromMinutes(1), select_option_chain_symbols)
    //    {
    //        
    //    }
//
    //    protected override OptionFilterUniverse Filter(OptionFilterUniverse filter)
    //    {
    //        // Define options filter -- strikes +/- 3 and expiry between 0 and 180 days away
    //        return (filter.Strikes(-20, +20)
    //                      .Expiration(TimeSpan.FromMinutes(0), TimeSpan.FromMinutes(30)));
    //    }
    //}
}