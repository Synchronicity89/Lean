from Alphas.MacdAlphaModel import MacdAlphaModel
from Execution.StandardDeviationExecutionModel import StandardDeviationExecutionModel
from Selection.QC500UniverseSelectionModel import QC500UniverseSelectionModel

class CalibratedUncoupledShield(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2020, 2, 19)  # Set Start Date
        self.SetCash(100000)  # Set Strategy Cash
        # self.AddEquity("SPY", Resolution.Minute)
        self.AddAlpha(MacdAlphaModel(12, 26, 9, MovingAverageType.Simple, Resolution.Daily))

        self.SetExecution(ImmediateExecutionModel(60, 2, Resolution.Minute))

        self.SetPortfolioConstruction(InsightWeightingPortfolioConstructionModel())

    
        self.SetRiskManagement(MaximumDrawdownPercentPortfolio(0.03))

        self.SetUniverseSelection(QC500UniverseSelectionModel())


    # def OnData(self, data):
    #     '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
    #         Arguments:
    #             data: Slice object keyed by symbol containing the stock data
    #     '''

        # if not self.Portfolio.Invested:
        #    self.SetHoldings("SPY", 1)