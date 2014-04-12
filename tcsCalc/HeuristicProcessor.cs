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
        protected readonly IDictionary<int, double> _optimalRates;

        public HeuristicProcessor(double coinPresicion, double minimumDeposit, int maxDeposits, IDictionary<int, double> availableRates, double bonus)
            : base(coinPresicion, minimumDeposit, maxDeposits, availableRates, bonus)
        {
            _optimalRates = _availableRates.GroupBy(g => g.Value).ToDictionary(k => k.Min(t => t.Key), v => v.Key);
        }

        public IEnumerable<Lot> BuildInittialLots(double principal, DateTime openDate, int wholePeriod)
        {
            Contract.Requires(principal >= _minimumDeposit);
            Contract.Requires(wholePeriod >= _minDepositTermKey);

            var lot = Lot.Create();

            var amount = DistrubuteAmountOnLadder(lot, principal, wholePeriod, openDate, openDate);

            if (amount > 0)
            {
                throw new Exception("Heuristic problem detected");
            }

            return new[] { lot };
        }

        protected IEnumerable<int> GetProbingPeriods(int wholePeriod, DateTime openDate, DateTime currentDate)
        {
            var minRateKey = _optimalRates.Keys.Min();
            var maxRateKey = _optimalRates.Keys.Where(k => k <= wholePeriod && currentDate.AddMonths(k) <= openDate.AddMonths(wholePeriod)).OrderByDescending(r => r).FirstOrDefault();
            var result = new[]
            {
                maxRateKey,
                _optimalRates.Keys.Where(r => r > minRateKey && r < maxRateKey).OrderBy(r => r).FirstOrDefault(),
                minRateKey
            }.Where(rateKey => rateKey != default(int));

            return result;
        }

        protected double DistrubuteAmountOnLadder(Lot lot, double amount, int wholePeriod, DateTime openDate, DateTime currentDate)
        {
            Contract.Requires(currentDate >= openDate);

            foreach (var nextProbingPeriod in GetProbingPeriods(wholePeriod, openDate, currentDate))
            {
                if
                    (
                        !(
                            // have enough months left till last period
                            currentDate.AddMonths(nextProbingPeriod) <= openDate.AddMonths(wholePeriod) &&
                            // deposits opened at the same moment cannot exceed _maxDeposits
                            (lot.Deposits.Count) < _maxDeposits &&
                            (amount) >= _minimumDeposit
                        )
                    )
                {
                    continue;
                }

                var nextDepositAmount = (amount - _minimumDeposit) >= _minimumDeposit ? _minimumDeposit : amount;

                var nextMinTermClosingDeposit =
                    lot.Deposits.Where(d => d.ClosesOn <= currentDate.AddMonths(_minDepositTermKey))
                        .OrderByDescending(d => d.MonthFlatRate)
                        .FirstOrDefault();

                var nextToNextClosingDeposit = nextMinTermClosingDeposit != null
                    ? lot.Deposits
                        .Where(
                            d =>
                                d.ClosesOn.AddDays(-Deposit.NO_BONUS_DAYS_TO_CLOSING) >
                                nextMinTermClosingDeposit.ClosesOn)
                        .OrderBy(
                            d =>
                                new DateDiff(nextMinTermClosingDeposit.ClosesOn,
                                    d.ClosesOn.AddDays(-Deposit.NO_BONUS_DAYS_TO_CLOSING)).Days)
                        .ThenByDescending(d => d.MonthFlatRate)
                        .FirstOrDefault()
                    : null;

                // Don't open new deposit if we can top up the sooner closing with same or better interest
                if ((nextMinTermClosingDeposit != null && nextToNextClosingDeposit != null
                        && nextMinTermClosingDeposit.Rate >= _optimalRates[nextProbingPeriod]) ||
                    (nextMinTermClosingDeposit != null &&
                     nextMinTermClosingDeposit.Rate >= _optimalRates[nextProbingPeriod]))
                {
                    nextDepositAmount = amount;
                    nextMinTermClosingDeposit.AddAmount(nextDepositAmount, currentDate, _bonus);
                }
                else
                {
                    var anotherBonusDeposit = lot.Deposits
                        .Where(d => d.ClosesOn.AddDays(-Deposit.NO_BONUS_DAYS_TO_CLOSING) <= currentDate.AddMonths(nextProbingPeriod)
                            && _optimalRates[nextProbingPeriod] < d.Rate)
                        .OrderBy(d => d.ClosesOn)
                        .ThenByDescending(d => d.MonthFlatRate)
                        .FirstOrDefault();

                    // Just heuristics
                    if (anotherBonusDeposit != default(Deposit) && nextProbingPeriod > _optimalRates.Keys.Min())
                    {
                        continue;
                    }

                    if (nextProbingPeriod <= _optimalRates.Keys.Min())
                    {
                        nextDepositAmount = amount;
                    }
                    lot.AddDeposit(nextDepositAmount, nextProbingPeriod, _optimalRates[nextProbingPeriod], currentDate, _bonus);
                }

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

            amount = DistrubuteAmountOnLadder(lot, amount, lastPeriod, openDate, currentDate);

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

