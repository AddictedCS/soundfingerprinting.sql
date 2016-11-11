namespace SoundFingerprinting.SQL
{
    using SoundFingerprinting.DAO;

    public class SqlModelService : AdvancedModelService
    {
        public SqlModelService() : base(new TrackDao(), new SubFingerprintDao(), new FingerprintDao(), new SpectralImageDao())
        {
            // no op
        }

        protected SqlModelService(
            ITrackDao trackDao,
            ISubFingerprintDao subFingerprintDao,
            IFingerprintDao fingerprintDao,
            ISpectralImageDao spectralImageDao)
            : base(trackDao, subFingerprintDao, fingerprintDao, spectralImageDao)
        {
            // no op
        }
    }
}
