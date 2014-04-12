using System;
using System.Collections.Generic;
using tcsCalc.Core.Domain;

namespace tcsCalc.Core.Service.Processor
{
    public interface ILotProcessor
    {
        IEnumerable<Lot> BuildInittialLots(double principal, DateTime openDate, int wholePeriod);
        IList<Lot> ProcessPeriod(IEnumerable<Lot> lotSet, DateTime openDate, DateTime currentDate, int lastPeriod);
    }
}
