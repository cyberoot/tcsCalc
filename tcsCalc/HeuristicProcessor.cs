using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Itenso.TimePeriod;

namespace tcsCalc
{
    class HeuristicProcessor : BaseProcessor, ILotProcessor
    {
        public HeuristicProcessor(double coinPresicion, double minimumDeposit, int maxDeposits, IDictionary<int, double> availableRates, double bonus)
            : base(coinPresicion, minimumDeposit, maxDeposits, availableRates, bonus)
        {
        }

        public IEnumerable<Lot> BuildInittialLots(double principal, DateTime openDate, int wholePeriod)
        {
            Contract.Requires(principal >= _minimumDeposit);
            Contract.Requires(wholePeriod >= _minDepositTermKey);

            var lot = Lot.Create();

            var amount = principal;

            var nextDepositAmount = (amount - _minimumDeposit) >= _minimumDeposit ? _minimumDeposit : amount;

            var rateKey = wholePeriod;

            if (!_availableRates.ContainsKey(rateKey))
            {
                rateKey = _availableRates.Keys.Single(p => p <= wholePeriod);
            }

            lot.AddDeposit(new Deposit(nextDepositAmount, wholePeriod, _availableRates[rateKey], openDate, _bonus));

            amount -= nextDepositAmount;

            amount = DistrubutePrincipalOnLadder(lot, amount, _minDepositTermKey * 2, wholePeriod, openDate, openDate, 1);
            
            if(amount > 0)
            {
                lot.AddDeposit(new Deposit(amount, _minDepositTermKey, _availableRates[_minDepositTermKey], openDate, _bonus));
            }

            return new[] {lot};
        }

        protected double DistrubutePrincipalOnLadder(Lot lot, double amount, int probingPeriod, int wholePeriod, DateTime openDate, DateTime currentDate, int needExtraDeposits = 0)
        {
            while
                (
                currentDate.AddMonths(probingPeriod) <= openDate.AddMonths(wholePeriod) &&
                (int) (amount/_minimumDeposit) > needExtraDeposits && // can add one more final deposit
                (lot.Deposits.Count + needExtraDeposits) < _maxDeposits &&
                // deposits opened at the same moment cannot exceed _maxDeposits
                (amount - _minimumDeposit) >= _minimumDeposit*needExtraDeposits &&
                // after another round we will have enough funds for all extra deposits
                (amount) >= _minimumDeposit &&
                (probingPeriod) < wholePeriod // have enough deposit periods available for another _minDepositTermKey
                )
            {
                if (!_availableRates.ContainsKey(probingPeriod))
                {
                    probingPeriod = _availableRates.Keys.Single(p => p >= probingPeriod && p <= wholePeriod);
                }

                var nextDepositAmount = (amount - _minimumDeposit) >= _minimumDeposit ? _minimumDeposit : amount;

                var nextMinTermClosingDeposit = lot.Deposits.Where(d => d.ClosesOn <= currentDate.AddMonths(_minDepositTermKey)).OrderByDescending(d => d.MonthFlatRate).FirstOrDefault();

                var nextToNextClosingDeposit = nextMinTermClosingDeposit != null ?
                        lot.Deposits
                        .Where(
                            d =>
                                d.ClosesOn.AddDays(-Deposit.NO_BONUS_DAYS_TO_CLOSE) >
                                nextMinTermClosingDeposit.ClosesOn)
                        .OrderBy(d => new DateDiff(nextMinTermClosingDeposit.ClosesOn, d.ClosesOn.AddDays(-Deposit.NO_BONUS_DAYS_TO_CLOSE)).Days)
                        .ThenByDescending(d => d.MonthFlatRate)
                        .FirstOrDefault()
                        : null;

                if ((nextMinTermClosingDeposit != null && nextToNextClosingDeposit != null) ||
                    (nextMinTermClosingDeposit != null && nextMinTermClosingDeposit.Rate > _availableRates[probingPeriod]))
                {
                        nextMinTermClosingDeposit.AddAmount(nextDepositAmount, currentDate, _bonus);
                }
                else
                {
                    lot.AddDeposit(new Deposit(nextDepositAmount, probingPeriod, _availableRates[probingPeriod], currentDate, _bonus));
                }

                probingPeriod += _minDepositTermKey;
                amount -= nextDepositAmount;
            }
            return amount;
        }

        protected override void ProcessLot(Lot lot, DateTime openDate, DateTime currentDate, int lastPeriod, ref ConcurrentBag<Lot> results)
        {
            Contract.Requires(currentDate > openDate);

            var amount = lot.WithdrawAvailable(currentDate);

            if (amount <= 0)
            {
                FoundLot(lot, ref results);
                return;
            }

            amount = DistrubutePrincipalOnLadder(lot, amount, _minDepositTermKey, lastPeriod, openDate, currentDate);

            if (lot.Deposits.Count > 0 && (amount > 0))
            {
                lot.Deposits.OrderBy(d => d.ClosesOn).First().AddAmount(amount, currentDate, _bonus);
            }
            else
            {
                lot.AddAccumulateValue(amount);
            }

            FoundLot(lot, ref results);
        }
    }
}
