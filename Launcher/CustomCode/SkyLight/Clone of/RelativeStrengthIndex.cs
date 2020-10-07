using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using QuantConnect.Securities;
using QuantConnect.Data.Market;

namespace QuantConnect {

    /*
    *   Relative Strength Index Indicator:
    *
    *                    100  
    *   RSI = 100 -  ------------
    *                   1 + RS
    *
    *   Where RS = Avg of X Period Close Up / Absolute(Avg) X of Period Close Down.
    *   
    */
    public class CloneRelativeStrengthIndex
    {
        
        //Public Access to the RSI Output
        public decimal RSI {
            get {
                return (100 - (100 / (1 + _rs)));
            }
        }
        
        //Public Access to Know if RSI Indicator Ready
        public bool Ready {
            get {
                return (_upward.Count >= _period) && (_downward.Count >= _period);
            }
        }
        
        //Private Class Variables:
        private decimal _rs = 0;
        private bool _ema = false;
        private decimal _period = 14;
        private decimal _joinBars = 1;
        private Candle _superCandle = new Candle();
        private Candle _previousCandle = new Candle();
        private FixedSizedQueue<decimal> _downward = new FixedSizedQueue<decimal>(0);
        private FixedSizedQueue<decimal> _upward = new FixedSizedQueue<decimal>(0);
        private decimal _upwardSum = 0, _avgUpward = 0;
        private decimal _downwardSum = 0, _avgDownward = 0;
        
        //Initialize the RSI with 'period' candles
        public CloneRelativeStrengthIndex(int period, int joinBars = 1, bool useEMA = false) {
            
            //Range check variables:
            if (period < 2) period = 2;
            
            //Class settings:
            _period = (decimal)period;  // How many samples is the RSI?
            _ema = useEMA;              // Use the EMA average for RSI
            _joinBars = joinBars;       // Join multiple tradebars together
            
            //Remember the upward and downward movements in a FIFO queue:
            _upward = new FixedSizedQueue<decimal>(period);
            _downward = new FixedSizedQueue<decimal>(period);
            
            //Online implementation of SMA - needs moving sum of all components:
            _upwardSum = 0; _downwardSum = 0;
        }
        
        //Add a new sample to build the RSI Indicator:
        public void AddSample(TradeBar bar) { 
            
            //Build a multibar candle, until join reached return.
            _superCandle.Update(bar);
            if (_superCandle.Samples < _joinBars) return;
            
            //Initialize the first loop.
            if (_previousCandle.Samples == 0) {
                _previousCandle = _superCandle;
                _superCandle = new Candle();
                return;
            }
            
            //Get the difference between this bar and previous bar:
            decimal difference = _superCandle.Close - _previousCandle.Close;
            
            //Update the Moving Average Calculations:
            if (difference >= 0) {
                if (_ema) {
                    _avgUpward = UpdateDirectionalEMA(ref _upward, difference);
                    _avgDownward = UpdateDirectionalEMA(ref _downward, 0);
                } else {
                    _avgUpward = UpdateDirectionalSMA(ref _upward, ref _upwardSum, difference);
                    _avgDownward = UpdateDirectionalSMA(ref _downward, ref _downwardSum, 0);
                }
            }
            if (difference <= 0) {
                difference = Math.Abs(difference);
                if (_ema) {
                    _avgUpward = UpdateDirectionalEMA(ref _upward, 0);
                    _avgDownward = UpdateDirectionalEMA(ref _downward, difference);
                } else {
                    _avgUpward = UpdateDirectionalSMA(ref _upward, ref _upwardSum, 0);
                    _avgDownward = UpdateDirectionalSMA(ref _downward, ref _downwardSum, difference);
                }
            }
            
            //Refresh RS Factor:
            //RS Index Automatically Updated in the Public Property Above:
            if (_avgDownward != 0) {
                _rs = _avgUpward / _avgDownward;
            } else {
                _rs = Decimal.MaxValue - 1;
            }
            
            //Reset for next loop:
            _previousCandle = _superCandle;
            _superCandle = new Candle();
        }
        
        
        // Update the moving average and fixed length queue in a generic fashion to work for up and downward movement.
        // Return the average.
        private decimal UpdateDirectionalSMA(ref FixedSizedQueue<decimal> queue, ref decimal sum, decimal sample) {
            
            //Increment Sum
            sum += sample;
            
            //If we've shuffled off queue, remove from sum:
            if(queue.Enqueue(sample)) {
                sum -= queue.LastDequeued;
            }
            
            //When less than period samples, only divide by the number of samples.
            if (queue.Count < _period) {
                return (sum / (decimal)queue.Count);
            } else {
                return (sum / _period);
            }
        } 
        
        
        // Update the moving average and fixed length queue in a generic fashion to work for up and downward movement.
        // Return the average.
        private decimal UpdateDirectionalEMA(ref FixedSizedQueue<decimal> queue, decimal sample) {
            queue.Enqueue(sample);
            if (queue.Count == 1) {
                return sample;
            } else {
                return (1m / _period) * sample  +  ((_period - 1m) / _period) * queue.LastEnqueued; 
            }
        }
        
        
        
        //Fixed length queue that dumps things off when no more space in queue.
        private class FixedSizedQueue<T> : ConcurrentQueue<T> {
            public int Size { get; private set; }
            public T LastDequeued { get; private set; }
            public T LastEnqueued {get; private set;}
            public bool Dequeued { get; private set; }
            public FixedSizedQueue(int size) { Size = size; }
            public new bool Enqueue(T obj) {
                base.Enqueue(obj);
                LastEnqueued = obj;
                Dequeued = false;
                lock (this) {
                    if (base.Count > Size) {
                        T outObj;
                        Dequeued = base.TryDequeue(out outObj);
                        LastDequeued = outObj;
                    }
                }
                return Dequeued;
            }
        }
        
        /// <summary>
        /// Simple online "super-tradebar" generator for making an OHLC from multiple bars.
        /// </summary>
        public class Candle {
            
            public decimal Open = 0;
            public decimal High = Decimal.MinValue;
            public decimal Low = Decimal.MaxValue;
            public decimal Close = 0;
            public int Samples = 0;
            
            public void Update(TradeBar bar) {
                if (Open == 0) Open = bar.Open;
                if (High < bar.High) High = bar.High;
                if (Low > bar.Low) Low = bar.Low;
                Close = bar.Close;
                Samples++;
            }
        }
        
    }
}