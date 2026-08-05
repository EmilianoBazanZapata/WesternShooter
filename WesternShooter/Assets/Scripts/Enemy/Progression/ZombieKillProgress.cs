using System;

public class ZombieKillProgress
{
    private int totalKilled;
    private int victoryKillThreshold;
    private bool hasRaisedVictoryThreshold;

    public int TotalKilled => totalKilled;
    public int VictoryKillThreshold => victoryKillThreshold;

    public event Action<int> TotalKilledChanged;
    public event Action<int> VictoryThresholdReached;

    public void Configure(int configuredVictoryKillThreshold)
    {
        victoryKillThreshold = configuredVictoryKillThreshold;
        totalKilled = 0;
        hasRaisedVictoryThreshold = false;
    }

    public void RegisterKill()
    {
        totalKilled++;
        TotalKilledChanged?.Invoke(totalKilled);

        if (hasRaisedVictoryThreshold)
        {
            return;
        }

        if (victoryKillThreshold <= 0 || totalKilled < victoryKillThreshold)
        {
            return;
        }

        hasRaisedVictoryThreshold = true;
        VictoryThresholdReached?.Invoke(totalKilled);
    }
}