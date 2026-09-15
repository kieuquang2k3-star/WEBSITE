using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mitech.Web.Controllers.Admin;

[Authorize(Roles = "Admin")]
public abstract class AdminBaseController : Controller
{
}
