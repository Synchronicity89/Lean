class ParticleOptimizedComputer(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2019, 3, 3)  # Set Start Date
        self.SetCash(100000)  # Set Strategy Cash
        
        # Universe Selection
        symbols = [Symbol.Create("SPY", SecurityType.Equity, Market.USA)]
        self.AddUniverseSelection(ManualUniverseSelectionModel(symbols))
        
        # Alpha Model
        self.AddAlpha(MacdAlphaModel(12, 26, 9, MovingAverageType.Simple, Resolution.Daily))
        
        # Portfolio Construction
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())
        
        # Execution
        self.SetExecution(ImmediateExecutionModel())
        
        # Risk Management
        self.AddRiskManagement(NullRiskManagementModel())