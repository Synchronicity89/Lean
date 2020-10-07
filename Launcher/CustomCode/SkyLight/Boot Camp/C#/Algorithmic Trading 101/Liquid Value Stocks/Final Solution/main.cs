namespace QuantConnect
{
    using QuantConnect.Algorithm;
    using QuantConnect.Algorithm.Framework.Alphas;
    using QuantConnect.Algorithm.Framework.Execution;
    using QuantConnect.Algorithm.Framework.Portfolio;
    using QuantConnect.Algorithm.Framework.Risk;
    using QuantConnect.Algorithm.Framework.Selection;
    using QuantConnect.Data;
    using QuantConnect.Data.Fundamental;
    using QuantConnect.Data.UniverseSelection;
    using QuantConnect.Orders;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;


    public partial class LiquidValueStocks : QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2016, 10, 1);
            SetEndDate(2017, 10, 1);
            SetCash(100000);
            UniverseSettings.Resolution = Resolution.Hour;
            AddUniverseSelection(new LiquidValueUniverseSelectionModel());
            AddAlpha(new NullAlphaModel());
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());
            SetExecution(new ImmediateExecutionModel());
        }
        
    }

    public class LiquidValueUniverseSelectionModel : FundamentalUniverseSelectionModel
    {
        private int _lastMonth = -1;
        List<string> universe = new List<string>();

        public LiquidValueUniverseSelectionModel()
            : base(true, null, null)
        {
        }

        public override IEnumerable<Symbol> SelectCoarse(QCAlgorithm algorithm,
            IEnumerable<CoarseFundamental> coarse)
        {
            if (_lastMonth == algorithm.Time.Month)
            {
                return Universe.Unchanged;
            }

            _lastMonth = algorithm.Time.Month;

            var sortedByDollarVolume = coarse
                .Where(x => x.HasFundamentalData)
                .OrderByDescending(x => x.DollarVolume);

            return sortedByDollarVolume
                .Take(100)
                .Select(x => x.Symbol);
        }

        public override IEnumerable<Symbol> SelectFine(QCAlgorithm algorithm, IEnumerable<FineFundamental> fine)
        {
            //1. Sort yields per share
			var sortedByYields = fine.OrderByDescending(x => x.ValuationRatios.EarningYield);

            //2. Take top 10 most profitable stocks -- and bottom 10 least profitable stocks
            IOrderedEnumerable<FineFundamental> sortedByDebt = fine.OrderBy(x => x.ValuationRatios.PERatio1YearHigh);
            //3. Return the symbol objects as a selection of the universe
            var universe = sortedByYields.Take(10).Concat(sortedByDebt.Cast< FineFundamental>().Reverse().Take(10));
			return universe.Select(x => x.Symbol);
        }
    }
}