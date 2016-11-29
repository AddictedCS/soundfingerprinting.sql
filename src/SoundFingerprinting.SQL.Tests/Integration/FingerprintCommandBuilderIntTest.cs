namespace SoundFingerprinting.SQL.Tests.Integration
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Transactions;

    using NUnit.Framework;

    using SoundFingerprinting.Audio;
    using SoundFingerprinting.Audio.Bass;
    using SoundFingerprinting.Audio.NAudio;
    using SoundFingerprinting.Builder;
    using SoundFingerprinting.Command;
    using SoundFingerprinting.Configuration;
    using SoundFingerprinting.DAO.Data;
    using SoundFingerprinting.Strides;

    [TestFixture]
    public class FingerprintCommandBuilderIntTest : AbstractIntegrationTest
    {
        private static readonly Random Rand = new Random();

        private readonly ModelService modelService;
        private readonly IFingerprintCommandBuilder fingerprintCommandBuilder;
        private readonly QueryFingerprintService queryFingerprintService;
        private readonly BassAudioService bassAudioService;
        private readonly BassWaveFileUtility bassWaveFileUtility;
        private readonly NAudioService audioService;

        private TransactionScope transactionPerTestScope;

        public FingerprintCommandBuilderIntTest()
        {
            bassAudioService = new BassAudioService();
            audioService = new NAudioService();
            modelService = new SqlModelService();
            bassWaveFileUtility = new BassWaveFileUtility();
            fingerprintCommandBuilder = new FingerprintCommandBuilder();
            queryFingerprintService = new QueryFingerprintService();
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
        public void CreateFingerprintsFromDefaultFileAndAssertNumberOfFingerprints()
        {
            const int StaticStride = 5115;
            var tagService = new BassTagService();

            var fingerprintCommand = fingerprintCommandBuilder.BuildFingerprintCommand()
                                        .From(PathToMp3)
                                        .WithFingerprintConfig(config => config.Stride = new IncrementalStaticStride(StaticStride))
                                        .UsingServices(bassAudioService);
                                    
            double seconds = tagService.GetTagInfo(PathToMp3).Duration;
            int expectedFingerprints = (int)(seconds * fingerprintCommand.FingerprintConfiguration.SampleRate / StaticStride * StaticStride / StaticStride) - 2; // ? new file generates 1 fingerprint less;

            var fingerprints = ((FingerprintCommand)fingerprintCommand).Fingerprint().Result;

            Assert.AreEqual(expectedFingerprints, fingerprints.Count);
        }

        [Test]
        public void CreateFingerprintsInsertThenQueryAndGetTheRightResult()
        {
            const int SecondsToProcess = 10;
            const int StartAtSecond = 30;
            var tagService = new BassTagService();
            var info = tagService.GetTagInfo(PathToMp3);
            var track = new TrackData(info.ISRC, info.Artist, info.Title, info.Album, info.Year, info.Duration);
            var trackReference = modelService.InsertTrack(track);

            var hashedFingerprints = fingerprintCommandBuilder
                                            .BuildFingerprintCommand()
                                            .From(PathToMp3, SecondsToProcess, StartAtSecond)
                                            .UsingServices(bassAudioService)
                                            .Hash()
                                            .Result;

            modelService.InsertHashDataForTrack(hashedFingerprints, trackReference);

            var queryResult = queryFingerprintService.Query(hashedFingerprints, new DefaultQueryConfiguration(), modelService);

            Assert.IsTrue(queryResult.ContainsMatches);
            Assert.AreEqual(1, queryResult.ResultEntries.Count());
            Assert.AreEqual(trackReference, queryResult.BestMatch.Track.TrackReference);
        }

        [Test]
        public void CreateFingerprintsFromFileAndFromAudioSamplesAndGetTheSameResultTest()
        {
            const int SecondsToProcess = 20;
            const int StartAtSecond = 15;

            var samples = bassAudioService.ReadMonoSamplesFromFile(PathToMp3, SampleRate, SecondsToProcess, StartAtSecond);

            var hashDatasFromFile = fingerprintCommandBuilder
                                        .BuildFingerprintCommand()
                                        .From(PathToMp3, SecondsToProcess, StartAtSecond)
                                        .UsingServices(bassAudioService)
                                        .Hash()
                                        .Result;

            var hashDatasFromSamples = fingerprintCommandBuilder
                                        .BuildFingerprintCommand()
                                        .From(samples)
                                        .UsingServices(bassAudioService)
                                        .Hash()
                                        .Result;

            AssertHashDatasAreTheSame(hashDatasFromFile, hashDatasFromSamples);
        }

        [Test]
        public void CompareFingerprintsCreatedByDifferentProxiesTest()
        {
            var naudioFingerprints = ((FingerprintCommand)fingerprintCommandBuilder.BuildFingerprintCommand()
                                                        .From(PathToMp3)
                                                        .UsingServices(audioService))
                                                        .Fingerprint()
                                                        .Result;

            var bassFingerprints = ((FingerprintCommand)fingerprintCommandBuilder.BuildFingerprintCommand()
                                                 .From(PathToMp3)
                                                 .UsingServices(bassAudioService))
                                                 .Fingerprint()
                                                 .Result;
            int unmatchedItems = 0;
            int totalmatches = 0;

            Assert.AreEqual(bassFingerprints.Count, naudioFingerprints.Count);
            for (int i = 0; i < naudioFingerprints.Count; i++)
            {
                for (int j = 0; j < naudioFingerprints[i].Signature.Length; j++)
                {
                    if (naudioFingerprints[i].Signature[j] != bassFingerprints[i].Signature[j])
                    {
                        unmatchedItems++;
                    }

                    totalmatches++;
                }
            }

            Assert.AreEqual(true, (float)unmatchedItems / totalmatches < 0.04, "Rate: " + ((float)unmatchedItems / totalmatches));
            Assert.AreEqual(bassFingerprints.Count, naudioFingerprints.Count);
        }

        [Test]
        public void CheckFingerprintCreationAlgorithmTest()
        {
            string tempFile = Path.GetTempPath() + DateTime.Now.Ticks + ".wav";
            RecodeFileToWaveFile(tempFile);
            long fileSize = new FileInfo(tempFile).Length;

            var list = fingerprintCommandBuilder.BuildFingerprintCommand()
                                      .From(PathToMp3)
                                      .WithFingerprintConfig(customConfiguration => customConfiguration.Stride = new StaticStride(0))
                                      .UsingServices(bassAudioService)
                                      .Hash()
                                      .Result;

            long expected = fileSize / (8192 * 4); // One fingerprint corresponds to a granularity of 8192 samples which is 16384 bytes
            Assert.AreEqual(expected, list.Count);
            File.Delete(tempFile);
        }
        
        [Test]
        public void CreateFingerprintsWithTheSameFingerprintCommandTest()
        {
            const int SecondsToProcess = 20;
            const int StartAtSecond = 15;

            var fingerprintCommand = fingerprintCommandBuilder
                                            .BuildFingerprintCommand()
                                            .From(PathToMp3, SecondsToProcess, StartAtSecond)
                                            .UsingServices(bassAudioService);
            
            var firstHashDatas = fingerprintCommand.Hash().Result;
            var secondHashDatas = fingerprintCommand.Hash().Result;

            AssertHashDatasAreTheSame(firstHashDatas, secondHashDatas);
        }

        [Test]
        public void CreateFingerprintFromSamplesWhichAreExactlyEqualToMinimumLength()
        {
            var config = new DefaultFingerprintConfiguration();

            var samples = GenerateRandomAudioSamples(config.SamplesPerFingerprint + config.SpectrogramConfig.WdftSize);

            var hash = fingerprintCommandBuilder.BuildFingerprintCommand()
                                                .From(samples)
                                                .UsingServices(bassAudioService)
                                                .Hash()
                                                .Result;
            Assert.AreEqual(1, hash.Count);
        }

        private void RecodeFileToWaveFile(string tempFile)
        {
            var samples = bassAudioService.ReadMonoSamplesFromFile(PathToMp3, 5512);
            bassWaveFileUtility.WriteSamplesToFile(samples.Samples, 5512, tempFile);
        }

        private AudioSamples GenerateRandomAudioSamples(int length)
        {
            return new AudioSamples
            {
                Duration = length,
                Origin = string.Empty,
                SampleRate = 5512,
                Samples = GenerateRandomFloatArray(length)
            };
        }

        private float[] GenerateRandomFloatArray(int length)
        {
            float[] result = new float[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = (float)Rand.NextDouble() * 32767;
            }

            return result;
        }
    }
}
