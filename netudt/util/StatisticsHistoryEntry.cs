using System;
using System.Collections.Generic;
using System.Text;

namespace netudt.util
{

    public class StatisticsHistoryEntry {

        private object[] values;

        private long timestamp;

        private bool isHeading;

        public StatisticsHistoryEntry(long time, bool heading=false,params object[] values) {
            this.values = values;
            this.isHeading = heading;
            this.timestamp = time;
        }

        public StatisticsHistoryEntry(bool heading, long time, List<MeanValue> metrics) {
            this.isHeading = heading;
            this.timestamp = time;
            int length = metrics.Count;
            if (isHeading) length++;
            Object[] metricValues = new Object[length];
            if (isHeading) {
                metricValues[0] = "time";
                for (int i = 0; i < metrics.Count; i++) {
                    metricValues[i + 1] = metrics[i].GetName();
                }
            }
            else {
                for (int i = 0; i < metricValues.Length; i++) {
                    metricValues[i] = metrics[i].GetFormattedMean();
                }
            }
            this.values = metricValues;
        }

       

        /**
         * output as comma separated list
         */

        public string toString() {
            StringBuilder sb = new StringBuilder();
            if (!isHeading) {
                sb.Append(timestamp);
                foreach (Object val in values) {
                    sb.Append(" , ").Append(val.ToString());
                }
            }
            else {
                for (int i = 0; i < values.Length; i++) {
                    if (i > 0) sb.Append(" , ");
                    sb.Append(values[i].ToString());
                }
            }
            return sb.ToString();
        }
    }
}
