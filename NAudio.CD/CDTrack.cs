namespace NAudio.CD
{
    public class CDTrack
    {
        private readonly uint startSector;
        private readonly uint endSector;

        public CDTrack(uint startSector, uint endSector)
        {
            this.startSector = startSector;
            this.endSector = endSector;
        }

        internal uint StartSector
        {
            get
            {
                return this.startSector;
            }
        }

        internal uint EndSector
        {
            get
            {
                return this.endSector;
            }
        }
    }
}
