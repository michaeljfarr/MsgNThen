using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MsgNThen.Redis.Converters
{
    /// <summary>
    /// See also https://devblogs.microsoft.com/pfxteam/parallelextensionsextras-tour-4-blockingcollectionextensions/
    /// </summary>
    static class TaskPartitioner
    {
        public static Partitioner<T> GetConsumingPartitioner<T>(this BlockingCollection<T> collection,
            CancellationToken cancellationToken)
        {
            return new BlockingCollectionPartitioner<T>(collection, cancellationToken);
        }

        private class BlockingCollectionPartitioner<T> : Partitioner<T>
        {
            private readonly BlockingCollection<T> _collection;
            private readonly CancellationToken _cancellationToken;

            internal BlockingCollectionPartitioner(BlockingCollection<T> collection,
                CancellationToken cancellationToken)
            {
                _collection = collection ?? throw new ArgumentNullException(nameof(collection));
                _cancellationToken = cancellationToken;
            }

            public override bool SupportsDynamicPartitions => true;

            public override IList<IEnumerator<T>> GetPartitions(int partitionCount)
            {
                if (partitionCount < 1)
                    throw new ArgumentOutOfRangeException(nameof(partitionCount));

                var dynamicPartitioner = GetDynamicPartitions();
                return Enumerable.Range(0, partitionCount).Select(_ =>
                    dynamicPartitioner.GetEnumerator()).ToArray();
            }

            public override IEnumerable<T> GetDynamicPartitions()
            {
                return _collection.GetConsumingEnumerable(_cancellationToken);
            }
        }
    }
}