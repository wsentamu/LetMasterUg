using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LetMasterWebApp.Core;
public class BaseModel
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string? CreatedBy {  get; set; }
    public DateTime CreatedDate { get; set; }=DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set;}
    public bool IsActive {  get; set; }=true;
}
public class BaseSearch
{
    public int Skip { get; set; } = 0;
    public int Limit { get; set; } = 100;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string OrderBy { get; set; } = "CreatedDate";
    public string Order { get; set; } = "desc";
}
public class Serviceresponse
{
    public bool Success { get; set; }
    public string? ErrorMsg { get; set; }
}