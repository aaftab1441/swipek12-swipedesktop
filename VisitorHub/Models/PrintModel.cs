using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SwipeDesktop.Common;
using SwipeDesktop.ViewModels;

namespace SwipeDesktop.Models
{
   
    public class PrintModel<T>
    {
        public PrintModel(T model)
        {
            DataModel = model;
            PrintDate = DateTime.Today;
        }


        public DateTime PrintDate { get; set; }

        public string Title { get; set; }

        private string _school;
        public string SchoolName { get { return _school.SafeToUpper(); } set { _school = value; } }

        public T DataModel { get; set; }
    }
}
