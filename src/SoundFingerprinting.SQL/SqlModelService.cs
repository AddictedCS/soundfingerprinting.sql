namespace SoundFingerprinting.SQL
{
    using SoundFingerprinting.DAO;

    public class SqlModelService : ModelService
    {
        public SqlModelService() : base(new TrackDao(), new SubFingerprintDao())
        {
            // no op
        }

        protected SqlModelService(ITrackDao trackDao, ISubFingerprintDao subFingerprintDao)
            : base(trackDao, subFingerprintDao)
        {
            // no op
        }

        public override bool SupportsBatchedSubFingerprintQuery
        {
            get
            {
                return false;
            }
        }
    }
}
