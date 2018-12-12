
using System.Threading;

namespace netudt.util
{
    public class UDTThreadFactory {

        private static volatile int num = 0;

        public static readonly UDTThreadFactory theInstance = new UDTThreadFactory();


        public static UDTThreadFactory Instance
        {
          get { 
            return theInstance;}
        }


        public string NewThreadName() {
           
            return ("UDT-Thread-" + Interlocked.Increment(ref num));
           
        }
    }
}
