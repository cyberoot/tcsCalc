using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Itenso.TimePeriod;

namespace tcsCalc
{
    class BruteProcessor : BaseProcessor, ILotProcessor
    {
        public BruteProcessor(double coinPresicion, double minimumDeposit, int maxDeposits, IDictionary<int, double> availableRates, double bonus)
            : base(coinPresicion, minimumDeposit, maxDeposits, availableRates, bonus)
        {
        }

        public IEnumerable<Lot> BuildInittialLots(double principal, DateTime openDate, int wholePeriod)
        {
            var depositCoins = DepositMath.GenerateDeposits(_minimumDeposit, principal, _coinPresicion).ToList();

            var depositCombinations = DepositMath.GetAllPossibleDepositCombinations(principal, depositCoins).ToList();

            var possibleSimultaneousDepositSets = depositCombinations.Where(r => r.Count <= _maxDeposits);

            var possibleTerms = _availableRates.Keys.Where(k => k <= wholePeriod).ToList();

            foreach (var possibleSimultaneousDeposits in possibleSimultaneousDepositSets)
            {
                foreach (var termSet in possibleTerms.Permute(possibleSimultaneousDeposits.Count, true))
                {
                    yield return MakeRound(Lot.Create(), possibleSimultaneousDeposits, termSet.ToList(), _availableRates, openDate, _bonus);
                }
            }
        }

        protected override void ProcessLot(Lot lot, DateTime openDate, DateTime currentDate, int lastPeriod, ref ConcurrentBag<Lot> results)
        {
            var amount = lot.WithdrawAvailable(currentDate);

            if (!(amount > 0))
            {
                FoundLot(lot, ref results);
                return;
            }

            var leftover = amount % _coinPresicion;

            var tempAmount = amount - leftover;

            List<double> depositAmounts = tempAmount > 0 ?
                DepositMath.GenerateDeposits(_minimumDeposit, tempAmount, _coinPresicion).ToList() :
                new List<double>() { leftover };

            depositAmounts.Add(depositAmounts.Min() + leftover);
            depositAmounts.Add(depositAmounts.Max() + leftover);

            depositAmounts.Distinct().ToList().Sort();

            List<List<double>> depositCombinations = DepositMath.GetAllPossibleDepositCombinations(amount, depositAmounts).ToList();

            var refLot = lot;
            var possibleSimultaneousCoinSets = depositCombinations.Where(r => r.Count <= (_maxDeposits - refLot.Deposits.Count));

            var newTerms = _availableRates.Keys.Where(k => k <= (lastPeriod - new DateDiff(openDate, currentDate).Months)).ToList();
            var depositsByRate = lot.Deposits.OrderByDescending(d => d.MonthFlatRate).ToList();
            var maxRateDeposit = depositsByRate.FirstOrDefault();

            if (newTerms.Count <= 0)
            {
                if (maxRateDeposit == null)
                {
                   lot.AddAccumulateValue(amount);
                }
                else
                {
                    maxRateDeposit.AddAmount(amount, currentDate, _bonus);
                }
                FoundLot(lot, ref results);
            }

            foreach (var possibleSimultaneousCoins in possibleSimultaneousCoinSets)
            {
                foreach (var termSet in newTerms.Permute(possibleSimultaneousCoins.Count, true))
                {
                    FoundLot(MakeRound(lot.Copy(), possibleSimultaneousCoins, termSet.ToList(), _availableRates, currentDate, _bonus), ref results);
                }
            }

            int idx = 0;
            foreach (var deposit in depositsByRate)
            {
                var daysToFinalWithdrawal = new DateDiff(currentDate, deposit.ClosesOn).Days;
                if (daysToFinalWithdrawal > Deposit.NO_BONUS_DAYS_TO_CLOSING)
                {
                    var theOtherLot = lot.Copy();
                    theOtherLot.Deposits.OrderByDescending(d => d.MonthFlatRate).ToList()
                        .ElementAt(idx)
                        .AddAmount(amount, currentDate, _bonus);
                    FoundLot(theOtherLot, ref results);
                }
                idx++;
            }
        }
        protected Lot MakeRound
            (
                Lot lot,
                IList<double> coinSet,
                IList<int> termSet,
                IDictionary<int, double> rates,
                DateTime openDate,
                double flatBonus = 0
            )
        {
            if (coinSet.Count == termSet.Count)
            {
                for (int j = 0; j < termSet.Count(); j++)
                {
                    lot.AddDeposit(new Deposit(coinSet[j], termSet[j], rates[termSet[j]], openDate, flatBonus));
                }
            }
            else
            {
                int i = 0;
                int termVal = termSet[i];
                foreach (var coin in coinSet)
                {
                    if (termSet.Count > i)
                    {
                        termVal = termSet[i];
                        i++;
                    }
                    lot.AddDeposit(new Deposit(coin, termVal, rates[termVal], openDate, flatBonus));
                }
            }
            return lot;
        }
    }
}
