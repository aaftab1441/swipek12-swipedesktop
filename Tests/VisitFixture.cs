using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Redis;
using ServiceStack.Text;
using SwipeDesktop.Models;
using SwipeDesktop.Storage;
using SwipeDesktop.ViewModels;

namespace Tests
{
    [TestFixture]
    public class VisitStorageFixture
    {
        readonly IRedisClient Client = new RedisClient();
        
        [TestFixtureSetUp]
        public void before_all()
        {
            
        }

        [TestFixtureTearDown]
        public void after_all()
        {
         
           
           Client.As<VisitModel>().DeleteAll();
           Client.Dispose();

        }

        [TearDown]
        public void after_each()
        {
            Client.As<VisitModel>().DeleteAll();

        }

        [Test]
        public void can_get_visit()
        {

            using (var redis = new VisitStorage())
            {
                var visit = new VisitModel
                {
                    FirstName = "James",
                    LastName = "Monroe",
                    Street1 = "100 Main Street",
                    City = "Balitmore",
                    State = "MD",
                    Zip = "21015",
                    DateOfBirth = DateTime.Parse("1/1/1986"),
                    Identification = "C-000-000-000-000"
                };
               
                redis.InsertObject(visit);

                var expected = redis.GetById(visit.Id);

                Assert.That(expected, Is.Not.Null);
                Console.WriteLine(visit.Id);

                Assert.That(expected.Identification, Is.EqualTo(visit.Identification));
                Assert.That(expected.LastName, Is.EqualTo(visit.LastName));
                Assert.That(expected.FirstName, Is.EqualTo(visit.FirstName));

            }

        }

        [Test]
        public void can_get_all_visit()
        {

            using (var redis = new RedisClient())
            {
                var typedClient = redis.As<VisitModel>();

                var scan1 = new VisitModel
                {
                    FirstName = "James",
                    LastName = "Monroe",
                    Street1 = "100 Main Street",
                    City = "Baltimore",
                    State = "MD",
                    Zip = "21015",
                    DateOfBirth = DateTime.Parse("1/1/1986"),
                    Identification = "C-000-000-000-000",
                    Id = typedClient.GetNextSequence()
                };


                var scan2 = new VisitModel
                {
                    FirstName = "James",
                    LastName = "Madison",
                    Street1 = "101 Main Street",
                    City = "Balitmore",
                    State = "MD",
                    Zip = "21234",
                    DateOfBirth = DateTime.Parse("1/1/1976"),
                    Identification = "C-000-000-000-001",
                    Id = typedClient.GetNextSequence()
                };

                redis.Store(scan1);
                redis.Store(scan2);
            }

            var scans = Client.As<VisitModel>().GetAll();
         
            //Recursively print the values of the POCO
            Console.WriteLine(scans.Dump());

            Assert.That(scans.Count, Is.EqualTo(2));
          
        }

        [Test]
        public void can_get_visits_by_date()
        {
            VisitModel scan1, scan2, scan3;

            using (var redis = new VisitStorage())
            {
               
                scan1 = new VisitModel
                {
                    FirstName = "James",
                    LastName = "Monroe",
                    Street1 = "100 Main Street",
                    City = "Baltimore",
                    State = "MD",
                    Zip = "21015",
                    DateOfBirth = DateTime.Parse("1/1/1986"),
                    Identification = "C-000-000-000-000",
                    VisitEntryDate = DateTime.Now
                };
              
                scan2 = new VisitModel
                {
                    FirstName = "James",
                    LastName = "Madison",
                    Street1 = "101 Main Street",
                    City = "Balitmore",
                    State = "MD",
                    Zip = "21234",
                    DateOfBirth = DateTime.Parse("1/1/1976"),
                    Identification = "C-000-000-000-001",
                    VisitEntryDate = DateTime.Now.AddHours(1.4)
                };

                scan3 = new VisitModel
                {
                    FirstName = "James",
                    LastName = "Madison",
                    Street1 = "101 Main Street",
                    City = "Balitmore",
                    State = "MD",
                    Zip = "21234",
                    DateOfBirth = DateTime.Parse("1/1/1976"),
                    Identification = "C-000-000-000-001",
                    VisitEntryDate = DateTime.Now.AddDays(1)
                };

                redis.InsertObject(scan1);
                redis.InsertObject(scan2);
                redis.InsertObject(scan3);
            }

        
            var scans = Client.As<VisitModel>().GetAll();
            Console.WriteLine(scans.Dump());

            Assert.That(scans.Count, Is.EqualTo(3));

            using (var redis = new VisitStorage())
            {
                string urn = null, urn2 = null;
                var set = redis.FindByDate(DateTime.Today, out urn);

                Console.WriteLine(set.Dump());
                
                Assert.That(set.Contains(scan1.Id), Is.True);
                Assert.That(set.Contains(scan2.Id), Is.True);
                Assert.That(set.Contains(scan3.Id), Is.False);

                var set2 = redis.FindByDate(DateTime.Today.AddDays(1), out urn2);

                Console.WriteLine(set2.Dump());
              
                Assert.That(set2.Contains(scan1.Id), Is.False);
                Assert.That(set2.Contains(scan2.Id), Is.False);
                Assert.That(set2.Contains(scan3.Id), Is.True);

            }
        }
    }
}
