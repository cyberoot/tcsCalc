﻿using System;
using System.Collections.Generic;
using System.Linq;
using tcsCalc.Core.Util;

namespace tcsCalc.Core.Domain
{
    [Serializable]
    public class Lot
    {
        private double _value { get; set; }

        public IList<Deposit> Deposits { get; private set; }

        public IList<Deposit> ClosedDeposits { get; private set; }

        private Lot()
        {
            Deposits = new List<Deposit>();
            ClosedDeposits = new List<Deposit>();
            _value = 0.0;
        }

        public void AddDeposit(Deposit deposit)
        {
            Deposits.Add(deposit);
        }

        public void AddDeposit(double amount, int monthTerm, double rate, DateTime opened, double flatBonus = 0)
        {
            AddDeposit(new Deposit(amount, monthTerm, rate, opened, flatBonus));
        }

        public double WithdrawAvailable(DateTime date)
        {
            double sum = 0.0;

            var oldDeposits = new List<Deposit>();

            foreach (var deposit in Deposits.Where(deposit => date >= deposit.ClosesOn))
            {
                sum += deposit.GetAmountOn(date);
                oldDeposits.Add(deposit);
            }

            Action<Deposit> processClosing = deposit =>
            {
                Deposits.Remove(deposit);
                ClosedDeposits.Add(deposit);
            };

            oldDeposits.ForEach(processClosing);

            return sum;
        }

        public void AddAccumulateValue(double value)
        {
            _value += value;
        }

        public double Value(DateTime date)
        {
            return  Deposits.Select(d => d.GetAmountOn(date)).Sum() + _value;
        }

        public Lot Copy()
        {
            var newLot = this.Clone();

            return newLot;
        }

        public static Lot Create()
        {
            return new Lot();
        }

    }
}
