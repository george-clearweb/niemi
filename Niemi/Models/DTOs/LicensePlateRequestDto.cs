namespace Niemi.Models.DTOs;

public class LicensePlateItem
{
    public string Licenseplate { get; set; } = string.Empty;
}

public class LicensePlateRequestDto
{
    public List<string> LicensePlates { get; set; } = new List<string>();
}