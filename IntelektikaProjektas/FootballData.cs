using System;
using System.Collections.Generic;
using System.Text;

namespace IntelektikaProjektas
{
    class FootballData
    {
        public string FullTimeResult { get; set; }
        public string HomeTeamRanking { get; set; }
        public string AwayTeamRanking { get; set; }
        public string HomeTeamWinOdds { get; set; }
        public string AwayTeamWinOdds { get; set; }
        public string DrawOdds { get; set; }

        public FootballData(string FullTimeResult, string HomeTeamRanking, string AwayTeamRanking, string HomeTeamWinOdds,
            string AwayTeamWinOdds, string DrawOdds)
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
