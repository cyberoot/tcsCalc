using System;
using System.Collections.Generic;

namespace tcsCalc.Core.Util
{
    static class DepositMath
    {
        public static IEnumerable<double> GenerateDeposits(double minDeposit, double maxDeposit, double step)
        {
            double currentDepositShift = 0;

            while ((minDeposit + currentDepositShift) <= maxDeposit)
            {
                yield return minDeposit + currentDepositShift;
                currentDepositShift += step;
            }
        }

        public static IEnumerable<List<double>> GetAllPossibleDepositCombinations(
            double remainingTarget,
            IReadOnlyList<double> deposits,
            List<double> currentDeposits = null,
            int currentDepositIndex = 0)
        {
            currentDeposits = currentDeposits ?? new List<double>();

            for (int i = currentDepositIndex; i < deposits.Count; i++)
            {
                double newTarget = remainingTarget - deposits[i];
                var currentCombination = new List<double>(currentDeposits) { deposits[i] };
                if (newTarget < 0)
                {
                    break;
                }
                if (Math.Abs(newTarget) < 0.01)
                {
                    yield return currentCombination;
                    break;
                }
                foreach (var res in GetAllPossibleDepositCombinations(newTarget, deposits, currentCombination, i))
                {
                    yield return res;
                }
            }
        }

        /// <summary>
        /// CompoundInterest.
        /// </summary>
        public static double CompoundInterest(double principal,
            double interestRate,
            int timesPerPeriod,
            double periods)
        {
            // (1 + r/n)
            double body = 1 + (interestRate / timesPerPeriod);

            // nt
            double exponent = timesPerPeriod * periods;

            // P(1 + r/n)^nt
            return principal * Math.Pow(body, exponent);
        }
    }
}
