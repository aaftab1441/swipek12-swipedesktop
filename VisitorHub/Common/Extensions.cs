using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Messages;
using SwipeDesktop.Models;
using Telerik.Windows.Documents.Layout;

namespace SwipeDesktop.Common
{
 
        public static class HelperExtensions
        {
        public static Bitmap cropAtRect(this Bitmap b, Rectangle r)
        {
            Bitmap nb = new Bitmap(r.Width, r.Height);
            Graphics g = Graphics.FromImage(nb);
            g.DrawImage(b, -r.X, -r.Y);
            return nb;
        }
            public static DateTime AddBusinessDays(this DateTime date, int days)
            {
                if (days < 0)
                {
                    throw new ArgumentException("days cannot be negative", "days");
                }

                if (days == 0) return date;

                if (date.DayOfWeek == DayOfWeek.Saturday)
                {
                    date = date.AddDays(2);
                    days -= 1;
                }
                else if (date.DayOfWeek == DayOfWeek.Sunday)
                {
                    date = date.AddDays(1);
                    days -= 1;
                }

                date = date.AddDays(days / 5 * 7);
                int extraDays = days % 5;

                if ((int)date.DayOfWeek + extraDays > 5)
                {
                    extraDays += 2;
                }

                return date.AddDays(extraDays);

            }
        public static string SafeToUpper(this string value)
            {
                if (!string.IsNullOrEmpty(value))
                {
                    return value.ToUpper();
                }

                return string.Empty;
            }

            public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
            {
                HashSet<TKey> seenKeys = new HashSet<TKey>();
                foreach (TSource element in source)
                {
                    if (seenKeys.Add(keySelector(element)))
                    {
                        yield return element;
                    }
                }
            }
            public static Boolean CompareOperator(string logic, int x, int y)
            {
                switch (logic)
                {
                    case "GreaterThanOrEqual": return x >= y;
                    case "GreaterThan": return x > y;
                    case "Equal": return x == y;

                    default: return x == y;
                }
            }
            public static IEnumerable<T> Select<T>(this SqlDataReader reader, Func<SqlDataReader, T> projection)
            {
                while (reader.Read())
                {
                    yield return projection(reader);
                }
            }

            public static VisitLog VisitBuilder(IDataReader reader)
            {

                return new VisitLog()
                {
                    Id = reader["pk_VisitRecord"].ToString(),
                    VisitNumber = reader["VisitNumber"].ToString(),
                    VisitorLastName = reader["LastName"].ToString(),
                    VisitorFirstName = reader["FirstName"].ToString(),
                    ExitDate = reader["DateExited"] as DateTime?

                };
            }

            public static PersonModel ViewModelBuilder(IDataReader reader)
            {

                if (int.Parse(reader["PersonTypeId"].ToString()) > 1)
                {

                    return new PersonModel()
                    {
                        IsManualEntry = true,
                        UniqueId = Guid.Empty,
                        IdNumber = reader["SSN"].ToString(),
                        LastName = reader["LastName"].ToString(),
                        FirstName = reader["FirstName"].ToString(),

                        PhotoPath = reader["PhotoPath"].ToString(),
                        PersonId = int.Parse(reader["PersonId"].ToString()),

                    };
                }

                if (DBNull.Value != reader["StudentId"] && reader["PersonTypeId"].ToString() == "1")
                {
                    Guid guid = Guid.Empty;
                    if (reader["Guid"] != null)
                    {
                        Guid.TryParse(reader["Guid"].ToString(), out guid);
                    }

                    var s = new StudentModel()
                    {
                        IsManualEntry = true,
                        UniqueId = guid,
                        IdNumber = reader["SSN"].ToString(),
                        LastName = reader["LastName"].ToString(),
                        FirstName = reader["FirstName"].ToString(),
                        Grade = DBNull.Value == reader["Grade"] ? "N/A" : reader["Grade"].ToString(),
                        Homeroom = DBNull.Value == reader["Homeroom"] ? "N/A" : reader["Homeroom"].ToString(),
                        PhotoPath = reader["PhotoPath"].ToString(),
                        PersonId = int.Parse(reader["PersonId"].ToString()),
                        StudentId = reader.GetInt32(0)
                    };

                    return s;
                }
                return null;
            }

            public static BitmapSource LoadImage(this Byte[] imageBytes)
            {
                BitmapImage bmpImage = new BitmapImage();
                MemoryStream mystream = new MemoryStream(imageBytes);
                bmpImage.SetSource(mystream);
                return bmpImage;
            }

        public static dynamic VisitorPassBuilder(this IDataReader reader)
            {
               
                return new 
                {
                    IsManualEntry = true,
                    UniqueId = Guid.Parse(reader[0].ToString()),
                    FirstName = reader[1].ToString(),
                    LastName = reader[2].ToString(),
                    Expiration = DateTime.Parse(reader[3].ToString()),
                    PassType = reader[4].ToString(),
                    PassId = reader[5].ToString(),
                    SchoolId = reader[6].ToString(),
                    Image = DBNull.Value == reader[7] ? null : Encoding.UTF8.GetBytes(reader[7].ToString()),
                    DateOfBirth = DBNull.Value == reader[8] ? DateTime.MinValue : DateTime.Parse(reader[8].ToString())
                };

            }
    }


    
}
