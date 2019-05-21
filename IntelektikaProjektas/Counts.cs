using System;
using System.Collections.Generic;
using System.Text;

namespace IntelektikaProjektas
{
    class Counts
    {
        public int lowCount { get; set; }
        public double lowCountProbability { get; set; }
        public int mediumCount { get; set; }
        public double mediumCountProbability { get; set; }
        public int highCount { get; set; }
        public double highCountProbability { get; set; }

        public Counts()
        {
            lowCount = 0;
            mediumCount = 0;
            highCount = 0;
            lowCountProbability = 0;
            mediumCountProbability = 0;
            highCountProbability = 0;
        }
    }
}
