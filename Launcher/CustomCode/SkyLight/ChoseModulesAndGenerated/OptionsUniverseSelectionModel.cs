namespace QuantConnect.Algorithm.Framework.Selection {
    using QuantConnect.Algorithm;
    using QuantConnect.Algorithm.Framework.Alphas;
    using QuantConnect.Algorithm.Framework.Execution;
    using QuantConnect.Algorithm.Framework.Portfolio;
    using QuantConnect.Algorithm.Framework.Risk;
    using QuantConnect.Algorithm.Framework.Selection;
    using QuantConnect.Data;
    using QuantConnect.Data.Market;
    using QuantConnect.Data.UniverseSelection;
    using QuantConnect.Orders;
    using QuantConnect.Securities;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class OptionsUniverseSelectionModel : OptionUniverseSelectionModel{
		public OptionsUniverseSelectionModel(Func<DateTime, IEnumerable<Symbol>> futureChainSymbolSelector)
			:base(TimeSpan.FromDays(1), futureChainSymbolSelector){
			}
		public OptionFilterUniverse filter(OptionFilterUniverse filter){
			return filter.Strikes(-2, 2)
				.Expiration(TimeSpan.FromDays(0), TimeSpan.FromDays(180));
		}
	}

}