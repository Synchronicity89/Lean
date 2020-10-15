#!/usr/bin/env python
# coding: utf-8

# ![QuantConnect Logo](https://cdn.quantconnect.com/web/i/logo-small.png)
# # QuantConnect Fitness Function Research

# ## Objective
# 
# QuantConnect _Alpha Streams_ is a market place of alpha from the global QuantConnect Community. It provides individuals unparalleled distribution to institutional capital, giving individuals opportunties to earn revenue from their efforts. 
#  
# The volume of submissions and unclear selection criteria prompted research into filters on alpha submissions by QuantConnect's Team. The goal was to define a _fitness function_ which could score and judge a strategy's performance, and suitability for the Alpha Stream Market. Potential candidate fitness functions were judged by the following:
#  
#      - Independent and Deterministic - functions should not rely on a distribution of community performance.
#      - Positive Percentage - functions should have results ranging from 0 - 1.
#      - Generous Initial Skew - alphas in profit should achieve near 50% score.
#      - Statistically Significant Activity - Algorithm should be the cause of the performance.

# ### Proposing a Fitness Function 
# 
# The fitness function needed to account for strategy volatility, returns, drawdown, and factor in the turnover to ensure the algorithm engagement was statistically significant. We reviewed a few popular metrics and selected the following factors as inputs to the function:
# 
# ##### **Factor 1:** _Sortino Ratio:_ 
# Sortino gives a relative picture of the strategy volatility. It is calculated by taking a portfolio's annualized rate of return and subtracting the risk free rate of return. The result is divided by the portfolio algorithm downside deviation (standard deviation of negative days). Sortino Ratio results range from $ -\infty \text{ to}+\infty $.
# 
# > $\begin{equation} 
# SR = \frac{ R_{P} - r_{f} }{ \sigma_{d} }
# \end{equation} $
#  
# ##### **Factor 2:**  _Returns Over Max Drawdown (RoMaD):_ 
# RoMaD provides a risk adjusted way to factor in the returns and drawdown of the strategy. It is calculated by dividing the Portfolio Annualized Return by the Maximum Drawdown seen during the backtest. This factor ensures large drawdowns are factored into the overall algorthm fitness score. RoMaD results can range from  $ -\infty \text{ to}+\infty $.
# 
# > $\begin{equation} 
# \text{RoMaD}=\frac{ R_{P} }{ DD_{Max} }
# \end{equation}$
# 
# ##### **Factor 3:** _Portfolio Turnover:_
# Portfolio Turnover was selected to be a volume factor to add weight to strategies which are actively trading so that we can be confident the algorithm returns are due to its trading activity. The turn over should be calculated on the trailing 12 months to ensure it is recent activity.
# 
# > $\begin{equation} 
#  Trailing Turn Over = \frac{ \sum P_{vol} }{ NAV_{12mo} }
#  \end{equation}$
# 
# 
# ##### **Scaling Result Range:**
# Unfortunately these factors do not have a defined range so to use these for our fitness function we'll need to scale them to a fixed range. To handle this we'll create a scaler function $ f_{scale}(x) $ which will adjust the values of Sortino and RoMaD into a predictable space. A sigmoidal curve was chosen as it would result in aggresive dampening of extreme values, and constants of 5 and 10 were determined to give a smooth near linear distribution over the most common values of the Sortino Ratio. 
# 
# > $ \begin{equation} 
# f_{scale}(x) =\frac{5x}{\sqrt{10+x^2}}
# \end{equation}
# $
# 
# 

# In[1]:

import pandas as pd
import datetime
import numpy as np
import math
from matplotlib import style
import matplotlib.pyplot as plt
import matplotlib.mlab as mlab

## Code inspired by this blog post: https://programmingforfinance.com/2017/11/monte-carlo-simulations-of-future-stock-prices-in-python/

