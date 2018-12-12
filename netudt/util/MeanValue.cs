


using System;
using System.Globalization;
/**
* holds a floating mean timing value (measured in microseconds)
*/
namespace netudt.util
{
    public class MeanValue {

        private double mean = 0;

        private int n = 0;

        private NumberFormatInfo format;

        private bool verbose;

        private long nValue;
        private long start;

        private string msg;

        private string name;

       

        public MeanValue(string name, bool verbose=false, int nValue=64) {
            format = NumberFormatInfo.CurrentInfo;
            format.NumberDecimalDigits = 2;
          //  format.
           // format.setMaximumFractionDigits(2);
           // format.setGroupingUsed(false);
            this.verbose = verbose;
            this.nValue = nValue;
            this.name = name;
        }

        public void AddValue(double value) {
            mean = (mean * n + value) / (n + 1);
            n++;
            if (verbose && n % nValue == 0) {
                if (msg != null) Console.WriteLine(msg + " " + GetFormattedMean());
                else Console.WriteLine(name + GetFormattedMean());
            }
        }

        public double GetMean() {
            return mean;
        }

        public string GetFormattedMean() {
            return GetMean().ToString(format);
        }

        public void Clear() {
            mean = 0;
            n = 0;
        }

        public void Begin() {
            start = Util.getCurrentTime();
        }

        public void End() {
            if (start > 0) AddValue(Util.getCurrentTime() - start);
        }
        public void End(string msg) {
            this.msg = msg;
            AddValue(Util.getCurrentTime() - start);
        }

        public String GetName() {
            return name;
        }


        public String toString() {
            return name;
        }
    }
}
