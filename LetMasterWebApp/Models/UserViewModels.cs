using LetMasterWebApp.Core;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LetMasterWebApp.Models;
public class UserViewModel
{
    public string? Id { get; set; }
    [Display(Name = "Username")]
    public string? UserName { get; set; }
    [Display(Name = "First Name")]
    public string? FirstName { get; set; }
    [Display(Name = "Organization Name")]
    public string? OtherNames { get; set; }
    [Display(Name = "Surname")]
    public string? LastName { get; set; }
    [Display(Name = "Name")]
    public string? DisplayName { get; set; }
    [Display(Name = "Physical Address")]
    public string? Address { get; set; }
    [Display(Name = "Phone Number")]
    public string? PhoneNumber {  get; set; }
    [Display(Name = "Alternate Number")]
    public string? MobileNumber { get; set; }
    [EmailAddress]
    [Display(Name = "Email Address")]
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public List<string> Roles { get; set; } = new List<string>();
}
public class UserUpdateModel
{
    [Required]
    public string? Id { get; set; }
    [Display(Name = "Is Individual Account")]
    public bool IsIndividual { get; set; } = true;
    [Display(Name = "First Name")]
    public string? FirstNameUpdate { get; set; }

    [Display(Name = "Surname")]
    public string? LastNameUpdate { get; set; }
    [Display(Name = "Organization Name")]
    public string? OtherNamesUpdate { get; set; }
    [Display(Name = "Physical Address")]
    public string? Address { get; set; }
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }
    [Display(Name = "Alternate Number")]
    public string? MobileNumber { get; set; }
    [Display(Name = "Email Address")]
    [EmailAddress]
    public string? Email { get; set; }
    [Display(Name = "Activate Account")]
    public bool IsActive { get; set; } = true;
    //[Required(ErrorMessage ="Select atleast one role")]
    public List<string>? Roles { get; set; }
    public List<SelectListItem>? AvailableRoles { get; set; }= new List<SelectListItem>();
    public string? UpdatedBy { get; set; }
}
public class UserCreateModel
{
    [Display(Name = "Is Individual Account")]
    public bool IsIndividual { get; set; } = true;
    [Display(Name = "First Name")]
    public string? FirstName { get; set; }
    [Display(Name = "Surname")]
    public string? LastName { get; set; }
    [Display(Name = "Organization Name")]
    public string? OtherNames { get; set; }
    [Display(Name = "Name")]
    public string? DisplayName { get; set; }
    [Display(Name = "Physical Address")]
    [Required]
    public string? Address { get; set; }
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }
    [Display(Name = "Alternate Number")]
    public string? MobileNumber { get; set; }
    [Display(Name = "Email Address")]
    [Required]
    [EmailAddress]
    public string Email { get; set; } = default!;
    [Display(Name = "Activate Account")]
    public bool IsActive { get; set; } = true;
    [Required(ErrorMessage = "Select atleast one role")]
    public List<string> Roles { get; set; } = new List<string>();
    public string? CreatedBy { get; set; }
}
public class UserSearchModel: BaseSearch
{
    [Display(Name = "Username")]
    public string? UserName { get; set; }
    [Display(Name = "Name")]
    public string? DisplayName { get; set; }
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }
    [Display(Name = "Alternate Number")]
    public string? MobileNumber { get; set; }
    [Display(Name = "Email Address")]
    [EmailAddress]
    public string? Email { get; set; }
    [Display(Name = "Active Account")]
    public bool IsActive { get; set; } = true;
    public string? Role { get; set; }
}