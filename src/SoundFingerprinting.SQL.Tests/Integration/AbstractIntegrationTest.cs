namespace SoundFingerprinting.SQL.Tests.Integration
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using SoundFingerprinting.Audio;
    using SoundFingerprinting.DAO;
    using SoundFingerprinting.DAO.Data;
    using SoundFingerprinting.Data;

    [DeploymentItem(@"TestEnvironment\floatsamples.bin")]
    [DeploymentItem(@"TestEnvironment\Kryptonite.mp3")]
    [DeploymentItem(@"x86", @"x86")]
    [DeploymentItem(@"x64", @"x64")]
    [TestClass]
    public abstract class AbstractIntegrationTest 
    {
        protected const double Epsilon = 0.0001;

        protected const int SampleRate = 5512;

        protected const string PathToMp3 = @"Kryptonite.mp3";

        protected const string PathToSamples = @"floatsamples.bin";

        protected readonly bool[] GenericFingerprint = new[]
            {
                true, false, true, false, true, false, true, false, true, false, true, false, false, true, false, true,
                false, true, false, true, false, true, false, true, true, false, true, false, true, false, true, false,
                true, false, true, false, false, true, false, true, false, true, false, true, false, true, false, true,
                true, false, true, false, true, false, true, false, true, false, true, false, false, true, false, true,
                false, true, false, true, false, true, false, true, true, false, true, false, true, false, true, false,
                true, false, true, false, false, true, false, true, false, true, false, true, false, true, false, true,
                true, false, true, false, true, false, true, false, true, false, true, false, false, true, false, true,
                false, true, false, true, false, true, false, true
            };

        protected readonly byte[] GenericSignature = new[]
            {
                (byte)1, (byte)0, (byte)0, (byte)0,
                (byte)2, (byte)0, (byte)0, (byte)0,
                (byte)3, (byte)0, (byte)0, (byte)0,
                (byte)4, (byte)0, (byte)0, (byte)0,
                (byte)5, (byte)0, (byte)0, (byte)0,
                (byte)6, (byte)0, (byte)0, (byte)0,
                (byte)7, (byte)0, (byte)0, (byte)0,
                (byte)8, (byte)0, (byte)0, (byte)0,
                (byte)9, (byte)0, (byte)0, (byte)0,
                (byte)10, (byte)0, (byte)0, (byte)0,
                (byte)11, (byte)0, (byte)0, (byte)0,
                (byte)12, (byte)0, (byte)0, (byte)0,
                (byte)13, (byte)0, (byte)0, (byte)0,
                (byte)14, (byte)0, (byte)0, (byte)0,
                (byte)15, (byte)0, (byte)0, (byte)0,
                (byte)16, (byte)0, (byte)0, (byte)0,
                (byte)17, (byte)0, (byte)0, (byte)0,
                (byte)18, (byte)0, (byte)0, (byte)0,
                (byte)19, (byte)0, (byte)0, (byte)0,
                (byte)20, (byte)0, (byte)0, (byte)0,
                (byte)21, (byte)0, (byte)0, (byte)0,
                (byte)22, (byte)0, (byte)0, (byte)0,
                (byte)23, (byte)0, (byte)0, (byte)0,
                (byte)24, (byte)0, (byte)0, (byte)0,
                (byte)25, (byte)0, (byte)0, (byte)0,
            };

        protected readonly long[] GenericHashBuckets = new[]
            {
                1L, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25 
            };

        protected void AssertTracksAreEqual(TrackData expectedTrack, TrackData actualTrack)
        {
            Assert.AreEqual(expectedTrack.TrackReference, actualTrack.TrackReference);
            Assert.AreEqual(expectedTrack.Album, actualTrack.Album);
            Assert.AreEqual(expectedTrack.Artist, actualTrack.Artist);
            Assert.AreEqual(expectedTrack.Title, actualTrack.Title);
            Assert.AreEqual(expectedTrack.TrackLengthSec, actualTrack.TrackLengthSec);
            Assert.AreEqual(expectedTrack.ISRC, actualTrack.ISRC);
            Assert.AreEqual(expectedTrack.GroupId, actualTrack.GroupId);
        }

        protected void AssertHashDatasAreTheSame(IList<HashedFingerprint> firstHashDatas, IList<HashedFingerprint> secondHashDatas)
        {
            Assert.AreEqual(firstHashDatas.Count, secondHashDatas.Count);
         
            // hashes are not ordered as parallel computation is involved
            firstHashDatas = this.SortHashesByFirstValueOfHashBin(firstHashDatas);
            secondHashDatas = this.SortHashesByFirstValueOfHashBin(secondHashDatas);

            for (int i = 0; i < firstHashDatas.Count; i++)
            {
                CollectionAssert.AreEqual(firstHashDatas[i].SubFingerprint, secondHashDatas[i].SubFingerprint);
                CollectionAssert.AreEqual(firstHashDatas[i].HashBins, secondHashDatas[i].HashBins);
                Assert.AreEqual(firstHashDatas[i].SequenceNumber, secondHashDatas[i].SequenceNumber);
                Assert.AreEqual(firstHashDatas[i].StartsAt, secondHashDatas[i].StartsAt, Epsilon);
            }
        }

        protected void AssertModelReferenceIsInitialized(IModelReference modelReference)
        {
            Assert.IsNotNull(modelReference);
            Assert.IsTrue(modelReference.GetHashCode() != 0);
        }
 
        protected TagInfo GetTagInfo()
        {
            return new TagInfo
                {
                    Album = "Album",
                    AlbumArtist = "AlbumArtist",
                    Artist = "Artist",
                    Composer = "Composer",
                    Duration = 100.2,
                    Genre = "Genre",
                    IsEmpty = false,
                    ISRC = "ISRC",
                    Title = "Title",
                    Year = 1986
                };
        }

        private List<HashedFingerprint> SortHashesByFirstValueOfHashBin(IEnumerable<HashedFingerprint> hashDatasFromFile)
        {
            return hashDatasFromFile.OrderBy(hashData => hashData.SequenceNumber).ToList();
        }
    }
}
