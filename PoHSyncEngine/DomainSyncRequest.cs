using System;


namespace PoHSyncEngine
{
    public class NonEmptyString
    {
        public string Value { get; private set; }
        public NonEmptyString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(nameof(value));
            }
            else
            {
                Value = value;
            }
        }
    }

    public sealed class DomainSyncRequest
    {
        public DomainID DomainID { get; private set; }
        public DateTime DomainRequestDate { get; private set; }

        public DomainSyncRequest(DomainID domainID,DateTime domainRequestDate)
        {
            this.DomainID = domainID ?? throw new ArgumentNullException(nameof(domainID));
            this.DomainRequestDate = domainRequestDate;
        }
    }

    public sealed class DomainID
    {
        public NonEmptyString DomainName { get; private set; }
        public NonEmptyString Identity { get; private set; }
        public Type IdentityType { get; private set; }
        public DomainID(NonEmptyString domainName,NonEmptyString identity,Type identityType)
        {
            DomainName = domainName;
            Identity = identity;
            IdentityType = identityType ?? throw new ArgumentNullException(nameof(identityType));

        }
    }

    public enum SyncType
    {
        Blob
    }

    public interface ISyncType
    {
        SyncType SyncType { get; }
    }

    public class BlobSyncInfo : ISyncType
    {
        public SyncType SyncType { get; } = SyncType.Blob;
        public NonEmptyString SyncURL { get; private set; }

        public BlobSyncInfo(NonEmptyString syncURL)
        {
            SyncURL = syncURL;
        }
    }

    public class DomainDocumentResult
    {
        public DomainSyncRequest DomainSyncRequest { get; private set; }
        public string JsonData { get; private set; }
        public DomainDocumentResult(DomainSyncRequest domainSyncRequest,string jsonData)
        {
            this.DomainSyncRequest = domainSyncRequest;
            this.JsonData = jsonData;
        }
    }

    public interface IDomainSyncRequestGenerator
    {
        Func<string,DomainSyncRequest> DomainRequestGeneratorFunc { get; }
    }

    public interface IDocumentGenerator
    {
        Func<DomainSyncRequest,DomainDocumentResult> DomainDocumentGeneratorFunc { get; }
    }

    public sealed class DomainSyncConfiguration
    {
        public NonEmptyString DomainName { get; private set; }
        public int MinDocGenerationThread { get; private set; }
        public int MaxDocGenerationThread { get; private set; }
        public ISyncType SyncType { get; private set; }
        public IDomainSyncRequestGenerator DomainRequestGenerator { get; private set; }
        public IDocumentGenerator DocumentGenerator { get; private set; }

        public DomainSyncConfiguration(NonEmptyString domainName,IDomainSyncRequestGenerator requestGenerator,IDocumentGenerator documentGenerator,int minDocGenerationThread,int maxDocGenerationThread,ISyncType syncType)
        {
            if (maxDocGenerationThread < minDocGenerationThread)
                throw new ArgumentOutOfRangeException($"Argument {nameof(maxDocGenerationThread)} should be more than {nameof(minDocGenerationThread)}");
            if (minDocGenerationThread < 1) throw new ArgumentOutOfRangeException($"Argument {nameof(minDocGenerationThread)} should be greater than zero");
            DomainName = domainName;
            DomainRequestGenerator = requestGenerator ?? throw new ArgumentNullException(nameof(requestGenerator));
            DocumentGenerator = documentGenerator ?? throw new ArgumentNullException(nameof(documentGenerator));
            MinDocGenerationThread = minDocGenerationThread;
            MaxDocGenerationThread = maxDocGenerationThread;
            SyncType = syncType;
        }
    }

    public sealed class WorkerThreadInfo
    {
        public int ThreadSequenceNumber { get; private set; }
        public string DomainName { get; private set; }

        public WorkerThreadInfo(int threadSequenceNumber,string domainName)
        {
            if (threadSequenceNumber < 1) throw new ArgumentException($"{nameof(threadSequenceNumber)} should not be less than 1");
            if (string.IsNullOrEmpty(domainName)) throw new ArgumentNullException(nameof(domainName));
            ThreadSequenceNumber = threadSequenceNumber;
            DomainName = domainName;
        }
    }

} //namespace

