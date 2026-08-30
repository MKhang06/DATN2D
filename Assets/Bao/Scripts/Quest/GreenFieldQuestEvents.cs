using System;

public static class GreenFieldQuestEvents
{
    public static event Action<int> CropsPlanted;
    public static event Action<int> CropsHarvested;
    public static event Action<int> FishCaught;
    public static event Action<int> FishSold;

    public static void ReportCropPlanted(int amount = 1)
    {
        if (amount > 0)
            CropsPlanted?.Invoke(amount);
    }

    public static void ReportCropHarvested(int amount = 1)
    {
        if (amount > 0)
            CropsHarvested?.Invoke(amount);
    }

    public static void ReportFishCaught(int amount = 1)
    {
        if (amount > 0)
            FishCaught?.Invoke(amount);
    }

    public static void ReportFishSold(int amount = 1)
    {
        if (amount > 0)
            FishSold?.Invoke(amount);
    }
}
