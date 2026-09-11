using System;
using System.IO;

namespace OpenRigs.Tests
{
    /// <summary>
    /// Reads raw Harp binary logs of the kind written by MatrixWriter.
    /// </summary>
    public static class HarpBinaryLog
    {
        // A Harp frame is [MessageType][Length][Address][Port][PayloadType]...[Checksum], where the
        // length byte counts everything after itself. The frame therefore occupies Length + 2 bytes.
        const int LengthOffset = 1;
        const int LengthOverhead = 2;

        /// <summary>
        /// Counts the messages in a Harp binary log. Messages are variable length, so the log is
        /// walked frame by frame rather than dividing by a fixed record size.
        /// </summary>
        /// <exception cref="InvalidDataException">
        /// The log does not divide cleanly into frames, which means it is truncated or corrupt.
        /// </exception>
        public static int CountMessages(string fileName)
        {
            var data = File.ReadAllBytes(fileName);
            var count = 0;
            var offset = 0;
            while (offset < data.Length)
            {
                if (offset + LengthOffset >= data.Length)
                {
                    throw new InvalidDataException(
                        $"'{fileName}' ends with a partial Harp frame header at offset {offset}.");
                }

                var frameLength = data[offset + LengthOffset] + LengthOverhead;
                if (offset + frameLength > data.Length)
                {
                    throw new InvalidDataException(
                        $"'{fileName}' declares a {frameLength} byte frame at offset {offset}, " +
                        $"but only {data.Length - offset} bytes remain.");
                }

                offset += frameLength;
                count++;
            }

            return count;
        }
    }
}