class MonteCarlo:
    def __init__(self, algo, start, end):
        self.start = start
        self.end = end
        self.equityCurves = pd.DataFrame()
        self.symbols = []

    def get_asset(self, ticker):
        #Dates
        start = self.start 
        end = self.end 
        
        symbol = qb.AddEquity(ticker).Symbol
        prices = qb.History(symbol, (end - start), Resolution.Daily)
        prices = prices.loc[ticker]["close"] 
        returns = prices.pct_change()
        symbols.append(symbol)
        
        self.returns = returns
        self.prices = prices

    def brownian_motion(self, numSimulations, numDays):
        returns = self.returns
        prices = self.prices
 
        startPrice = 100
 
        # Store equity curves in DataFrame 
        equityCurves = pd.DataFrame()
        
        #Create Each Simulation as a Column in df
        for x in range(numSimulations):
            
            #Inputs
            count = 0
            meanRet = returns.mean()
            variance = returns.var()
            
            dailyVol = returns.std()
            dailyDrift = meanRet - (variance/2)
            drift = dailyDrift - 0.5 * dailyVol ** 2
            
            #Append Start Value    
            prices = []
            
            shock = drift + dailyVol * np.random.normal()
            startPrice * math.exp(shock)
            prices.append(startPrice)
            
            for i in range(numDays):
                if count == 251:
                    break
                shock = drift + dailyVol * np.random.normal()
                price = prices[count] * math.exp(shock)
                prices.append(price)
                
                count += 1
            equityCurves[x] = prices
            self.equityCurves = equityCurves
            self.numDays = numDays
            
    def line_graph(self, title):
        prices = self.prices
        numDays = self.numDays
        equityCurves = self.equityCurves
        
        startPrice = prices[-1]
        fig = plt.figure()
        style.use('bmh')
        plt.plot(equityCurves)
        fig.suptitle(title,fontsize=16)
        plt.xlabel('Day')
        plt.ylabel('Price ($USD)')
        plt.grid(True,color='grey')
        #plt.axhline(y=last_price, color='r', linestyle='-')
        plt.axhline(y=100, color='r', linestyle='-')
        plt.show()

# From FFN package: https://github.com/pmorissette/ffn
def to_drawdown_series(prices):
    """
    Calculates the `drawdown <https://www.investopedia.com/terms/d/drawdown.asp>`_ series.
    This returns a series representing a drawdown.
    When the price is at all time highs, the drawdown
    is 0. However, when prices are below high water marks,
    the drawdown series = current / hwm - 1
    The max drawdown can be obtained by simply calling .min()
    on the result (since the drawdown series is negative)
    Method ignores all gaps of NaN's in the price series.
    Args:
        * prices (Series or DataFrame): Series of prices.
    """
    # make a copy so that we don't modify original data
    drawdown = prices.copy()

    # Fill NaN's with previous values
    drawdown = drawdown.fillna(method='ffill')

    # Ignore problems with NaN's in the beginning
    drawdown[np.isnan(drawdown)] = -np.Inf

    # Rolling maximum
    roll_max = np.maximum.accumulate(drawdown)
    drawdown = drawdown / roll_max - 1.
    return drawdown

def line_graph(equityCurves, title):
    fig = plt.figure()
    style.use('bmh')

    #title = "Monte Carlo Simulation: " + str(252) + " Days"
    plt.plot(equityCurves)
    fig.suptitle(title, fontsize=16)
    plt.xlabel('Day')
    plt.ylabel('Price ($USD)')
    plt.grid(True,color='grey')
    #plt.axhline(y=last_price, color='r', linestyle='-')
    plt.axhline(y=100, color='r', linestyle='-')
    plt.show()

# Scale the equity curve performance measures
def f_scale(x):
    return x*5/np.sqrt(10+x*x)

def f_scale_to_range(x, minimum, maximum):
    return (x - minimum)/(maximum-minimum)

# Function factor for the volume coefficient.
def f_volume(v, nav):
    if v/nav > 1:
        return 1
    else:
        return v/nav

def plot_sigmoid():
    x = np.arange(-10., 10., 0.2)
    sig = f_scale(x)
    plt.plot(x,sig)
    plt.xlabel("Input Function Values")
    plt.ylabel("Scaled Output Function Result")
    plt.title("Sigmoidal Scale Factor Distribution")
    plt.show()


# In[ ]:



plot_sigmoid()


