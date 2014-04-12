using System;

namespace tcsCalc.Core.Util
{
    public class EnvironmentUtils
    {
        public static int GetMaxParallelism()
        {
            return (int) Math.Ceiling(Environment.ProcessorCount/1.5);
        }
    }
}
