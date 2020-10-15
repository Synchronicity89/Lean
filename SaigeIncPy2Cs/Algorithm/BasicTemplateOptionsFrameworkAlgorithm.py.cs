
using AddReference = clr.AddReference;

using ConstantAlphaModel = Alphas.ConstantAlphaModel.ConstantAlphaModel;

using OptionUniverseSelectionModel = Selection.OptionUniverseSelectionModel.OptionUniverseSelectionModel;

using ImmediateExecutionModel = Execution.ImmediateExecutionModel.ImmediateExecutionModel;

using NullRiskManagementModel = Risk.NullRiskManagementModel.NullRiskManagementModel;

using date = datetime.date;

using timedelta = datetime.timedelta;

using System.Collections.Generic;

public static class BasicTemplateOptionsFrameworkAlgorithm {
    
    static BasicTemplateOptionsFrameworkAlgorithm() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Algorithm.Framework");
        AddReference("QuantConnect.Common");
    }
    
    public class BasicTemplateOptionsFrameworkAlgorithm
        : QCAlgorithm {
        
        public virtual object Initialize() {
            this.UniverseSettings.Resolution = Resolution.Minute;
            this.SetStartDate(2014, 6, 5);
            this.SetEndDate(2014, 6, 6);
            this.SetCash(100000);
            // set framework models
            this.SetUniverseSelection(new EarliestExpiringWeeklyAtTheMoneyPutOptionUniverseSelectionModel(this.SelectOptionChainSymbols));
            this.SetAlpha(new ConstantOptionContractAlphaModel(InsightType.Price, InsightDirection.Up, new timedelta(hours: 0.5)));
            this.SetPortfolioConstruction(new SingleSharePortfolioConstructionModel());
            this.SetExecution(ImmediateExecutionModel());
            this.SetRiskManagement(NullRiskManagementModel());
        }
        
        public virtual object SelectOptionChainSymbols(object utcTime) {
            var newYorkTime = Extensions.ConvertFromUtc(utcTime, TimeZones.NewYork);
            var ticker = newYorkTime.date() < new date(2014, 6, 6) ? "TWX" : "AAPL";
            return new List<object> {
                Symbol.Create(ticker, SecurityType.Option, Market.USA, "?{ticker}")
            };
        }
    }
    
    // Creates option chain universes that select only the earliest expiry ATM weekly put contract
    //     and runs a user defined optionChainSymbolSelector every day to enable choosing different option chains
    public class EarliestExpiringWeeklyAtTheMoneyPutOptionUniverseSelectionModel
        : OptionUniverseSelectionModel {
        
        public EarliestExpiringWeeklyAtTheMoneyPutOptionUniverseSelectionModel(object select_option_chain_symbols)
            : base(select_option_chain_symbols) {
        }
        
        // Defines the option chain universe filter
        public virtual object Filter(object filter) {
            return filter.Strikes(+1, +1).Expiration(new timedelta(0), new timedelta(7)).WeeklysOnly().PutsOnly().OnlyApplyFilterAtMarketOpen();
        }
    }
    
    // Implementation of a constant alpha model that only emits insights for option symbols
    public class ConstantOptionContractAlphaModel
        : ConstantAlphaModel {
        
        public ConstantOptionContractAlphaModel(object type, object direction, object period)
            : base(direction, period) {
        }
        
        public virtual object ShouldEmitInsight(object utcTime, object symbol) {
            // only emit alpha for option symbols and not underlying equity symbols
            if (symbol.SecurityType != SecurityType.Option) {
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