# As the sortio approached negative infinity, it would be scaled to -5, and conversely as it approached positive infinity it would be scaled to +5. This way outliers could generate valid fitness function scores. Portfolio Turnover was given a simple linear factor from 0 to 1, after which it was capped to 1. We only intended to penalize algorithms with less than 1.0 ratio, but also we did not want to _incentivise_ high turnover strategies. 
# 
# > $ \begin{equation} 
# f_{vol}(v) = \frac{v}{NAV}  \text{       where   v is 0 to 1, }  \end{equation}  
# \text {             } = \text {  } 1.0 \text {                      where } v > 1
# $ 

# ###### Candidate Fitness Function:
# Finally, putting it all together the following fitness function was proposed to cover the target criteria:
# 
# $
# \Large
# \begin{equation} Fitness \approx f_{vol} \times (  f_{scale}(RoMaD) + f_{scale}(Sortino)  ) \end{equation}
# $
# 
# ---------

# ## Testing Proposed Fitness Function
# 
# We needed to test the robustness and quality of a trading algorithm, and whether the fitness function would be a good indicator of that quality. To do this we defined some mock algorithms equity curves, and generated the fitness function result distribution. 

# #### Define Test Equity Curves

# Ten stocks were manually chosen based on a range of volatility and performance profiles to be seeds for an equity curve generator. From these seed securities we used a Monte Carlo generator (from the cell in apendix) to retrieve the seed security data and calculate key properties. Based on these properties we simulated randomized equity curves.

# In[25]:


# Seed equities manually chosen to cover a variety of volatility profiles and industries:
symbols = ['IWM', 'SPY', 'TLT', 'TSLA','BA','BAC','GS','V','VZ','AAPL']


# To execute this notebook yourself go to the bottom cell and execute it before continuing. 
# 
# #### Initialize QuantBook and Monte Carlo Generator:

# In[26]:


#  Monte Carlo Date Range (Function at bottom of notebook)

from clr import AddReference
AddReference("System")
AddReference("QuantConnect.Common")
AddReference("QuantConnect.Jupyter")
AddReference("QuantConnect.Indicators")
from System import *
from QuantConnect import *
from QuantConnect.Data.Custom import *
from QuantConnect.Data.Market import TradeBar, QuoteBar
from QuantConnect.Jupyter import *
from QuantConnect.Indicators import *
from datetime import *
import matplotlib.pyplot as plt
import pandas as pd

start = datetime(1990, 1, 3)
end = datetime.now()
qb = QuantBook()
simulator = MonteCarlo(qb, start, end)
print(simulator.equityCurves)


# #### Retrieve Time Series and Append Perfect Equity Curve

# We retrieve time-series for ten different securities, estimate the properties for each security and simulate their equity curves using Brownian Motion. We combine the daily returns of the simulated equity curves and store them in the DataFrame object `returns`.

# In[28]:


simulator.get_asset(symbols[0])
print(symbols[0])
print(simulator.prices)
print(simulator.returns)
simulator.brownian_motion(100, 252)
returns=simulator.equityCurves.pct_change()
print(simulator.symbols)
symbol = simulator.symbols[1]
#From roullet options trading example
# compute probability
hist = qb.History([symbol], 252*5, Resolution.Daily)
print(type(hist))
prct_changes = hist.loc[symbol]['close'].pct_change(40)
# prct_changes1259 = hist.loc[symbol]['close'].pct_change(1259)
# prct_changes1 = hist.loc[symbol]['close'].pct_change(0)
print(type(prct_changes))
# 68% = 1sd, 90% = 2sd.
m2sd,m1sd,p1sd,p2sd = np.nanpercentile(prct_changes,[5,32,68,95])

# roulette logic
optionStyle = np.random.choice(['short_strangle','short_iron_condor','long_strangle','synthetic_long'],1)[0]
print(optionStyle)
num = np.random.choice([2,5,10],1)[0]
#end roullet example

for i in range(1,10):
    simulator.get_asset(symbols[i])
    simulator.brownian_motion(100, 252)
    returns = pd.concat([returns, simulator.equityCurves.pct_change()], axis=1)


