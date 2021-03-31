using System;
using System.Collections.Generic;
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
        public Func<DomainPullRequest,List<DomainSyncRequest>> DomainRequestGeneratorFunc { get; }
        public GenericDomainSyncRequestGenerator(string domainName)
        {
            DomainRequestGeneratorFunc = (DomainPullRequest pullRequest) =>
            {
                if (_counter > 15)
                {
                    Thread.Sleep(5000);
                }
                Thread.Sleep(200);
                List<DomainSyncRequest> retVal = new List<DomainSyncRequest>();
                for (int i = 0; i < pullRequest.MaxNumberOfPullRequest; i++)
                {
                    _counter++;
                    var req = new DomainSyncRequest(
                        new DomainID(new NonEmptyString(domainName),new NonEmptyString(_counter.ToString()),typeof(int)),
                        DateTime.Now);
                    Console.WriteLine($"Request ID: {req.DomainID.Identity.Value}");
                    retVal.Add(req);
                }
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
