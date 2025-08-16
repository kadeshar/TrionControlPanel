namespace TrionControlPanel.API.Classes
{
    /// <summary>
    /// A custom stream that generates a large amount of random data on-the-fly
    /// without storing it all in memory. This is ideal for speed tests.
    /// </summary>
    public class OnTheFlyRandomStream : Stream
    {
        private long _position;
        private readonly long _length;
        private static readonly Random _random = new Random();
        private const int BufferSize = 81920; // 80 KB buffer for efficiency

        /// <summary>
        /// Initializes a new instance of the OnTheFlyRandomStream class.
        /// </summary>
        /// <param name="length">The total length of the stream in bytes. Default is 100 MB.</param>
        public OnTheFlyRandomStream(long length = 100 * 1024 * 1024)
        {
            _length = length;
            _position = 0;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _length;

        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException("This stream cannot be sought.");
        }

        /// <summary>
        /// This is the core method. It's called by the web server as it sends data to the client.
        /// It generates random bytes directly into the output buffer.
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_position >= _length)
            {
                return 0; // End of stream
            }

            // Determine how many bytes to read in this call
            int bytesToRead = (int)Math.Min(count, _length - _position);

            // Create a temporary buffer of random bytes
            var randomBytes = new byte[bytesToRead];
            _random.NextBytes(randomBytes);

            // Copy the newly generated random bytes to the output buffer
            Buffer.BlockCopy(randomBytes, 0, buffer, offset, bytesToRead);

            _position += bytesToRead;

            return bytesToRead;
        }

        // The following methods are not needed for a read-only stream but must be overridden.
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    /// <summary>
    /// Network utility class.
    /// </summary>
    public static class Network
    {
        /// <summary>
        /// Generates an efficient, memory-light stream of random data for speed testing.
        /// </summary>
        /// <param name="totalSizeInMB">The total size of the speed test file to generate.</param>
        /// <returns>A read-only stream that generates random data on the fly.</returns>
        public static Stream GenerateRandomStream(long totalSizeInMB = 100)
        {
            long streamLength = totalSizeInMB * 1024 * 1024;
            return new OnTheFlyRandomStream(streamLength);
        }
    }
}