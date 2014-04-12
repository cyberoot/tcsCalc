using System;
using System.Collections.Generic;
using System.Linq;
using tcsCalc.Core.Domain;
using tcsCalc.Core.Service.Processor;

namespace tcsCalc.Core.Service
{
    public class LotService
    {
        private readonly ILotProcessor _lotProcessor;

        public LotService(ILotProcessor lotProcessor)
        {
            _lotProcessor = lotProcessor;
        }

        public IEnumerable<Lot> Run(IList<Lot> lots, DateTime openDate, int lastPeriod)
        {
            IEnumerable<Lot> nextPeriodLots = lots ;

            for (int i = 1; i <= (lastPeriod); i++)
            {
                nextPeriodLots = _lotProcessor.ProcessPeriod(nextPeriodLots, openDate, openDate.AddMonths(i), lastPeriod);
            }
            return nextPeriodLots;
        }

        public Lot GetBestLot(IList<Lot> lots, DateTime openDate, int lastPeriod)
        {
            var finalDate = openDate.AddMonths(lastPeriod);

            var results = Run(lots, openDate, lastPeriod)
                .OrderByDescending(x => x.Value(finalDate))
                .ToList();

            var maxLot = results.First();
            var maxFutureValue = maxLot.Value(finalDate);

            return results.Where(r => Math.Abs(r.Value(finalDate) - maxFutureValue) < 0.01).OrderBy(r => r.ClosedDeposits.Count).First();
        }

    }
}
