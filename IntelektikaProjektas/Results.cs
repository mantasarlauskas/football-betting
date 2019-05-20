using System;
using System.Collections.Generic;
using System.Text;

namespace IntelektikaProjektas
{
    class Results
    {
        public int HomeTeamWinCount { get; set; }
        public int AwayTeamWinCount { get; set; }
        public int DrawCount { get; set; }
        public double BayesProbability { get; set; }

        public Results()
        {
            HomeTeamWinCount = 0;
            AwayTeamWinCount = 0;
            DrawCount = 0;
            BayesProbability = 0;
        }
    }
}
