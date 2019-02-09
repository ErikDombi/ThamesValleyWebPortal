using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace TVWB.Controllers
{
    public class LoginController : Controller
    {
        public bool CorrectLogin(string username, string password)
        {
            WebRequest webRequest = WebRequest.Create($"https://schoolapps2.tvdsb.ca/students/student_login/lgn.aspx?__EVENTTARGET&__EVENTARGUMENT&__VIEWSTATE=%2FwEPDwULLTE2MDk1ODI3MTFkZMUq3L2kXLCgWE%2BxPNKGiR2aDkz5&__VIEWSTATEGENERATOR=00958D10&__EVENTVALIDATION=%2FwEWBALO%2BPagDALT8dy8BQKd%2B7qdDgLCi9reA9VqLcMs82KsM9lnbdFM5U4r7vSJ&txtUserID={username}&txtPwd={password}&btnSubmit=Login");
            HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse();

            var encoding = ASCIIEncoding.ASCII;
            string responseText;
            using (var reader = new System.IO.StreamReader(response.GetResponseStream(), encoding))
            {
                responseText = reader.ReadToEnd();
            }
            return !responseText.Contains("Login Failed");
        }

        public IActionResult Login([FromHeader]string Username, [FromHeader]string Password)
        {
            if (CorrectLogin(Username, Password))
                return Ok();
            else
                return Unauthorized();
        }

        public IActionResult Logout()
        {
            Response.Cookies.Delete("Username");
            Response.Cookies.Delete("Password");
            return Redirect("/");
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
