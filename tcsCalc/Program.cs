using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NDesk.Options;
using tcsCalc.Core;
using tcsCalc.Core.Domain;
using tcsCalc.Core.Service;
using tcsCalc.Core.Service.Processor;
using tcsCalc.Core.Util;

namespace tcsCalc
{
    class Program
    {
        private static readonly Dictionary<int, double> _availableRates = new Dictionary<int, double>();

        private const double Bonus = 1.5;
        private const double MinimumDeposit = 30000;
        private static int _maxDeposits = 6;

        private static void InitRates()
        {
            foreach (var term in Enumerable.Range(3, 3))
            {
                _availableRates.Add(term, 4);
            }
            foreach (var term in Enumerable.Range(6, 6))
            {
                _availableRates.Add(term, 6.5);
            }
            foreach (var term in Enumerable.Range(12, 25))
            {
                _availableRates.Add(term, 9.5);
            }
        }
        private static void InitOptimalRates()
        {
            foreach (var term in Enumerable.Range(3, 1))
            {
                _availableRates.Add(term, 4);
            }
            foreach (var term in Enumerable.Range(6, 1))
            {
                _availableRates.Add(term, 6.5);
            }
            foreach (var term in Enumerable.Range(12, 1))
            {
                _availableRates.Add(term, 9.5);
            }
        }

        static void Main(string[] args)
        {
            double principal = 0;
            int maxPeriod = 12;
            double depositPrecision = 15000;
            bool showHelp = false;
            bool bruteForce = false;
            bool useAllRates = false;

            var p = new OptionSet()
            {
                { "p|principal=", "the starting principal (e.g. 100000)", v => principal = double.Parse(v) },
                { "m|months=", "the number of month to evaluate deposit possibilities (default 12)", (int v) => maxPeriod = v },
                { "d|maxdeposits=", "how many deposits can be opened at the same time (default 6)", (int v) => _maxDeposits = v },
                { "b|brute",  "Brute-force strategy instead of heuristics (WARNING!!! May require huge resources of both cpu and memory!)", (v) => bruteForce = v != null },
                { "a|all-rates", "include all possible rate/term combinations, not just optimal (default false (3, 6 and 12 month)", v => useAllRates = v != null },
                { "r|precision=", "how precise to brute-search for deposit combinations (default 15000)", v => depositPrecision = double.Parse(v) },
                { "h|help",  "show this message and exit", (v) => showHelp = v != null },
            };

            try
            {
                p.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("tcsCalc: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `tcsCalc --help' for more information.");
                return;
            }

            if (principal == 0.00)
            {
                showHelp = true;
            }

            if (showHelp)
            {
                ShowHelp(p);
                Console.ReadLine();
                return;
            }

            Console.WriteLine("tcsCalc version {0}", Assembly.GetEntryAssembly().GetName().Version);
            Console.WriteLine(" = Number of cpus gonna use: {0}", EnvironmentUtils.GetMaxParallelism());
            Console.WriteLine(" = Principal: {1}{0} = Deposit term: {2}{0} = Strategy: {3}{0}{0}",
                Environment.NewLine, principal, maxPeriod, bruteForce ? "Brute-force" : "Heuristic"
                );

            if(useAllRates)
            {
                InitRates();
            }
            else
            {
                InitOptimalRates();
            }

            var openDate = DateTime.Now.Round(TimeSpan.FromDays(1));
            var finalDate = openDate.AddMonths(maxPeriod);

            ILotProcessor lotProcessor;

            if(bruteForce)
            {
                lotProcessor = new BruteProcessor(depositPrecision, MinimumDeposit, _maxDeposits, _availableRates, Bonus);
            }
            else
            {
                lotProcessor = new HeuristicProcessor(depositPrecision, MinimumDeposit, _maxDeposits, _availableRates, Bonus);
            }

            var lotService = new LotService(lotProcessor);

            IEnumerable<Lot> lots = lotProcessor.BuildInittialLots(principal, openDate, maxPeriod);
            var bestLot = lotService.GetBestLot(lots.ToList(), openDate, maxPeriod);

            var maxFutureValue = bestLot.Value(finalDate);
            var finalInterest = (maxFutureValue / principal) - 1;

            Console.WriteLine("Max possible future value:{0} = {1:F} ({2:P}) in {3} months ({4:P} annual){0}", Environment.NewLine, maxFutureValue, finalInterest, maxPeriod, (finalInterest / maxPeriod) * 12);

            ShowLotStats(bestLot);

            Console.Write("{0}{0}{0}Press any key to continue...", Environment.NewLine);

            Console.ReadKey();

        }

        private static void ShowLotStats(Lot lot)
        {
            int j = 0;
            foreach (var deposit in lot.ClosedDeposits.OrderBy(d => d.ClosesOn))
            {
                j++;
                Console.WriteLine("= Deposit {0} open from {1:d} till {4:d} for {2} months @ {3:P} annual", j, deposit.Opened, deposit.MonthTerm, deposit.Rate / 100, deposit.ClosesOn);
                foreach (var debit in deposit.Debits.GroupBy(d => d.DateTime)
                    .Select(g => new { DateTime = g.Key, Amount = g.Sum(v => v.Amount) } ).OrderBy(d => d.DateTime))
                {
                    Console.WriteLine("\t{0} {1:F} on {2:d}", "+ top up ", debit.Amount, debit.DateTime);
                }
                Console.WriteLine("\t{0} {1:F} on {2:d}", "- withdraw ", deposit.GetAmountOn(deposit.ClosesOn), deposit.ClosesOn);
            }
        }

        static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: tcsCalc [OPTIONS]+ ");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }

    }
}
