using System;
using System.Collections.Generic;
using System.Linq;
using Itenso.TimePeriod;

namespace tcsCalc
{
    [Serializable]
    class Deposit
    {
        public static int NO_BONUS_DAYS_TO_CLOSE = 85;

        private IList<Debit> _debits; 

        public int MonthTerm { get; protected set; }
        public double Rate { get; protected set; }
        public DateTime Opened { get; protected set; }

        public IList<Debit> Debits { get { return _debits; }} 

        public Deposit(double amount, int monthTerm, double rate, DateTime opened, double flatBonus = 0)
        {
            Opened = opened;
            MonthTerm = monthTerm;
            Rate = rate;
            _debits = new List<Debit>();
            AddAmount(amount, opened, flatBonus);
        }

        public void AddAmount(double amount, DateTime date, double flatBonus = 0)
        {
            _debits.Add(new Debit(this, amount, date, (new DateDiff(date, ClosesOn).Days > NO_BONUS_DAYS_TO_CLOSE) ? amount * (flatBonus / 100) : 0));
        }

        public double GetAmountOn(DateTime date)
        {
            return _debits.Select(d => DepositMath.CompoundInterest(d.AmountWithBonus, MonthFlatRate, 1, new DateDiff(d.DateTime, date).Months)).Sum();
        }

        public double MonthFlatRate
        {
            get { return (Rate / 100) / 12; }
        }

        public DateTime ClosesOn
        {
            get { return Opened.AddMonths(MonthTerm); }
        }
    }
}
