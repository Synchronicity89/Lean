from math import floor
class MomentumBasedTacticalAllocation(QCAlgorithm):
    
    def Initialize(self):
        
        self.SetStartDate(2012, 8, 1) 
        self.SetEndDate(2019, 8, 1)  
        self.SetCash(3000)  
        
        self.spy = self.AddEquity("SPY", Resolution.Daily)  
        self.bnd = self.AddEquity("BND", Resolution.Daily)  
      
        self.spyMomentum = self.MOMP("SPY", 50, Resolution.Daily) 
        self.bondMomentum = self.MOMP("BND", 50, Resolution.Daily) 
       
        self.SetBenchmark(self.spy.Symbol)  
        self.SetWarmUp(50) 
  
    def OnData(self, data):
        
        if self.IsWarmingUp:
            return
        
        #1. Limit trading to happen once per week
        if self.Time.weekday() != 1:
            return
        
        if self.spyMomentum.Current.Value > self.bondMomentum.Current.Value:
            if self.Securities["SPY"].Close == 0: return
            self.Liquidate(self.bnd.Symbol)
            self.MarketOrder(self.spy.Symbol, floor(self.Portfolio.MarginRemaining/self.Securities["SPY"].Close))
            #2. Otherwise we liquidate our holdings in SPY and allocate 100% to BND
        else:
            if self.Securities["BND"].Close == 0: return
            self.Liquidate(self.spy.Symbol)
            self.MarketOrder(self.bnd.Symbol, floor(self.Portfolio.MarginRemaining/self.Securities["BND"].Close))