import numpy as np
import pandas as pd

def GetUncorrelatedAssets(returns, num_assets):
    # Get correlation
    correlation = returns.corr()
    
    # Find assets with lowest mean correlation, scaled by STD
    selected = []
    for index, row in correlation.iteritems():
        corr_rank = row.abs().mean()/row.abs().std()
        selected.append((index, corr_rank))

    # Sort and take the top num_assets
    selected = sorted(selected, key = lambda x: x[1])[:num_assets]
    
    return selected

# Import custom function
#from GetUncorrelatedAssets import GetUncorrelatedAssets

class ModulatedOptimizedEngine(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2019, 1, 1)  # Set Start Date

        self.SetCash(1000000)  # Set Strategy Cash
        
        self.UniverseSettings.Resolution = Resolution.Minute
        self.AddUniverse(self.CoarseSelectionFunction)

        self.SetBrokerageModel(AlphaStreamsBrokerageModel())

        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())
        
        self.SetExecution(ImmediateExecutionModel())
        
        self.AddEquity('SPY')
        self.SetBenchmark('SPY')
        
        self.Schedule.On(self.DateRules.EveryDay('SPY'), self.TimeRules.AfterMarketOpen("SPY", 5), self.Recalibrate)
        
        self.symbols = []

    def CoarseSelectionFunction(self, coarse):
        sortedByDollarVolume = sorted(coarse, key=lambda x: x.DollarVolume, reverse=True)
        filtered = [ x.Symbol for x in sortedByDollarVolume ][:100]
        
        return filtered

    def Recalibrate(self):
        insights = []
        
        insights = [Insight.Price(symbol, timedelta(5), InsightDirection.Up, 0.03) for symbol in self.symbols]

        self.EmitInsights(insights)
        
    def OnSecuritiesChanged(self, changes):
        symbols = [x.Symbol for x in changes.AddedSecurities]
        
        qb = self
        
        # Copied from research notebook
        #---------------------------------------------------------------------------
        # Fetch history
        history = qb.History(symbols, 150, Resolution.Hour)
        
        # Get hourly returns
        returns = history.unstack(level = 1).close.transpose().pct_change().dropna()
        
        # Get 5 assets with least overall correlation
        selected = GetUncorrelatedAssets(returns, 5)
        #---------------------------------------------------------------------------
        
        # Add to symbol dictionary for use in Recalibrate
        self.symbols = [symbol for symbol, corr_rank in selected]
        
        symbols = [x.Symbol for x in changes.RemovedSecurities]
        insights = [Insight.Price(symbol, timedelta(minutes = 1), InsightDirection.Flat) for symbol in symbols if self.Portfolio[symbol].Invested]
        self.EmitInsights(insights)