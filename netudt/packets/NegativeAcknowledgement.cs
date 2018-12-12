




using System;
using System.Collections.Generic;
using System.IO;
/**
* NAK carries information about lost packets
* 
* loss info is described in the spec on p.15
*/
namespace netudt.packets
{
    public class NegativeAcknowledgement : ControlPacket
    {

        //after decoding this contains the lost sequence numbers
        List<int> lostSequenceNumbers;

        //this contains the loss information intervals as described on p.15 of the spec
        MemoryStream lossInfo = new MemoryStream();



        public NegativeAcknowledgement(byte[] controlInformation = null)
        {
            this.controlPacketType = (int)ControlPacketType.NAK;
            if (controlInformation != null)
            {
                lostSequenceNumbers = decode(controlInformation);
            }
        }

        /**
         * decode the loss info
         * @param lossInfo
         */
        private List<int> decode(byte[] lossInfo)
        {
            List<int> lostSequenceNumbers = new List<int>();
            // ByteBuffer bb = ByteBuffer.wrap(lossInfo);
            byte[] buffer = new byte[4];
            int remaining = lossInfo.Length;
            int index = 0;
            while (index < lossInfo.Length)
            {
                //read 4 bytes
                buffer[0] = lossInfo[index++];
                buffer[1] = lossInfo[index++];
                buffer[2] = lossInfo[index++];
                buffer[3] = lossInfo[index++];
                bool isNotSingle = (buffer[0] & 128) != 0;
                //set highest bit back to 0
                buffer[0] = (byte)(buffer[0] & 0x7f);
                int lost = BitConverter.ToInt32(buffer, 0);
                if (isNotSingle)
                {
                    //get the end of the interval
                    int end = BitConverter.ToInt32(lossInfo, index);
                    //and add all lost numbers to the result list
                    for (int i = lost; i <= end; i++)
                    {
                        lostSequenceNumbers.Add(i);
                    }
                }
                else
                {
                    lostSequenceNumbers.Add(lost);
                }
            }
            return lostSequenceNumbers;
        }

        /**
         * add a single lost packet number
         * @param singleSequenceNumber
         */
        public void AddLossInfo(long singleSequenceNumber)
        {
            byte[] enc = PacketUtil.EncodeSetHighest(false, singleSequenceNumber);
            try
            {
                lossInfo.Write(enc, 0, enc.Length);
            }
            catch (IOException ignore) { }
        }

        /**
         * add an interval of lost packet numbers
         * @param firstSequenceNumber
         * @param lastSequenceNumber
         */
        public void AddLossInfo(long firstSequenceNumber, long lastSequenceNumber)
        {
            //check if we really need an interval
            if (lastSequenceNumber - firstSequenceNumber == 0)
            {
                AddLossInfo(firstSequenceNumber);
                return;
            }
            //else add an interval
            byte[] enc1 = PacketUtil.EncodeSetHighest(true, firstSequenceNumber);
            byte[] enc2 = PacketUtil.EncodeSetHighest(false, lastSequenceNumber);
            try
            {
                lossInfo.Write(enc1, 0, enc1.Length);
                lossInfo.Write(enc2, 0, enc2.Length);
            }
            catch (IOException ignore) { }
        }

        /**
         * pack the given list of sequence numbers and add them to the loss info
         * @param sequenceNumbers - a list of sequence numbers
         */
        public void AddLossInfo(List<long> sequenceNumbers)
        {
            long start = 0;
            int index = 0;
            do
            {
                start = sequenceNumbers[index];
                long end = 0;
                int c = 0;
                do
                {
                    c++;
                    index++;
                    if (index < sequenceNumbers.Count)
                    {
                        end = sequenceNumbers[index];
                    }
                } while (end - start == c);
                if (end == 0)
                {
                    AddLossInfo(start);
                }
                else
                {
                    end = sequenceNumbers[index - 1];
                    AddLossInfo(start, end);
                }
            } while (index < sequenceNumbers.Count);
        }

        /**
         * Return the lost packet numbers
         * @return
         */
        public List<int> GetDecodedLossInfo()
        {
            return lostSequenceNumbers;
        }


        public override byte[] EncodeControlInformation()
        {
            try
            {
                return lossInfo.ToArray();
            }
            catch (Exception e)
            {
                // can't happen
                return null;
            }
        }


        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (!this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            NegativeAcknowledgement other = (NegativeAcknowledgement)obj;

            List<int> thisLost = null;
            List<int> otherLost = null;

            //compare the loss info
            if (lostSequenceNumbers != null)
            {
                thisLost = lostSequenceNumbers;
            }
            else
            {
                thisLost = decode(lossInfo.ToArray());
            }
            if (other.lostSequenceNumbers != null)
            {
                otherLost = other.lostSequenceNumbers;
            }
            else
            {
                otherLost = other.decode(other.lossInfo.ToArray());
            }
            if (!thisLost.Equals(otherLost))
            {
                return false;
            }

            return true;
        }
    }

}
