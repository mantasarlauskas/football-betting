using System;
using System.Collections.Generic;
using System.Text;

namespace IntelektikaProjektas
{
    class Counts
    {
        public int homeWinCount { get; set; }
        public int drawCount { get; set; }
        public int awayWinCount { get; set; }

        public Counts()
        {
            homeWinCount = 0;
            drawCount = 0;
            awayWinCount = 0;
        }
    }
}
