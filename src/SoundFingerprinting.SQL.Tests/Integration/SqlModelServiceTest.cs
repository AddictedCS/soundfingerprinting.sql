namespace SoundFingerprinting.SQL.Tests.Integration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Transactions;

    using NUnit.Framework;

    using SoundFingerprinting.Configuration;
    using SoundFingerprinting.DAO.Data;
    using SoundFingerprinting.Data;
    using SoundFingerprinting.SQL;

    [TestFixture]
    public class SqlModelServiceTest : AbstractIntegrationTest
    {
        private readonly IAdvancedModelService modelService;
        private TransactionScope transactionPerTestScope;

        public SqlModelServiceTest()
        {
            modelService = new SqlModelService();
        }

        [SetUp]
        public void SetUp()
        {
            transactionPerTestScope = new TransactionScope();
        }

        [TearDown]
        public void TearDown()
        {
            transactionPerTestScope.Dispose();
        }

        [Test]
        public void InsertTrackTest()
        {
            var track = new TrackData("isrc", "artist", "title", "album", 1986, 200);

            var trackReference = modelService.InsertTrack(track);

            AssertModelReferenceIsInitialized(trackReference);
        }

        [Test]
        public void ReadTrackByTrackReferenceTest()
        {
            var expectedTrack = new TrackData("isrc", "artist", "title", "album", 1986, 200);
            var trackReference = modelService.InsertTrack(expectedTrack);

            var actualTrack = modelService.ReadTrackByReference(trackReference);

            AssertTracksAreEqual(expectedTrack, actualTrack);
        }

        [Test]
        public void ReadTrackByISRCTest()
        {
            const string ISRC = "isrc";
            var expectedTrack = new TrackData(ISRC, "artist", "title", "album", 1986, 200);
            modelService.InsertTrack(expectedTrack);

            var actualTrack = modelService.ReadTrackByISRC(ISRC);

            AssertTracksAreEqual(expectedTrack, actualTrack);
        }

        [Test]
        public void ReadTrackByArtistAndTitleTest()
        {
            const string Artist = "artist";
            const string Title = "title";
            var expectedTrack = new TrackData("isrc", Artist, Title, "album", 1986, 200);
            modelService.InsertTrack(expectedTrack);

            var actualTracks = modelService.ReadTrackByArtistAndTitleName(Artist, Title);

            Assert.IsTrue(actualTracks.Count == 1);
            AssertTracksAreEqual(expectedTrack, actualTracks[0]);
        }

        [Test]
        public void ReadMultipleTracksTest()
        {
            const int NumberOfTracks = 100;
            var allTracks = new HashSet<TrackData>();
            for (int i = 0; i < NumberOfTracks; i++)
            {
                TrackData track = new TrackData("isrc" + i, "artist", "title", "album", 1986, 200);
                modelService.InsertTrack(track);
                if (!allTracks.Add(track))
                {
                    Assert.Fail("Same primary key identifier was returned after inserting a track to the collection.");
                }
            }

            var actualTracks = modelService.ReadAllTracks();
            Assert.IsTrue(actualTracks.Count == NumberOfTracks);
        }

        [Test]
        public void DeleteTrackTest()
        {
            TrackData track = new TrackData("isrc", "artist", "title", "album", 1986, 200);
            var trackReference = modelService.InsertTrack(track);
            var hashedFingerprints = new HashedFingerprint(GenericSignature, GenericHashBuckets, 1, 0.928, Enumerable.Empty<string>());
            modelService.InsertHashDataForTrack(new[] { hashedFingerprints }, trackReference);

            modelService.DeleteTrack(trackReference);

            var subFingerprints = modelService.ReadSubFingerprints(GenericHashBuckets, new DefaultQueryConfiguration());
            Assert.IsTrue(subFingerprints.Any() == false);
            TrackData actualTrack = modelService.ReadTrackByReference(trackReference);
            Assert.IsNull(actualTrack);
        }

        [Test]
        public void InsertHashDataTest()
        {
            TrackData expectedTrack = new TrackData("isrc", "artist", "title", "album", 1986, 200);
            var trackReference = modelService.InsertTrack(expectedTrack);
            var hashedFingerprints = new HashedFingerprint(GenericSignature, GenericHashBuckets, 1, 0.928, Enumerable.Empty<string>());
            modelService.InsertHashDataForTrack(new[] { hashedFingerprints }, trackReference);

            var subFingerprints = modelService.ReadSubFingerprints(GenericHashBuckets, new DefaultQueryConfiguration());

            Assert.IsTrue(subFingerprints.Count == 1);
            Assert.AreEqual(trackReference, subFingerprints[0].TrackReference);
            Assert.IsFalse(subFingerprints[0].SubFingerprintReference.GetHashCode() == 0);
            CollectionAssert.AreEqual(GenericHashBuckets, subFingerprints[0].Hashes);
        }

        [Test]
        public void ReadSubFingerprintsByHashBucketsHavingThresholdTest()
        {
            TrackData firstTrack = new TrackData("isrc1", "artist", "title", "album", 1986, 200);
            var firstTrackReference = modelService.InsertTrack(firstTrack);
            TrackData secondTrack = new TrackData("isrc2", "artist", "title", "album", 1986, 200);
            var secondTrackReference = modelService.InsertTrack(secondTrack);
            Assert.IsFalse(firstTrackReference.Equals(secondTrackReference));
            long[] firstTrackBuckets = new long[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25 };
            long[] secondTrackBuckets = new long[] { 2, 2, 4, 5, 6, 7, 7, 9, 10, 11, 12, 13, 14, 14, 16, 17, 18, 19, 20, 20, 22, 23, 24, 25, 26 };
            var firstHashData = new HashedFingerprint(GenericSignature, firstTrackBuckets, 1, 0.928, Enumerable.Empty<string>());
            var secondHashData = new HashedFingerprint(GenericSignature, secondTrackBuckets, 1, 0.928, Enumerable.Empty<string>());

            modelService.InsertHashDataForTrack(new[] { firstHashData }, firstTrackReference);
            modelService.InsertHashDataForTrack(new[] { secondHashData }, secondTrackReference);

            // query buckets are similar with 5 elements from first track and 4 elements from second track
            long[] queryBuckets = new long[] { 3, 2, 5, 6, 7, 8, 7, 10, 11, 12, 13, 14, 15, 14, 17, 18, 19, 20, 21, 20, 23, 24, 25, 26, 25 };

            var subFingerprints = modelService.ReadSubFingerprints(queryBuckets, new DefaultQueryConfiguration());

            Assert.IsTrue(subFingerprints.Count == 1);
            Assert.AreEqual(firstTrackReference, subFingerprints[0].TrackReference);
        }

        [Test]
        public void ReadSubFingerprintsByHashBucketsHavingThresholdWithGroupIdTest()
        {
            TrackData firstTrack = new TrackData("isrc1", "artist", "title", "album", 1986, 200);
            var firstTrackReference = modelService.InsertTrack(firstTrack);
            TrackData secondTrack = new TrackData("isrc2", "artist", "title", "album", 1986, 200);
            var secondTrackReference = modelService.InsertTrack(secondTrack);
            Assert.IsFalse(firstTrackReference.Equals(secondTrackReference));
            long[] firstTrackBuckets = new long[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25 };
            long[] secondTrackBuckets = new long[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25 };
            var firstHashData = new HashedFingerprint(
                GenericSignature, firstTrackBuckets, 1, 0.928, new[] { "first-group-id" });
            var secondHashData = new HashedFingerprint(
                GenericSignature, secondTrackBuckets, 1, 0.928, new[] { "second-group-id" });

            modelService.InsertHashDataForTrack(new[] { firstHashData }, firstTrackReference);
            modelService.InsertHashDataForTrack(new[] { secondHashData }, secondTrackReference);

            // query buckets are similar with 5 elements from first track and 4 elements from second track
            long[] queryBuckets = new long[] { 3, 2, 5, 6, 7, 8, 7, 10, 11, 12, 13, 14, 15, 14, 17, 18, 19, 20, 21, 20, 23, 24, 25, 26, 25 };

            var subFingerprints = modelService.ReadSubFingerprints(queryBuckets, new DefaultQueryConfiguration { Clusters = new[] { "first-group-id" } });

            Assert.IsTrue(subFingerprints.Count == 1);
            Assert.AreEqual(firstTrackReference, subFingerprints[0].TrackReference);
        }

        [Test]
        public void InsertFingerprintTest()
        {
            var track = new TrackData("isrc", "artist", "title", "album", 1986, 200);
            var trackReference = modelService.InsertTrack(track);
            var fingerprint = new FingerprintData(GenericFingerprint, trackReference);

            modelService.InsertFingerprint(fingerprint);

            AssertModelReferenceIsInitialized(fingerprint.FingerprintReference);
        }

        [Test]
        public void ReadFingerprintsByTrackReferenceTest()
        {
            TrackData track = new TrackData("isrc", "artist", "title", "album", 1986, 200);
            var trackReference = modelService.InsertTrack(track);
            FingerprintData fingerprint = new FingerprintData(GenericFingerprint, trackReference);
            modelService.InsertFingerprint(fingerprint);

            var fingerprints = modelService.ReadFingerprintsByTrackReference(trackReference);

            Assert.IsTrue(fingerprints.Count == 1);
            Assert.AreEqual(fingerprint.FingerprintReference, fingerprints[0].FingerprintReference);
            Assert.AreEqual(trackReference, fingerprints[0].TrackReference);
            for (int i = 0; i < GenericFingerprint.Length; i++)
            {
                Assert.AreEqual(GenericFingerprint[i], fingerprints[0].Signature[i]);
            }
        }
    }
}
