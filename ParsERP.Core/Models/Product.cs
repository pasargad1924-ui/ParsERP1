namespace ParsERP.Core.Models;

public class Product
{
    public int ProductId { get; set; }
    public int? CategoryId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameFa { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string Unit { get; set; } = "عدد";
    public string ProductType { get; set; } = "Finished"; // RawMaterial, SemiFinished, Finished, Service
    public bool HasSerial { get; set; }
    public bool HasWarranty { get; set; }
    public int WarrantyMonths { get; set; } = 12;
    public double MinStock { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

public class SerialNumber
{
    public int SerialId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SerialNo { get; set; } = string.Empty;
    public string Status { get; set; } = "InStock";
    public DateTime? ProductionDate { get; set; }
    public DateTime? SaleDate { get; set; }
    public int? CustomerId { get; set; }
    public DateTime? WarrantyStart { get; set; }
    public DateTime? WarrantyEnd { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
