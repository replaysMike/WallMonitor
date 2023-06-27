using System.Runtime.CompilerServices;

namespace WallMonitor.Common.IO
{
    /// <summary>
    /// A simple circular (ring) buffer. Also called a coder buffer
    /// </summary>
    /// <remarks>
    /// This class is designed to use aggressive inlining of frequently used properties and methods for performance reasons.
    /// Also, the internal buffer uses a double locked array (via volatile + locking) for thread safety.
    /// Eventually, this could be upgraded to a lock-free implementation using memory barriers and ranges.
    /// </remarks>
    public class RingBuffer : IDisposable
    {
        private volatile byte[] _buffer;
        private readonly ReaderWriterLockSlim _lock = new();
        private readonly bool _overwriteThrowsException;
        private int _nextWriteIndex;
        private int _nextReadIndex = -1;
        private bool _isDisposed;

        /// <summary>
        /// Get the capacity length of the ring buffer
        /// </summary>
        public int Capacity => _buffer.Length;

        /// <summary>
        /// Get the number of bytes available to read
        /// </summary>
        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return GetReadLengthInternal();
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Get the number of bytes left that can be written to the buffer
        /// </summary>
        public int Available
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _buffer.Length - GetReadLengthInternal();
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Get the percent full of the buffer
        /// </summary>
        public double PercentFull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                _lock.EnterReadLock();
                try
                {
                    var length = GetReadLengthInternal();
                    if (length >= _buffer.Length)
                        return 1.0;
                    if (_buffer.Length == 0)
                        return 0;
                    return length / (double)_buffer.Length;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Create a new Ring Buffer of a specified capacity of bytes
        /// </summary>
        /// <param name="capacity">The capacity length of bytes that can be stored</param>
        public RingBuffer(int capacity) : this(capacity, true)
        {
        }

        /// <summary>
        /// Create a new Ring Buffer of a specified capacity of bytes
        /// </summary>
        /// <param name="capacity">The capacity length of bytes that can be stored</param>
        /// <param name="overwriteThrowsException">True if an exception should be thrown when unread data is overwritten</param>
        public RingBuffer(int capacity, bool overwriteThrowsException)
        {
            _buffer = new byte[capacity];
            _overwriteThrowsException = overwriteThrowsException;
        }

        /// <summary>
        /// Return all of the bytes available to be read
        /// </summary>
        /// <returns></returns>
        public byte[] ReadAllBytes()
        {
            byte[] buffer;
            _lock.EnterReadLock();
            try
            {
                buffer = new byte[GetReadLengthInternal()];
                ReadBytesInternal(buffer, 0, buffer.Length);
            }
            finally
            {
                _lock.ExitReadLock();
            }
            return buffer;
        }

        /// <summary>
        /// Read bytes available to be read into a buffer
        /// </summary>
        /// <param name="buffer"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadBytes(byte[] buffer)
        {
            _lock.EnterReadLock();
            try
            {
                ReadBytesInternal(buffer, 0, buffer.Length);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Read bytes available to be read into a buffer
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="sourcePosition">The start position of the buffer to read into</param>
        /// <param name="length">The length of bytes to read</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadBytes(byte[] buffer, int sourcePosition)
        {
            _lock.EnterReadLock();
            try
            {
                ReadBytesInternal(buffer, sourcePosition, GetReadLengthInternal());
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Read bytes available to be read into a buffer
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="sourcePosition">The start position of the buffer to read into</param>
        /// <param name="length">The length of bytes to read</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadBytes(byte[] buffer, int sourcePosition, int length)
        {
            _lock.EnterReadLock();
            try
            {
                ReadBytesInternal(buffer, sourcePosition, length);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReadBytesInternal(byte[] buffer, int sourcePosition, int length)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(RingBuffer));
            // read all of the data available
            if (_nextReadIndex == -1)
                return;
            var availableLength = GetReadLengthInternal();
            if (length > availableLength)
                throw new ArgumentOutOfRangeException(nameof(length), $"Parameter exceeds available buffer length");
            if (sourcePosition + length > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(sourcePosition), $"Parameters exceed the length available in the buffer provided");
            if (_nextReadIndex + length > _buffer.Length)
            {
                // copy from the end of the buffer
                var readableLength = _buffer.Length - _nextReadIndex;
                Buffer.BlockCopy(_buffer, _nextReadIndex, buffer, sourcePosition, readableLength);
                _nextReadIndex = 0;
                // copy from the start of the buffer
                var remainingLength = _nextWriteIndex;
                Buffer.BlockCopy(_buffer, _nextReadIndex, buffer, sourcePosition + readableLength, remainingLength);
                _nextReadIndex += remainingLength;
            }
            else
            {
                Buffer.BlockCopy(_buffer, _nextReadIndex, buffer, 0, length);
                _nextReadIndex += length;
            }
            if (availableLength == length)
                _nextReadIndex = _nextWriteIndex;
        }

        /// <summary>
        /// Add bytes to the ring buffer
        /// </summary>
        /// <param name="bytes"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBytes(byte[] bytes)
        {
            _lock.EnterWriteLock();
            try
            {
                WriteBytesInternal(bytes, 0, bytes.Length);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Add bytes to the ring buffer
        /// </summary>
        /// <param name="bytes"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBytes(ArraySegment<byte> bytes)
        {
            _lock.EnterWriteLock();
            try
            {
                WriteBytesInternal(bytes.Array, bytes.Offset, bytes.Count);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Add bytes to the ring buffer
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="sourcePosition"></param>
        /// <param name="length"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBytes(byte[] bytes, int sourcePosition, int length)
        {
            _lock.EnterWriteLock();
            try
            {
                WriteBytesInternal(bytes, sourcePosition, length);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteBytesInternal(byte[] bytes, int sourcePosition, int length)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(RingBuffer));
            if (sourcePosition > bytes.Length)
                throw new ArgumentOutOfRangeException(nameof(sourcePosition), $"Argument is out of bounds.");
            if (sourcePosition + length > bytes.Length)
                throw new ArgumentOutOfRangeException(nameof(length), $"Argument is out of bounds.");
            var bytesAvailable = 0;

            if (_nextWriteIndex + length > _buffer.Length)
            {
                // not enough space to store all data in buffer, write what we can and loop to the start
                // of the buffer, overwriting old data.
                var currentWritableLength = _buffer.Length - _nextWriteIndex;
                var beginWritableLength = length - currentWritableLength;

                // if there is data in the buffer that has not been read yet, throw exception if configured to do so.
                // otherwise we will overwrite it and that data is lost.
                bytesAvailable = _buffer.Length - GetReadLengthInternal();
                if (_overwriteThrowsException && bytesAvailable < currentWritableLength + beginWritableLength)
                    throw new OverflowException($"Buffer is full and cannot be overwritten!");

                // copy what we can to the end of the buffer
                Buffer.BlockCopy(bytes, sourcePosition, _buffer, _nextWriteIndex, currentWritableLength);
                // copy the remaining data to the beginning of the buffer
                Buffer.BlockCopy(bytes, sourcePosition + currentWritableLength, _buffer, 0, beginWritableLength);
                _nextWriteIndex = beginWritableLength;
                if (_nextReadIndex == -1)
                    _nextReadIndex = 0;
                return;
            }

            // write all data into the buffer

            // if there is data in the buffer that has not been read yet, throw exception if configured to do so.
            // otherwise we will overwrite it and that data is lost.
            bytesAvailable = _buffer.Length - GetReadLengthInternal();
            if (_overwriteThrowsException && bytesAvailable < length)
                throw new OverflowException($"Buffer is full and cannot be overwritten!");

            // copy the full byte array into the buffer
            Buffer.BlockCopy(bytes, sourcePosition, _buffer, _nextWriteIndex, length);
            _nextWriteIndex += length;
            if (_nextReadIndex == -1)
                _nextReadIndex = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetReadLengthInternal()
        {
            if (_nextReadIndex == -1)
                return 0;
            // get the number of bytes that can be read in the buffer
            if (_nextReadIndex > _nextWriteIndex)
            {
                // buffer has looped. Calculate the length at the end of the buffer, plus data at the beginning
                return (_buffer.Length - _nextReadIndex) + _nextWriteIndex;
            }

            // all data is inside the buffer
            return _nextWriteIndex - _nextReadIndex;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (_isDisposed)
                return;
            if (isDisposing)
            {
                _isDisposed = true;
                _lock.EnterWriteLock();
                try
                {
                    _buffer = null;
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
                _lock?.Dispose();
            }
        }

    }
}
