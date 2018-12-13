

using System.Collections.Generic;

namespace netudt.util
{
    public abstract class Application  {

        protected static bool verbose;

        protected static string localIP = null;

        protected static int localPort = -1;

        public virtual void Configure() {
            //if (verbose) {
            //    Logger.getLogger("udt").setLevel(Level.INFO);
            //}
            //else {
            //    Logger.getLogger("udt").setLevel(Level.OFF);
            //}
        }


        internal static string[] ParseOptions(string[] args) {
            List<string> newArgs = new List<string>();
            foreach (string  arg in args) {
                if (arg.StartsWith("-")) {
                    ParseArg(arg);
                }
                else
                {
                    newArgs.Add(arg);
                }
            }
            return newArgs.ToArray();
        }


        internal static void ParseArg(string arg) {
            if ("-v".Equals(arg) || "--verbose".Equals(arg)) {
                verbose = true;
                return;
            }
            if (arg.StartsWith("--localIP")) {
                localIP = arg.Split('=')[1];
            }
            if (arg.StartsWith("--localPort")) {
                localPort = int.Parse(arg.Split('=')[1]);
            }
        }




     internal   static long Decode(byte[] data, int start) {
            long result = (data[start + 3] & 0xFF) << 24
                         | (data[start + 2] & 0xFF) << 16
                         | (data[start + 1] & 0xFF) << 8
                         | (data[start] & 0xFF);
            return result;
        }

      internal  static byte[] Encode(long value) {
            byte m4 = (byte)(value >> 24);
            byte m3 = (byte)(value >> 16);
            byte m2 = (byte)(value >> 8);
            byte m1 = (byte)(value);
            return new byte[] { m1, m2, m3, m4 };
        }

      internal  static byte[] Encode64(long value) {
            byte m4 = (byte)(value >> 24);
            byte m3 = (byte)(value >> 16);
            byte m2 = (byte)(value >> 8);
            byte m1 = (byte)(value);
            return new byte[] { m1, m2, m3, m4, 0, 0, 0, 0 };
        }
    }
}
