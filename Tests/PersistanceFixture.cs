using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class PersistanceFixture
    {
        [Test]
        public void GetSortaTicksUniqueId()
        {
            Console.WriteLine(DateTime.Now.ToString("MMddHHmmssff"));
            Assert.That(long.Parse(DateTime.Now.ToString("MMddHHmmssff")) > 0);
        }
    }
}
