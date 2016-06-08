using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iTppc.Core
{
    public class Statistic
    {
        public Statistic()
        {
            Wins = 0;
            Losses = 0;
            MoneyGained = 0;
            TeamPointsGained = 0;
        }

        public void incrementWins()
        {
            Wins++;
        }

        public void incrementLosses()
        {
            Losses++;
        }

        public void addMoneyGained(int amount)
        {
            MoneyGained += amount;
        }

        public void addTeamPointsGained(int amount)
        {
            TeamPointsGained += amount;
        }

        public int Wins { get; private set; }

        public int Losses { get; private set; }

        public int MoneyGained { get; private set; }

        public int TeamPointsGained { get; private set; }
    }
}
