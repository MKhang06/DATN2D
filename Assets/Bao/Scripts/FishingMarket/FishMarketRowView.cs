using UnityEngine;

public readonly struct FishMarketRowView
{
    public InventoryItemData Item { get; }
    public Sprite Icon { get; }
    public string FishName { get; }

    public float HistoryPrice3 { get; }
    public float HistoryPrice2 { get; }
    public float HistoryPrice1 { get; }
    public float CurrentPrice { get; }

    public int OwnedAmount { get; }
    public int TotalPayout { get; }

    public bool CanSell =>
        Item != null &&
        OwnedAmount > 0 &&
        TotalPayout > 0;

    public FishMarketRowView(
        InventoryItemData item,
        float historyPrice3,
        float historyPrice2,
        float historyPrice1,
        float currentPrice,
        int ownedAmount)
    {
        Item = item;
        Icon = item != null ? item.Icon : null;

        FishName =
            item != null &&
            !string.IsNullOrWhiteSpace(
                item.DisplayName)
                ? item.DisplayName
                : "Cá";

        HistoryPrice3 =
            Mathf.Max(0f, historyPrice3);

        HistoryPrice2 =
            Mathf.Max(0f, historyPrice2);

        HistoryPrice1 =
            Mathf.Max(0f, historyPrice1);

        CurrentPrice =
            Mathf.Max(0f, currentPrice);

        OwnedAmount =
            Mathf.Max(0, ownedAmount);

        double rawPayout =
            CurrentPrice *
            (double)OwnedAmount;

        if (double.IsNaN(rawPayout) ||
            double.IsInfinity(rawPayout) ||
            rawPayout <= 0d ||
            rawPayout > int.MaxValue)
        {
            TotalPayout = 0;
        }
        else
        {
            TotalPayout = (int)System.Math.Round(
                rawPayout,
                System.MidpointRounding.AwayFromZero
            );
        }
    }
}
