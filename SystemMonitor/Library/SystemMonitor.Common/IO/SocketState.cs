using System.Net;
using System.Net.Sockets;

namespace SystemMonitor.Common.IO
{
    public struct SocketState
    {
        private const int MaxBufferSize = 64 * 1024 * 1024;

        public int BufferSize;
        public byte[] Buffer;
        public Socket Socket;
        public EndPoint RemoteEndPoint;

        public SocketState(Socket socket, int bufferSize, EndPoint remoteEndPoint)
        {
            if (bufferSize > MaxBufferSize)
                throw new ArgumentOutOfRangeException(nameof(bufferSize), $"Maximum buffer size is {MaxBufferSize} bytes");
            BufferSize = bufferSize;
            Socket = socket;
            RemoteEndPoint = remoteEndPoint;
            Buffer = new byte[bufferSize];
        }

        public override bool Equals(object obj)
            => obj is SocketState socket && Socket.Equals(socket);

        public override int GetHashCode()
            => Socket.GetHashCode();

        public static bool operator ==(SocketState left, SocketState right) => left.Equals(right);

        public static bool operator !=(SocketState left, SocketState right) => !(left == right);
    }
}
