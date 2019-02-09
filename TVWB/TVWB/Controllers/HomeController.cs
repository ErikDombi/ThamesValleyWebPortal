using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TVWB.Models;
using System.Net.Http;
using HtmlAgilityPack;
using System.Net;
using System.IO;
using System.Text;

namespace TVWB.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        
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

        public async Task<string> GetLocation(string url)
        {
            HttpClientHandler httpClientHandler = new HttpClientHandler();
            httpClientHandler.AllowAutoRedirect = false;
            using (HttpClient client = new HttpClient(httpClientHandler))
            {
                var response = await client.GetAsync(url);
                return response.Headers.GetValues("Location").FirstOrDefault();
            }
        }

        private async Task<List<Cookie>> GetCookies(string url)
        {
            var cookieContainer = new CookieContainer();
            var uri = new Uri(url);
            using (var httpClientHandler = new HttpClientHandler
            {
                CookieContainer = cookieContainer
            })
            {
                using (var httpClient = new HttpClient(httpClientHandler))
                {
                    await httpClient.GetAsync(uri);
                    return cookieContainer.GetCookies(uri).Cast<Cookie>().ToList();
                }
            }
        }

        public async Task<IActionResult> Timetable()
        {
            string user = Request.Cookies["Username"];
            string pass = Request.Cookies["Password"];
            if (string.IsNullOrEmpty(user))
                return Redirect("/Login/");
            var htmlDoc = new HtmlDocument();
            var uri = new Uri($"https://schoolapps2.tvdsb.ca/students/student_login/lgn.aspx?__EVENTTARGET&__EVENTARGUMENT&__VIEWSTATE=%2FwEPDwULLTE2MDk1ODI3MTFkZMUq3L2kXLCgWE%2BxPNKGiR2aDkz5&__VIEWSTATEGENERATOR=00958D10&__EVENTVALIDATION=%2FwEWBALO%2BPagDALT8dy8BQKd%2B7qdDgLCi9reA9VqLcMs82KsM9lnbdFM5U4r7vSJ&txtUserID={user}&txtPwd={pass}&btnSubmit=Login");

            var z = await GetLocation(uri.ToString());
            var x = await GetCookies(z);
            Response.Cookies.Append("Token", x[0].Value);

            WebRequest webRequest = WebRequest.Create("https://schoolapps2.tvdsb.ca/students/portal_secondary/student_Info/timetable2.asp");
            foreach(var cookie in x)
            {
                webRequest.TryAddCookie(new Cookie(cookie.Name, cookie.Value, "/", "schoolapps2.tvdsb.ca"));
            }
            HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse();
            var encoding = ASCIIEncoding.ASCII;
            string responseText;
            using (var reader = new System.IO.StreamReader(response.GetResponseStream(), encoding))
            {
                responseText = reader.ReadToEnd();
            }
            htmlDoc.LoadHtml(responseText);
            var elem1 = htmlDoc.DocumentNode.Descendants("body").FirstOrDefault();
            var elem2 = elem1.Descendants("table").ElementAt(3);
            var elem3 = elem2.Descendants("tr");
            var classes = elem2.Descendants("td").Where(t => t.InnerText.Contains("- RM."));
            List<Class> classesList = new List<Class>();
            for (int i = 0; i < classes.ToList().Count; i++)
            {
                classesList.Add(Class.parse(classes.ElementAt(i).InnerText));
                var periods = classes.ElementAt(i).ParentNode.Descendants().Where(t => t.InnerText.Contains("Period"));
                if (periods.ToList().Count == 0)
                    classesList[i].Period = "0";
                else
                    classesList[i].Period = periods.FirstOrDefault().InnerText.Split(";")[1].Substring(0, 1);
            }
            int classesListSize = classesList.Count;
            for(int i = 1; i < classesListSize; i++)
            {
                int currentPeriod = int.Parse(classesList[i].Period);
                int lastPeriod = int.Parse(classesList[i - 1].Period);
                if(currentPeriod == 4)
                {
                    if (currentPeriod != lastPeriod + 2)
                        classesList.Add(new Class()
                        {
                            ClassCode = "Spare",
                            Period = (currentPeriod - 2).ToString(),
                            ClassRoom = "",
                            Teacher = ""
                        });
                }
                else
                {
                    if (currentPeriod != lastPeriod + 1)
                    {
                        classesList.Add(new Class()
                        {
                            ClassCode = "Spare",
                            Period = (currentPeriod - 1).ToString(),
                            ClassRoom = "",
                            Teacher = ""
                        });
                    }
                }
            }
            classesList = classesList.OrderBy(t => int.Parse(t.Period)).ToList();
            /*
            string cls1 = null, cls2 = null, cls3 = null, cls4 = null;
            try{ cls1 = tbl.ElementAt(0).InnerText; } catch { }
            try{ cls2 = tbl.ElementAt(1).InnerText; } catch { }
            try{ cls3 = tbl.ElementAt(2).InnerText; } catch { }
            try{ cls4 = tbl.ElementAt(3).InnerText; } catch { }
            Class cObj1 = cls1 != null ? Class.parse(cls1) : Class.Null;
            Class cObj2 = cls2 != null ? Class.parse(cls2) : Class.Null;
            Class cObj3 = cls3 != null ? Class.parse(cls3) : Class.Null;
            Class cObj4 = cls4 != null ? Class.parse(cls4) : Class.Null;*/
            var studentinfo = htmlDoc.DocumentNode.Descendants("body").FirstOrDefault().Descendants("table").ElementAt(1).Descendants("tr").ElementAt(0).Descendants("td");
            TimetableInfo tbi = new TimetableInfo();
            tbi.FirstName = studentinfo.ElementAt(0).ChildNodes[0].InnerText.Split(",").LastOrDefault().Trim();
            tbi.LastName = studentinfo.ElementAt(0).ChildNodes[0].InnerText.Split(",").FirstOrDefault().Trim();
            tbi.StudentNum = htmlDoc.DocumentNode.Descendants("td").FirstOrDefault(t => t.InnerText.Contains("Student#")).InnerText.Split(" ")[2].Substring(0, 9);
            tbi.OenNum = htmlDoc.DocumentNode.Descendants("td").FirstOrDefault(t => t.InnerText.Contains("Student#")).InnerText.Split(" ").LastOrDefault();
            tbi.Grade = htmlDoc.DocumentNode.Descendants("td").FirstOrDefault(t => t.InnerText.Contains("Grade: ")).InnerText.Split(" ").LastOrDefault();
            tbi.LockNum = htmlDoc.DocumentNode.Descendants("td").FirstOrDefault(t => t.InnerText.Contains("Locker #: ")).InnerText.Split(" ").LastOrDefault();
            tbi.Classes = classesList;
            return View(model: tbi);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
