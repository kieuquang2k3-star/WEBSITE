using System.ComponentModel.DataAnnotations;

namespace Mitech.Web.Models.ViewModels;

public class ContactViewModel
{
    [Required(ErrorMessage = "企業名は必須です")]
    [MaxLength(400)]
    public string CompanyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "お名前は必須です")]
    [MaxLength(400)]
    public string ContactName { get; set; } = string.Empty;

    [MaxLength(400)]
    public string Furigana { get; set; } = string.Empty;

    [Required(ErrorMessage = "メールアドレスは必須です")]
    [EmailAddress(ErrorMessage = "メールアドレスの形式が正しくありません")]
    [MaxLength(400)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "確認メールアドレスは必須です")]
    [EmailAddress]
    [MaxLength(400)]
    [Compare("Email", ErrorMessage = "メールアドレスが一致しません")]
    public string EmailConfirm { get; set; } = string.Empty;

    [Required(ErrorMessage = "電話番号は必須です")]
    [MaxLength(400)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(400)]
    public string ZipCode { get; set; } = string.Empty;

    [MaxLength(400)]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "ご相談内容は必須です")]
    [MaxLength(2000)]
    public string Message { get; set; } = string.Empty;
}
