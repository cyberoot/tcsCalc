using System;
using System.Collections.Generic;

namespace tcsCalc
{
    interface ILotProcessor
    {
        IEnumerable<Lot> BuildInittialLots(double principal, DateTime openDate, int wholePeriod);
        IList<Lot> ProcessPeriod(IEnumerable<Lot> lotSet, DateTime openDate, DateTime currentDate, int lastPeriod);
    }
}
