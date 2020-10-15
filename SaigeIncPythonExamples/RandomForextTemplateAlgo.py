from sklearn.ensemble import RandomForestRegressor
from sklearn.model_selection import train_test_split
import numpy as np
#https://www.quantconnect.com/forum/discussion/6743/from-research-to-production-random-forest-regression/p1
class AlphaFiveUSTreasuries(QCAlgorithm):

    def Initialize(self):

        #1. Required: Five years of backtest history
        self.SetStartDate(2014, 1, 1)
    
        #2. Required: Alpha Streams Models:
        self.SetBrokerageModel(BrokerageName.AlphaStreams)
    
        #3. Required: Significant AUM Capacity
        self.SetCash(1000000)

        #4. Required: Benchmark to SPY
        self.SetBenchmark("SPY")
        
        self.SetPortfolioConstruction(InsightWeightingPortfolioConstructionModel())
        self.SetExecution(ImmediateExecutionModel())
    
        self.assets = ["IEF", "SHY", "TLT", "IEI", "SHV", "TLH", "EDV", "BIL",
                      "SPTL", "TBT", "TMF", "TMV", "TBF", "VGSH", "VGIT",
                      "VGLT", "SCHO", "SCHR", "SPTS", "GOVT"]

        self.symbols = {}
        
        self.portfolioValue = RollingWindow[Decimal](500)
        
        self.SetWarmup(500)
        
        # Add Equity ------------------------------------------------ 
        for i in range(len(self.assets)):
            self.symbols[self.assets[i]] = self.AddEquity(self.assets[i],Resolution.Hour).Symbol 
                
        self.Schedule.On(self.DateRules.Every(DayOfWeek.Monday), self.TimeRules.AfterMarketOpen("IEF", 30), self.EveryDayAfterMarketOpen)

        
    def EveryDayAfterMarketOpen(self):
        if not self.Portfolio.Invested:
            insights = []
            for ticker, symbol in self.symbols.items():
                insights.append( Insight.Price(symbol, timedelta(days=5), InsightDirection.Up, 0.01, None, None, 1/len(self.symbols)) )
            self.EmitInsights(insights)
        else:
            qb = self 
            #==============================
            # Initialize instance of Random Forest Regressor
            regressor = RandomForestRegressor(n_estimators=100, min_samples_split=5, random_state = 1990)
    
            # Fetch history on our universe
            df = qb.History(qb.Securities.Keys, 500, Resolution.Hour)
            
            # Get train/test data
            returns = df.unstack(level=1).close.transpose().pct_change().dropna()
            X = returns
            y = [x for x in qb.portfolioValue][-X.shape[0]:]
            X_train, X_test, y_train, y_test = train_test_split(X, y, test_size = 0.2, random_state = 1990)
            
            # Fit regressor
            regressor.fit(X_train, y_train)
            
            # Get long-only predictions
            weights = regressor.feature_importances_
            symbols = returns.columns[np.where(weights)]
            selected = zip(symbols, weights)
            # ==============================
            
            insights = []
            for symbol, weight in selected:
                insights.append( Insight.Price(symbol, timedelta(days=5), InsightDirection.Up, 0.01, None, None, weight) )
            self.EmitInsights(insights)
        
    def OnData(self, data):
        self.portfolioValue.Add(self.Portfolio.TotalPortfolioValue)
