using System;
using System.Threading;
using PoHSyncEngine;

namespace PoHSyncTestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var engine = DocSyncTest.Engine;
            Console.WriteLine("Engine Started!");
            Console.ReadLine();
        }
    }

    public static class DocSyncTest
    {
        public static DomainSyncConfiguration PatientSyncConfig = new DomainSyncConfiguration(new NonEmptyString("Patient"),new GenericDomainSyncRequestGenerator("Patient"),new GenericDocumentGenerator("Patient"),2,5,new BlobSyncInfo(new NonEmptyString(@"http://")));
        public static DomainSyncEngine Engine = new DomainSyncEngine(new DomainSyncConfiguration[] { PatientSyncConfig });
    }

    public class DomainSyncRequestGeneratorFactory
    {
        Func<string,DomainSyncRequest> DomainRequestGeneratorFunc { get; }
    }

    public class GenericDomainSyncRequestGenerator : IDomainSyncRequestGenerator
    {
        public int _counter =0;
        public string DomainName { get; private set; }
        public Func<string,DomainSyncRequest> DomainRequestGeneratorFunc { get; }
        public GenericDomainSyncRequestGenerator(string domainName)
        {
            DomainRequestGeneratorFunc = (string domainName) =>
            {
                if (_counter > 5)
                {
                    Thread.Sleep(60000);
                }
                _counter++;
                Thread.Sleep(200);
                var retVal = new DomainSyncRequest(
                        new DomainID(new NonEmptyString(domainName), new NonEmptyString(_counter.ToString()), typeof(int)),
                        DateTime.Now);
                Console.WriteLine($"Request ID: {retVal.DomainID.Identity.Value}");
                return retVal;
            };
        }
    }

    public class GenericDocumentGenerator : IDocumentGenerator
    {
        public Func<DomainSyncRequest,DomainDocumentResult> DomainDocumentGeneratorFunc { get; private set; }
        public GenericDocumentGenerator(string domainName)
        {
            DomainDocumentGeneratorFunc = (DomainSyncRequest req) =>
            {
                Thread.Sleep(1000);
                var res = new DomainDocumentResult(req,
                    $"JSON Value of request:{req.DomainID.DomainName}_{req.DomainID.Identity.Value}");
                Console.WriteLine(res.JsonData);
                return res;

            };
        }
    }
}
