namespace Infrastructure.CSVManager.dto;


public class CampaignDto
{
    public string Number { get; set; }
    public string Title { get; set; }
    public string NomFichier { get; set; }
    public string Ligne { get; set; }
}

public class DetailDto
{
    public string CampaignNumber { get; set; }
    public string Title { get; set; }
    public string Type { get; set; }
    public string Date { get; set; }
    public string Amount { get; set; }
    public string NomFichier { get; set; }
    public string Ligne { get; set; }
}
