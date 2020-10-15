"""

refs
# https://www.quantconnect.com/tutorials/strategy-library/volatility-risk-premium-effect
# https://www.quantconnect.com/forum/discussion/2894/the-options-trading-strategy-based-on-macd-indicator/p1
# https://www.quantconnect.com/tutorials/tutorial-series/applied-options
# https://www.quantconnect.com/forum/discussion/5709/optionchain-is-empty/p1
"""

from datetime import timedelta
import numpy as np
import pandas as pd
from scipy import stats
np.random.seed(2020) # comment to make it a real roulette

class OptionRouletteAlgorithm(QCAlgorithm):

    def Initialize(self):
        
        self.SetStartDate(2017, 1, 15)
        self.SetEndDate(2017,2, 15)
        #self.SetStartDate(2015, 1, 1)
        #self.SetEndDate(datetime.now().date() - timedelta(1))
        
        self.SetCash(100000)
        equity = self.AddEquity("SPY", Resolution.Minute)
        option = self.AddOption("SPY", Resolution.Minute)
        self.symbol = equity.Symbol
        option.SetFilter(self.UniverseFunc)
        self.SetBenchmark(equity.Symbol)
        self.slice = None

        # Define the Schedules
        self.Schedule.On(
            self.DateRules.WeekStart(self.symbol),
            self.TimeRules.AfterMarketOpen(self.symbol, 5),
            Action(self.MyLiquidate)
        )
        
        # Define the Schedules
        self.Schedule.On(
            self.DateRules.WeekStart(self.symbol),
            self.TimeRules.AfterMarketOpen(self.symbol, 10),
            Action(self.MyTrade)
        )
        
    def OnData(self,slice):
        self.slice = slice
        if slice.OptionChains.Count > 0:
            pass
            
    def OnAssignmentOrderEvent(self, assignmentEvent):
        self.Log(str(assignmentEvent))
        self.MyLiquidate()
        
    def OnOrderEvent(self, orderEvent):
        self.Log(str(orderEvent)) 

    def UniverseFunc(self, universe):
        price = self.Securities[self.symbol].Price
        return universe.IncludeWeeklys()\
                    .Strikes(-50,50)\
                    .Expiration(timedelta(30), timedelta(50))
        # TODO: read above api.
                    
    def MyLiquidate(self):
        for x in self.Portfolio:
            if x.Value.Invested:
                self.Liquidate(x.Key)
        # redundant?
        if self.Portfolio[self.symbol].Invested:
            self.Liquidate(self.symbol)
                
        self.Log("MyLiquidate")
    
    
    def MyTrade(self):
        slice = self.slice
        
        if slice is None:
            return
        
        self.Log("MyTrade {} {}".format(self.Portfolio.Invested,slice.OptionChains.Count))
        if slice.OptionChains.Count == 0:
            return
        for i in slice.OptionChains:
            chains = i.Value
            
            if not self.Portfolio.Invested:
                self.Log("trading!")
                # divide option chains into call and put options 
                calls = list(filter(lambda x: x.Right == OptionRight.Call, chains))
                puts = list(filter(lambda x: x.Right == OptionRight.Put, chains))
                
                # if lists are empty return
                if not calls or not puts: return
                
                underlying_price = self.Securities[self.symbol].Price
                expiries = [i.Expiry for i in puts]
                
                # determine expiration date nearly one month
                expiry = min(expiries, key=lambda x: abs((x.date()-self.Time.date()).days-40))
                strikes = [i.Strike for i in puts]
                
                # determine at-the-money strike
                strike = min(strikes, key=lambda x: abs(x-underlying_price))
                
                # compute probability
                hist = self.History([self.symbol], 252*5, Resolution.Daily)
                prct_changes = hist.loc[self.symbol]['close'].pct_change(40)
                # 68% = 1sd, 90% = 2sd.
                m2sd,m1sd,p1sd,p2sd = np.nanpercentile(prct_changes,[5,32,68,95])
                
                # roulette logic
                optionStyle = np.random.choice(['short_strangle','short_iron_condor','long_strangle','synthetic_long'],1)[0]
                num = np.random.choice([2,5,10],1)[0]
                
                # long volatility strategies ********************************
                
                # why would you ever?
                if optionStyle == 'synthetic_long':
                    self.atm_put = [i for i in puts if i.Expiry == expiry and i.Strike == strike]
                    self.atm_call = [i for i in calls if i.Expiry == expiry and i.Strike == strike]
                    
                    if self.atm_put and self.atm_call:
                        mylist = [self.atm_put[0],self.atm_call[0]]
                        self.Log('{}'.format([stats.percentileofscore(prct_changes,(x.Strike-underlying_price)/underlying_price) for x in mylist]))
                        
                        self.Sell(self.atm_put[0].Symbol, num)
                        self.Buy(self.atm_call[0].Symbol, num)
                        
                if optionStyle == 'long_strangle':
                    
                    self.atm_put = [i for i in puts if i.Expiry == expiry and i.Strike == strike]
                    self.atm_call = [i for i in calls if i.Expiry == expiry and i.Strike == strike]
                    
                    if self.atm_put and self.atm_call:
                        mylist = [self.atm_put[0],self.atm_call[0]]
                        self.Log('{}'.format([stats.percentileofscore(prct_changes,(x.Strike-underlying_price)/underlying_price) for x in mylist]))
                        
                        self.Buy(self.atm_put[0].Symbol, num)
                        self.Buy(self.atm_call[0].Symbol, num)
                
                # short volatility strategies ********************************
                
                if optionStyle == 'short_iron_condor':
                    
                    otm_call_strike = min(strikes, key = lambda x:abs(x-underlying_price+p2sd*underlying_price))
                    atm_call_strike = min(strikes, key = lambda x:abs(x-underlying_price+p1sd*underlying_price)) # more like near atm
                    atm_put_strike = min(strikes, key = lambda x:abs(x-underlying_price+m1sd*underlying_price))
                    otm_put_strike = min(strikes, key = lambda x:abs(x-underlying_price+m2sd*underlying_price))

                    self.otm_call = [i for i in calls if i.Expiry == expiry and i.Strike == otm_call_strike]
                    self.atm_call = [i for i in calls if i.Expiry == expiry and i.Strike == atm_call_strike]
                    self.atm_put = [i for i in puts if i.Expiry == expiry and i.Strike == atm_put_strike]
                    self.otm_put = [i for i in puts if i.Expiry == expiry and i.Strike == otm_put_strike]
                    
                    if self.atm_call and self.atm_put and self.otm_put and self.otm_call:
    
                        mylist = [self.otm_put[0],self.atm_call[0],self.atm_put[0],self.otm_call[0]]
                        self.Log('{}'.format([stats.percentileofscore(prct_changes,(x.Strike-underlying_price)/underlying_price) for x in mylist]))
                        # TODO: log net profit and potential max loss.
                        
                        # buy otm
                        self.Buy(self.otm_call[0].Symbol, num)
                        self.Buy(self.otm_put[0].Symbol, num)
                        # sell near atm
                        self.Sell(self.atm_call[0].Symbol, num)
                        self.Sell(self.atm_put[0].Symbol, num)
                
                
                if optionStyle == 'short_strangle':
                    
                    otm_call_strike = min(strikes, key = lambda x:abs(x-underlying_price+p2sd*underlying_price))
                    otm_put_strike = min(strikes, key = lambda x:abs(x-underlying_price+m2sd*underlying_price))
                    
                    self.otm_put = [i for i in puts if i.Expiry == expiry and i.Strike == otm_put_strike]
                    self.otm_call = [i for i in calls if i.Expiry == expiry and i.Strike == otm_call_strike]
                    if self.otm_put and self.otm_call:
                        mylist = [self.otm_put[0],self.otm_call[0]]
                        self.Log('{}'.format([stats.percentileofscore(prct_changes,(x.Strike-underlying_price)/underlying_price) for x in mylist]))
                        
                        self.Sell(self.otm_put[0].Symbol, num)
                        self.Sell(self.otm_call[0].Symbol, num)
