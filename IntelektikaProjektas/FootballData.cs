using System;
using System.Collections.Generic;
using System.Text;

namespace IntelektikaProjektas
{
    class FootballData
    {
        public string FullTimeResult { get; set; }
        public int HomeTeamRanking { get; set; }
        public int AwayTeamRanking { get; set; }
        public double HomeTeamWinOdds { get; set; }
        public double AwayTeamWinOdds { get; set; }
        public double DrawOdds { get; set; }

        public FootballData(string FullTimeResult, int HomeTeamRanking, int AwayTeamRanking, double HomeTeamWinOdds,
            double AwayTeamWinOdds, double DrawOdds)
        {
            this.FullTimeResult = FullTimeResult;
            this.HomeTeamRanking = HomeTeamRanking;
            this.AwayTeamRanking = AwayTeamRanking;
            this.HomeTeamWinOdds = HomeTeamWinOdds;
            this.AwayTeamWinOdds = AwayTeamWinOdds;
            this.DrawOdds = DrawOdds;
        }
    }
}
