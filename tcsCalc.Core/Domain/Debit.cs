using System;

namespace tcsCalc.Core.Domain
{
    [Serializable]
    public class Debit
    {
        public double Amount { get; private set; }
        public double BonusAmount { get; private set; }
        public DateTime DateTime { get; private set; }
        public Deposit Deposit { get; private set; }

        public Debit(Deposit deposit, double amount, DateTime dateTime, double bonus = 0.00)
        {
            Amount = amount;
            DateTime = dateTime;
            BonusAmount = bonus;
            Deposit = deposit;
        }

        public double AmountWithBonus { get { return Amount + BonusAmount; } }
    }
}
