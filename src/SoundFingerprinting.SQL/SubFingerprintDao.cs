namespace SoundFingerprinting.SQL
{
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;

    using SoundFingerprinting.DAO;
    using SoundFingerprinting.DAO.Data;
    using SoundFingerprinting.Data;
    using SoundFingerprinting.Infrastructure;
    using SoundFingerprinting.Math;
    using SoundFingerprinting.SQL.Connection;
    using SoundFingerprinting.SQL.DAO;
    using SoundFingerprinting.SQL.ORM;

    internal class SubFingerprintDao : AbstractDao, ISubFingerprintDao
    {
        private const string SpInsertSubFingerprint = "sp_InsertSubFingerprint";
        private const string SpReadFingerprintsByHashBinHashTableAndThreshold = "sp_ReadFingerprintsByHashBinHashTableAndThreshold";
        private const string SpReadSubFingerprintsByHashBinHashTableAndThresholdWithClusters =
            "sp_ReadSubFingerprintsByHashBinHashTableAndThresholdWithClusters";

        private const string SpReadSubFingerprintsByTrackId = "sp_ReadSubFingerprintsByTrackId";

        private readonly IHashConverter hashConverter;

        public SubFingerprintDao()
            : this(
                DependencyResolver.Current.Get<IDatabaseProviderFactory>(),
                DependencyResolver.Current.Get<IModelBinderFactory>(),
                DependencyResolver.Current.Get<IHashConverter>())
        {
            // no op
        }

        public SubFingerprintDao(IDatabaseProviderFactory databaseProvider, IModelBinderFactory modelBinderFactory, IHashConverter hashConverter)
            : base(databaseProvider, modelBinderFactory)
        {
            this.hashConverter = hashConverter;
        }

        public void InsertHashDataForTrack(IEnumerable<HashedFingerprint> hashes, IModelReference trackReference)
        {
            foreach (var hashedFingerprint in hashes)
            {
                var procedure =
                    PrepareStoredProcedure(SpInsertSubFingerprint)
                            .WithParameter("TrackId", trackReference.Id, DbType.Int32)
                            .WithParameter("SequenceNumber", hashedFingerprint.SequenceNumber, DbType.Int32)
                            .WithParameter("SequenceAt", hashedFingerprint.StartsAt, DbType.Double)
                            .WithParameter("Clusters", string.Join(",", hashedFingerprint.Clusters), DbType.String);

                for (int i = 0; i < hashedFingerprint.HashBins.Length; ++i)
                {
                    procedure.WithParameter("HashTable_" + i, hashedFingerprint.HashBins[i], DbType.Int32);
                }

                procedure.Execute().AsScalar<long>();
            }
        }

        public IList<HashedFingerprint> ReadHashedFingerprintsByTrackReference(IModelReference trackReference)
        {
            return this.PrepareStoredProcedure(SpReadSubFingerprintsByTrackId)
                .WithParameter("TrackId", trackReference.Id, DbType.Int32)
                .Execute()
                .AsListOfModel<SubFingerprintDTO>()
                .Select(dto =>
                    {
                        var hashes = GetHashes(dto);
                        byte[] signature = hashConverter.ToBytes(hashes, 100);
                        return new HashedFingerprint(signature, hashes, dto.SequenceNumber, dto.SequenceAt, string.IsNullOrEmpty(dto.Clusters) ? Enumerable.Empty<string>() : dto.Clusters.Split(','));
                    }).ToList();
        }

        public IEnumerable<SubFingerprintData> ReadSubFingerprints(long[] hashBins, int thresholdVotes, IEnumerable<string> clusters)
        {
            return PrepareReadSubFingerprintsByHashBuckets(hashBins, thresholdVotes, clusters)
                    .Execute()
                    .AsListOfModel<SubFingerprintDTO>()
                    .Select(GetSubFingerprintData);
        }

        public ISet<SubFingerprintData> ReadSubFingerprints(IEnumerable<long[]> hashes, int threshold, IEnumerable<string> clusters)
        {
            var set = new HashSet<SubFingerprintData>();
            foreach (var subFingerprintData in hashes.Select(hash => this.ReadSubFingerprints(hash, threshold, clusters)).SelectMany(subs => subs))
            {
                set.Add(subFingerprintData);
            }

            return set;
        }

        private IParameterBinder PrepareReadSubFingerprintsByHashBuckets(long[] hashBuckets, int thresholdVotes, IEnumerable<string> clusters)
        {
            string storedProcedure = SpReadFingerprintsByHashBinHashTableAndThreshold;
            var enumerable = clusters as List<string> ?? clusters.ToList();
            if (enumerable.Any())
            {
                storedProcedure = SpReadSubFingerprintsByHashBinHashTableAndThresholdWithClusters;
            }

            var parameterBinder = this.PrepareStoredProcedure(storedProcedure);
            for (int hashTable = 0; hashTable < hashBuckets.Length; hashTable++)
            {
                parameterBinder = parameterBinder.WithParameter("HashBin_" + hashTable, hashBuckets[hashTable]);
            }

            if (enumerable.Any())
            {
                parameterBinder.WithParameter("Clusters", string.Format("%{0}%", string.Join(",", enumerable)));
            }

            return parameterBinder.WithParameter("Threshold", thresholdVotes);
        }

        private SubFingerprintData GetSubFingerprintData(SubFingerprintDTO dto)
        {
            long[] hashes = GetHashes(dto);
            return new SubFingerprintData(
                hashes,
                dto.SequenceNumber,
                dto.SequenceAt,
                new ModelReference<long>(dto.Id),
                new ModelReference<int>(dto.TrackId));
        }

        private long[] GetHashes(SubFingerprintDTO dto)
        {
            long[] hashes = new[]
                {
                    dto.HashTable_0, dto.HashTable_1, dto.HashTable_2, dto.HashTable_3, dto.HashTable_4, dto.HashTable_5,
                    dto.HashTable_6, dto.HashTable_7, dto.HashTable_8, dto.HashTable_9, dto.HashTable_10, dto.HashTable_11,
                    dto.HashTable_12, dto.HashTable_13, dto.HashTable_14, dto.HashTable_15, dto.HashTable_16, dto.HashTable_17,
                    dto.HashTable_18, dto.HashTable_19, dto.HashTable_20, dto.HashTable_21, dto.HashTable_22,
                    dto.HashTable_23, dto.HashTable_24
                };
            return hashes;
        }

    }
}
