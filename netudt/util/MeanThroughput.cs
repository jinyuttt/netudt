


/**
 * holds a floating mean value
 */
namespace netudt.util
{
    public class MeanThroughput : MeanValue
    {

        private double packetSize;

        //public MeanThroughput(string name, int packetSize)
        //{
        //    this(name, false, 64, packetSize);
        //}

        //public MeanThroughput(string name, bool verbose, int packetSize)
        //{
        //    this(name, verbose, 64, packetSize);
        //}

        public MeanThroughput(string name, int packetSize = 64, bool verbose=false,int nValue = 64) :base(name,verbose,nValue)
        {
          
            this.packetSize = packetSize;
        }


        public new double GetMean()
        {
            return packetSize / GetMean();
        }

    }
}
