using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace DomesticViolenceWebApp.Controllers
{
    [Route("home")]
    public class HomeController : Controller
    {
        [HttpGet]
        public ActionResult Index()
        {
            return Redirect("http://domesticviolence.azurewebsites.net/");
        }
    }
}
