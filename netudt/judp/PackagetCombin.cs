using netudt.util;
using System;
using System.Collections.Concurrent;
using System.IO;

namespace netudt.judp
{ 
public class PackagetCombin {
	private  static readonly ConcurrentDictionary<long,DataStruct> hash=new ConcurrentDictionary<long,DataStruct>();
	private readonly ConcurrentQueue<byte[]> queue=new ConcurrentQueue<byte[]>();

        /**
         * 添加数据
         * @param data
         * @return
         */
        public bool AddData(byte[] data)
        {
            MemoryStream buf = new MemoryStream(data);
            byte[] iduf = new byte[8];
            byte[] numbuf = new byte[4];
            buf.Read(iduf, 0, 8);
            buf.Read(numbuf, 0, 4);
            long id = BitConverter.ToInt64(iduf, 0);

            int num = BitConverter.ToInt32(numbuf, 0);

            DataStruct dstruct = null;
            if (!hash.TryGetValue(id,out dstruct))
            {
                dstruct = new DataStruct(num);
                hash.TryAdd(id, dstruct);
            }

            bool r = dstruct.AddData(data);
            if (r)
            {
                byte[] result = dstruct.GetData();
                byte[] tmp = new byte[result.Length];
                Array.Copy(result, 0, tmp, 0, tmp.Length);
                queue.Enqueue(tmp);
                dstruct.clear();
                hash.TryRemove(id, out dstruct);
            }
            return r;

        }

        /**
         * 获取数据
         * @return
         */
        public byte[] GetData()
        {
            byte[] item = null;
            queue.TryDequeue(out item);

            return item;
        }
}
}
