namespace LaundryApp.Models;

public class ProcessOrderViewModel
{
    public int Id { get; set; }
    public string UserEmail { get; set; } = "";
    public string ServiceType { get; set; } = "";
    public DateTime ScheduledAt { get; set; }
    public string Address { get; set; } = "";
    public string Notes { get; set; } = "";

    public decimal? WashFoldWeightLbs { get; set; }
    public bool UseByRequestRate { get; set; }
    public decimal? WeightedBlanketWeightLbs { get; set; }

    public int ComforterKingQty { get; set; }
    public int ComforterQueenQty { get; set; }
    public int ComforterFullQty { get; set; }
    public int ComforterTwinQty { get; set; }
    public int DuvetCoverQty { get; set; }
    public int BlanketQty { get; set; }

    public int BedspreadQty { get; set; }
    public int CushionSlipCoverQty { get; set; }
    public int ChairSlipCoverQty { get; set; }
    public int SofaSlipCoverQty { get; set; }
    public int PillowShamQty { get; set; }
    public int StandardPillowQty { get; set; }
    public int MattressCoverQty { get; set; }

    public decimal? EstimatedTotal { get; set; }
}
