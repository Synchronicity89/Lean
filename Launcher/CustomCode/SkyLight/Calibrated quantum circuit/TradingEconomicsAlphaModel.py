from QuantConnect.Data.Custom.TradingEconomics import *
class TradingEconomicsAlphaModel:
    
    def __init__(self, algorithm):
        ## Add the Trading Economics data we want -- this is just a small sample of what is available
        self.interestRate = algorithm.AddData(TradingEconomicsCalendar,TradingEconomics.Calendar.UnitedStates.InterestRate).Symbol
        self.economicOptimism = algorithm.AddData(TradingEconomicsIndicator,TradingEconomics.Indicator.UnitedStates.EconomicOptimismIndex).Symbol

    def Update(self, algorithm, data):
        insights = []
        
        ## Economics Calendar and Indicator data is provided by Trading Economics for 28 countries
        ## since 2013. Trading Economics data is divided into datasets by country and indicator.
        ## The data is relatively sparse with the most frequent sets being updated monthly.
        
        # Check for an announcement regarding the interest rate
        if data.ContainsKey(self.interestRate):
            announcement = data[self.interestRate]
            ## Generate Insights!
        
        # Check for an announcement regarding the economic optimism
        if data.ContainsKey(self.economicOptimism):
            announcement = data[self.economicOptimism]
            ## Generate Insights!
        
        return insights
        
    def OnSecuritiesChanged(self, algorithm, changes):
        pass