/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using NUnit.Framework;
using QuantConnect.Indicators;
using System;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class RateOfChangeTests : CommonIndicatorTests<IndicatorDataPoint>
    {
        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
        {
            return new RateOfChange(50);
        }

        protected override string TestFileName => "spy_with_rocp50.txt";

        protected override string TestColumnName => "Rate of Change % 50";

        protected override Action<IndicatorBase<IndicatorDataPoint>, double> Assertion =>
            (indicator, expected) =>
                Assert.AreEqual(expected, (double) indicator.Current.Value * 100, 1e-3);

        /// <summary>
        /// Test for issue #5491
        /// </summary>
        [Test]
        public void IndicatorValueIsNotZeroWhenReady()
        {
            var indicator = new RateOfChange(1);
            var time = DateTime.Now;

            for (int i = 1; i <= indicator.WarmUpPeriod; i++)
            {
                indicator.Update(time, i);
                time = time.AddSeconds(1);

                Assert.AreEqual(i == indicator.WarmUpPeriod, indicator.IsReady);
            }

            Assert.IsTrue(indicator.Current > 0);
        }
    }
}
