using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TVWB
{
    public class Class {
        public string ClassCode { get; set; }
        public string ClassRoom { get; set; }
        public string Teacher { get; set; }

        public string Period { get; set; }

        public static Class parse(string innerText)
        {
            Class cObj = new Class();
            cObj.ClassCode = string.Join("-", innerText.Split("-").Take(2));
            cObj.ClassRoom = innerText.Split(" ")[1].Substring(3, innerText.Split(" ")[1].Length - 5);
            cObj.Teacher = innerText.Substring(cObj.ClassCode.Length + cObj.ClassRoom.Length + 5);
            return cObj;
        }

        public static Class Null = new Class() { ClassCode = "Spare", ClassRoom = "", Teacher = "" };
    }
    public class TimetableInfo
    {
        public List<Class> Classes { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string StudentNum { get; set; }
        public string OenNum { get; set; }
        public string Grade { get; set; }
        public string LockNum { get; set; }
    }
}
