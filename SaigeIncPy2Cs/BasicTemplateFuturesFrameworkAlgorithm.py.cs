
using AddReference = clr.AddReference;

using ConstantAlphaModel = Alphas.ConstantAlphaModel.ConstantAlphaModel;

using FutureUniverseSelectionModel = Selection.FutureUniverseSelectionModel.FutureUniverseSelectionModel;

using date = datetime.date;

using timedelta = datetime.timedelta;

using System.Collections.Generic;

public static class BasicTemplateFuturesFrameworkAlgorithm {
    
    static BasicTemplateFuturesFrameworkAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Algorithm.Framework");
        AddReference("QuantConnect.Common");
    }
    
    public class BasicTemplateFuturesFrameworkAlgorithm
        : QCAlgorithm {
        
        public virtual object Initialize() {
            this.UniverseSettings.Resolution = Resolution.Minute;
            this.SetStartDate(2013, 10, 7);
            this.SetEndDate(2013, 10, 11);
            this.SetCash(100000);
            // set framework models
            this.SetUniverseSelection(new FrontMonthFutureUniverseSelectionModel(this.SelectFutureChainSymbols));
            this.SetAlpha(new ConstantFutureContractAlphaModel(InsightType.Price, InsightDirection.Up, new timedelta(1)));
            this.SetPortfolioConstruction(new SingleSharePortfolioConstructionModel());
            this.SetExecution(ImmediateExecutionModel());
            this.SetRiskManagement(NullRiskManagementModel());
        }
        
        public virtual object SelectFutureChainSymbols(object utcTime) {
            var newYorkTime = Extensions.ConvertFromUtc(utcTime, TimeZones.NewYork);
            var ticker = newYorkTime.date() < new date(2013, 10, 9) ? Futures.Indices.SP500EMini : Futures.Metals.Gold;
            return new List<object> {
                Symbol.Create(ticker, SecurityType.Future, Market.USA)
            };
        }
    }
    
    // Creates futures chain universes that select the front month contract and runs a user
    //     defined futureChainSymbolSelector every day to enable choosing different futures chains
    public class FrontMonthFutureUniverseSelectionModel
        : FutureUniverseSelectionModel {
        
        public FrontMonthFutureUniverseSelectionModel(object select_future_chain_symbols)
            : base(select_future_chain_symbols) {
        }
        
        // Defines the futures chain universe filter
        public virtual object Filter(object filter) {
            return filter.FrontMonth().OnlyApplyFilterAtMarketOpen();
        }
    }
    
    // Implementation of a constant alpha model that only emits insights for future symbols
    public class ConstantFutureContractAlphaModel
        : ConstantAlphaModel {
        
        public ConstantFutureContractAlphaModel(object type, object direction, object period)
            : base(direction, period) {
        }
        
        public virtual object ShouldEmitInsight(object utcTime, object symbol) {
            // only emit alpha for future symbols and not underlying equity symbols
            if (symbol.SecurityType != SecurityType.Future) {
                return false;
            }
            return super().ShouldEmitInsight(utcTime, symbol);
        }
    }
    
    // Portfolio construction model that sets target quantities to 1 for up insights and -1 for down insights
    public class SingleSharePortfolioConstructionModel
        : PortfolioConstructionModel {
        
        public virtual object CreateTargets(object algorithm, object insights) {
            var targets = new List<object>();
            foreach (var insight in insights) {
                targets.append(PortfolioTarget(insight.Symbol, insight.Direction));
            }
            return targets;
        }
    }
}