# For completeness one high return "impossible" alpha was added to the collection with abnormally high performance to act as an outlier.

# In[6]:


# Add a "perfect" equity curve to returns collection
returns[1001] = pd.Series(np.random.normal(0.012, 0.01, 252), index=returns.index)


# In[7]:


# Plot the daily returns as componding equity curves
equityCurves = returns.apply(lambda x: np.exp(np.cumsum(x)))*100
equityCurves.iloc[0,:]=100
line_graph(equityCurves, "Test Equity Curves From Monte Carlo Simulation Over 1 Year")


# ##### Calculate the Key Factors
# 
# With our test assets defined we can calculate the values of the Sortino and RoMaD factors and get a sense of their distribution of values. We can sanity check these results by confirming our "perfect" asset outlier in position 1001.

# In[8]:


# Calculate the asset drawdowns:
maxDrawdownSeries = equityCurves.apply(lambda x: to_drawdown_series(x))
maxDrawdown = maxDrawdownSeries.min()

# Calculate the RoMaD:
rawRoMaD = (returns.sum()/abs(maxDrawdown))

# Scaled average daily return over volatility of negative daily returns (Sortino ratio)
rawSortinoRatio = returns.mean()*252/(returns[returns<0].apply(lambda x: np.std(x))*np.sqrt(252))


# In[9]:


f,(sortinoPlot, romadPlot) = plt.subplots(1, 2, figsize=(15,5))
sortinoPlot.hist(np.hstack(np.array(rawSortinoRatio)), bins='auto')
sortinoPlot.title.set_text("Raw Sortino Ratio Distribution")
romadPlot.hist(np.hstack(np.array(rawRoMaD)), bins='auto')
romadPlot.title.set_text("Raw RoMaD Distribution")
romadPlot.set_xscale('log')
plt.show()


# To convert these to a reasonable and predictable space we apply the sigmoidal scalar function, limiting the range of the results.

# In[23]:


# Scale the results using the sigmoid function:
romad = f_scale(rawRoMaD)
sortino = f_scale(rawSortinoRatio)

f,(sortinoPlot, romadPlot) = plt.subplots(1, 2, figsize=(15,5))
sortinoPlot.hist(np.hstack(np.array(sortino)), bins='auto')
sortinoPlot.title.set_text("Scaled Sortino Ratio Distribution")
romadPlot.hist(np.hstack(np.array(romad)), bins='auto')
romadPlot.title.set_text("Scaled RoMaD Distribution")
plt.show()


# Adding the scaled factors together we get a theoretical distribution of results from -10 to +10. To get this into our desired range of 0 to 1 we apply a final scaling function on the summed values, using the theoretical range of results.
# 
# $
# \begin{equation}
# \Large
# S(x):=\frac{x-\text{min}(x)}{\text{max}(x)-\text{min}(x)}
# \end{equation}
# $

# In[24]:


fitnessRaw = sortino + romad
fitness = f_scale_to_range(fitnessRaw, -10, 10)

# Plot a histogram of the scaled fitness function results
fig, ax = plt.subplots(1,1, figsize=(15,5))
ax.hist(np.hstack(np.array(fitness)), bins='auto')
ax.title.set_text("Scaled Fitness Function Distribution") 
plt.show()

percent50 = sum(1 if x > 0.5 else 0 for x in fitness) / len(fitness)
percent85 = sum(1 if x > 0.85 else 0 for x in fitness) / len(fitness)

print(f"Alphas with a fitness greater than 50%: {percent50:.2%}.\nAlphas with a fitness greater than 85%:  {percent85:.2%}.")


# ---------
# 
# ## Summary
# 
# The proposed fitness function provided a good range of motion for the equity curves tested, while also being a simple single metric for the success of the algorithm.
# 
# Alpha Streams was created for the community and we'd welcome the community feedback into this scoring system as a the metric for Alpha Streams submission criteria. What did we miss? Is there a simpler way we can get a similar fitness metrics? What is the cut off for an acceptable Alpha Stream?

#  

# ### Appendix: MonteCarlo Code

# In[24]:







# In[ ]:




