using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

namespace PoHSyncEngine
{
	public sealed class DomainSyncEngine
	{
		private readonly System.Collections.Immutable.ImmutableSortedDictionary<string,DomainSyncConfiguration> _domainSyncConfigurationDic;
		private readonly ConcurrentDictionary<string,Thread> _daemonPullRequestThreadDictionary ;//= new ConcurrentDictionary<string,Thread>(); //daemon thread for each Domian that will pull the request
        private readonly ConcurrentDictionary<string, ConcurrentQueue<DomainSyncRequest>> _domainRequestQueueDictionary; //= new ConcurrentDictionary<string,ConcurrentQueue<DomainSyncRequest>>(); //queue that will store all the requests to be processed

		private readonly ConcurrentDictionary<string,ConcurrentDictionary<WorkerThreadInfo,Thread>> _domainDocGeneratorWorkerThreads = new ConcurrentDictionary<string,System.Collections.Concurrent.ConcurrentDictionary<WorkerThreadInfo,System.Threading.Thread>>();


		public DomainSyncEngine(DomainSyncConfiguration[] configuration)
		{
			if (configuration == null || (configuration.Any() == false)) throw new ArgumentNullException(nameof(configuration));
			_domainSyncConfigurationDic = configuration.ToLookup(c => c.DomainName.Value).ToDictionary(x => x.Key,xv => xv.First()).ToImmutableSortedDictionary();
			_daemonPullRequestThreadDictionary = new ConcurrentDictionary<string,Thread>(configuration.ToLookup(c => c.DomainName.Value).ToDictionary(x => x.Key,xv => new Thread(new ParameterizedThreadStart(DaemonPullRequest))));
			_domainRequestQueueDictionary = new ConcurrentDictionary<string,ConcurrentQueue<DomainSyncRequest>>(configuration.ToLookup(c => c.DomainName.Value).ToDictionary(x => x.Key,xv => new ConcurrentQueue<DomainSyncRequest>()));

            //Worker thread will exit if there is no work 
            foreach (var element in _domainSyncConfigurationDic)
            {
                var workerThreadDic = new ConcurrentDictionary<WorkerThreadInfo,Thread>();
                _domainDocGeneratorWorkerThreads.TryAdd(element.Key,workerThreadDic);
                LoadWorkerThreads(element.Key);
            }

			//Start the daemon threads
			foreach (var element in _daemonPullRequestThreadDictionary)
			{
				element.Value.Start(_domainSyncConfigurationDic[element.Key]);
			}

			

			//Assert whether initialization is properly done
			if (_domainSyncConfigurationDic.Any() == false)
			{
				throw new InvalidOperationException($"{nameof(_domainSyncConfigurationDic)} is Empty");
			}

			if (_daemonPullRequestThreadDictionary.Count() != _domainSyncConfigurationDic.Count())
			{
				throw new InvalidOperationException($"daemon threads have not been configured for all domains");
			}

			if (_domainDocGeneratorWorkerThreads.Count() != _domainSyncConfigurationDic.Count())
			{
				throw new InvalidOperationException($"Worker threads have not been configured for all domains");
			}

            var lessThanMinWorkerThreads = _domainDocGeneratorWorkerThreads
                .Where(d => d.Value.Count < _domainSyncConfigurationDic[d.Key].MinDocGenerationThread).ToList();

			if (lessThanMinWorkerThreads.Any())
            {
                var errorMessage = lessThanMinWorkerThreads.Aggregate(new StringBuilder(), (sb, d) =>
                {
                    sb.Append($"DomainL{d.Key}. WorkerThread Count:{d.Value.Count}");
                    return sb;
                }).ToString();
				throw new InvalidOperationException($"{errorMessage}");
			}
		}

        private void LoadWorkerThreads(string domainName)
		{
			var workerThreadDic = _domainDocGeneratorWorkerThreads[domainName];
			var domainConfig = _domainSyncConfigurationDic[domainName];
			var minThread = domainConfig.MinDocGenerationThread;
			var maxThread = domainConfig.MaxDocGenerationThread;
            var initialWorkerThreads = workerThreadDic.Count();
			var currentWorkerThreadCount = workerThreadDic.Count();
			var currentLoad = _domainRequestQueueDictionary[domainName].Count();
			var isThreadAdded = false;
			do
			{
				if ((currentWorkerThreadCount < minThread) || (currentWorkerThreadCount >= minThread && currentWorkerThreadCount < maxThread && currentLoad > currentWorkerThreadCount))
				{
					var w = new WorkerThreadInfo(currentWorkerThreadCount + 1,domainName);
					var x = new Thread(new ParameterizedThreadStart(WorkerDaemonJob));
					x.Start(w);
					workerThreadDic.TryAdd(w,x);
					currentWorkerThreadCount = workerThreadDic.Count();
					isThreadAdded = true;
				}
                else
                {
					isThreadAdded = false;
				}
			} while (isThreadAdded);

            if (initialWorkerThreads != currentWorkerThreadCount)
            {
                Console.WriteLine($"worker threads increased to:{currentWorkerThreadCount} from: {initialWorkerThreads} for domain {domainConfig.DomainName.Value}");
            }
        }

        private void WorkerDaemonJob(object workerThreadInfoArg)
		{
			var workerThreadInfo = workerThreadInfoArg as WorkerThreadInfo ?? throw  new ArgumentNullException(nameof(workerThreadInfoArg));
			var minThread = _domainSyncConfigurationDic[workerThreadInfo.DomainName].MinDocGenerationThread;
			var docGeneratorFun = _domainSyncConfigurationDic[workerThreadInfo.DomainName].DocumentGenerator.DomainDocumentGeneratorFunc;
			var domainRequestQueue = _domainRequestQueueDictionary[workerThreadInfo.DomainName];
			int currentWorkLoad = 0;
            do
            {
                if (domainRequestQueue.TryDequeue(out var req))
                {
                    var result = docGeneratorFun(req);
                }
                currentWorkLoad = domainRequestQueue.Count();
            } while (workerThreadInfo.ThreadSequenceNumber <= minThread || (workerThreadInfo.ThreadSequenceNumber > minThread && workerThreadInfo.ThreadSequenceNumber <= currentWorkLoad)); //Extra - (Non-Min) worker threads will exit if there is no work
			var workerThreadDic = _domainDocGeneratorWorkerThreads[workerThreadInfo.DomainName];
			workerThreadDic.Remove(workerThreadInfo,out var t);
            Console.WriteLine($"Removed Worker Thread {workerThreadInfo.DomainName} : {workerThreadInfo.ThreadSequenceNumber}. MinThread: {minThread}");
		}

        private void DaemonPullRequest(object domainSyncConfigurationArg)
		{
			DomainSyncConfiguration domainSyncConfiguration = domainSyncConfigurationArg as DomainSyncConfiguration ?? throw new ArgumentNullException(nameof(domainSyncConfigurationArg));
			var domainRequestQueue = _domainRequestQueueDictionary[domainSyncConfiguration.DomainName.Value];
			while (true)
			{
				if (domainRequestQueue.Count() < domainSyncConfiguration.MaxDocGenerationThread)
				{
					var request = domainSyncConfiguration.DomainRequestGenerator.DomainRequestGeneratorFunc(domainSyncConfiguration.DomainName.Value);
					if (request != null)
					{
						//Write to chanel the request
						domainRequestQueue.Enqueue(request);
						//If load is more and max threads haven't been reached -- spawn new threads for processing the request from channel
						LoadWorkerThreads(domainSyncConfiguration.DomainName.Value);
					}
				}
				else
				{
                    Console.WriteLine("sleeping");
					Thread.Sleep(10);
				}
			}

            
		}
	}

}
