
//using np = numpy;

//using pd = pandas;

//using System.Collections.Generic;

//using System.Linq;

//public static class GetUncorrelatedAssets {
    
//    public static object GetUncorrelatedAssets(object returns, object num_assets) {
//        // Get correlation
//        var correlation = returns.corr();
//        // Find assets with lowest mean correlation, scaled by STD
//        var selected = new List<object>();
//        foreach (var _tup_1 in correlation) {
//            var index = _tup_1.Item1;
//            var row = _tup_1.Item2;
//            var corr_rank = row.abs().mean() / row.abs().std();
//            selected.append(Tuple.Create(index, corr_rank));
//        }
//        // Sort and take the top num_assets
//        selected = selected.OrderBy(x => x[1]).ToList()[::num_assets];
//        return selected;
//    }
    
//    public class ModulatedOptimizedEngine
//        : QCAlgorithm {
        
//        public List<object> symbols;
        
//        public virtual object Initialize() {
//            this.SetStartDate(2019, 1, 1);
//            this.SetCash(1000000);
//            this.UniverseSettings.Resolution = Resolution.Minute;
//            this.AddUniverse(this.CoarseSelectionFunction);
//            this.SetBrokerageModel(AlphaStreamsBrokerageModel());
//            this.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel());
//            this.SetExecution(ImmediateExecutionModel());
//            this.AddEquity("SPY");
//            this.SetBenchmark("SPY");
//            this.Schedule.On(this.DateRules.EveryDay("SPY"), this.TimeRules.AfterMarketOpen("SPY", 5), this.Recalibrate);
//            this.symbols = new List<object>();
//        }
        
//        public virtual object CoarseSelectionFunction(object coarse) {
//            var sortedByDollarVolume = coarse.OrderByDescending(x => x.DollarVolume).ToList();
//            var filtered = (from x in sortedByDollarVolume
//                select x.Symbol).ToList()[::100];
//            return filtered;
//        }
        
//        public virtual object Recalibrate() {
//            var insights = new List<object>();
//            insights = (from symbol in this.symbols
//                select Insight.Price(symbol, timedelta(5), InsightDirection.Up, 0.03)).ToList();
//            this.EmitInsights(insights);
//        }
        
//        public virtual object OnSecuritiesChanged(object changes) {
//            var symbols = (from x in changes.AddedSecurities
//                select x.Symbol).ToList();
//            var qb = this;
//            // Copied from research notebook
//            //---------------------------------------------------------------------------
//            // Fetch history
//            var history = qb.History(symbols, 150, Resolution.Hour);
//            // Get hourly returns
//            var returns = history.unstack(level: 1).close.transpose().pct_change().dropna();
//            // Get 5 assets with least overall correlation
//            var selected = GetUncorrelatedAssets(returns, 5);
//            //---------------------------------------------------------------------------
//            // Add to symbol dictionary for use in Recalibrate
//            this.symbols = (from _tup_1 in selected.Chop((symbol,corr_rank) => Tuple.Create(symbol, corr_rank))
//                let symbol = _tup_1.Item1
//                let corr_rank = _tup_1.Item2
//                select symbol).ToList();
//            symbols = (from x in changes.RemovedSecurities
//                select x.Symbol).ToList();
//            var insights = (from symbol in symbols
//                where this.Portfolio[symbol].Invested
//                select Insight.Price(symbol, timedelta(minutes: 1), InsightDirection.Flat)).ToList();
//            this.EmitInsights(insights);
//        }
//    }
//}
