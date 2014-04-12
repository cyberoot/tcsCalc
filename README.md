tcsCalc
=======

Tinkoff Credit Systems Bank deposit bonus exploit calculator.

Usage
-------

```bat
tcsCalc [OPTIONS]+

Options:
  -p, --principal=VALUE      the starting principal (e.g. 100000)
  -m, --months=VALUE         the number of month to evaluate deposit
                               possibilities (default 12)
  -d, --maxdeposits=VALUE    how many deposits can be opened at the same time
                               (default 6)
  -b, --brute                Brute-force strategy instead of heuristics
                               (WARNING!!! May require huge resources of both
                               cpu and memory!)
  -a, --all-rates            include all possible rate/term combinations, not
                               just optimal (default false (3, 6 and 12 month)
  -r, --precision=VALUE      how precise to brute-search for deposit
                               combinations (default 15000)
  -h, --help                 show this message and exit

```

Example output
-------

```bat
tcscalc -p 120000 -m 24

tcsCalc version 1.0.0.0
 = Number of cpus gonna use: 6
 = Principal: 120000
 = Deposit term: 24
 = Strategy: Heuristic


Max possible future value:
 = 151482.74 (26.24 %) in 24 months (13.12 % annual)

= Deposit 1 open from 4/13/2014 till 7/13/2014 for 3 months @ 4.00 % annual
        + top up  60000.00 on 4/13/2014
        - withdraw  61511.03 on 7/13/2014
= Deposit 2 open from 4/13/2014 till 10/13/2014 for 6 months @ 6.50 % annual
        + top up  30000.00 on 4/13/2014
        + top up  31511.03 on 7/13/2014
        - withdraw  63959.38 on 10/13/2014
= Deposit 3 open from 10/13/2014 till 1/13/2015 for 3 months @ 4.00 % annual
        + top up  33959.38 on 10/13/2014
        - withdraw  34814.61 on 1/13/2015
= Deposit 4 open from 4/13/2014 till 4/13/2015 for 12 months @ 9.50 % annual
        + top up  30000.00 on 4/13/2014
        + top up  34814.61 on 1/13/2015
        - withdraw  69654.82 on 4/13/2015
= Deposit 5 open from 7/13/2014 till 7/13/2015 for 12 months @ 9.50 % annual
        + top up  30000.00 on 7/13/2014
        + top up  69654.82 on 4/13/2015
        - withdraw  105864.18 on 7/13/2015
= Deposit 6 open from 10/13/2014 till 10/13/2015 for 12 months @ 9.50 % annual
        + top up  30000.00 on 10/13/2014
        + top up  105864.18 on 7/13/2015
        - withdraw  143496.48 on 10/13/2015
= Deposit 7 open from 10/13/2015 till 1/13/2016 for 3 months @ 4.00 % annual
        + top up  113496.48 on 10/13/2015
        - withdraw  116354.76 on 1/13/2016
= Deposit 8 open from 10/13/2015 till 4/13/2016 for 6 months @ 6.50 % annual
        + top up  30000.00 on 10/13/2015
        + top up  116354.76 on 1/13/2016
        - withdraw  151482.74 on 4/13/2016
```
