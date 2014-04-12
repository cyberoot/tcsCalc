using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using tcsCalc.Core.Domain;
using tcsCalc.Core.Util;

namespace tcsCalc.Core.Service.Processor
{
    public abstract class BaseProcessor
    {
        protected readonly double _coinPresicion;
        protected readonly double _minimumDeposit;
        protected readonly int _maxDeposits;
        protected readonly IDictionary<int, double> _availableRates;
        protected readonly double _bonus;

        protected readonly int _minDepositTermKey;

        protected BaseProcessor(double coinPresicion, double minimumDeposit, int maxDeposits, IDictionary<int, double> availableRates, double bonus)
        {
            _bonus = bonus;
            _availableRates = availableRates;
            _maxDeposits = maxDeposits;
            _minimumDeposit = minimumDeposit;
            _coinPresicion = coinPresicion;
            _minDepositTermKey = _availableRates.Keys.OrderBy(k => k).First();
        }

        protected virtual void FoundLot(Lot lot, ref ConcurrentBag<Lot> results)
        {
            results.Add(lot);
        }

        protected abstract void ProcessLot(Lot lot, DateTime openDate, DateTime currentDate, int lastPeriod, ref ConcurrentBag<Lot> results);

        public virtual IList<Lot> ProcessPeriod(IEnumerable<Lot> lotSet, DateTime openDate, DateTime currentDate, int lastPeriod)
        {
            var result = new ConcurrentBag<Lot>();

            Parallel.ForEach(lotSet, GetParallelOptions(), lot => ProcessLot(lot, openDate, currentDate, lastPeriod, ref result));

            return result.ToList();
        }

        protected internal ParallelOptions GetParallelOptions()
        {
            return new ParallelOptions() {MaxDegreeOfParallelism = EnvironmentUtils.GetMaxParallelism()};
        }
    }
}
