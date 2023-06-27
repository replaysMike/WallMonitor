using System.Net.Sockets;
using System.Threading.Tasks.Sources;

namespace WallMonitor.Common.IO.Server.Transport
{
    public class AwaitableEventArgs : SocketAsyncEventArgs, IValueTaskSource<int>
    {
        private ManualResetValueTaskSourceCore<int> _source = new ();

        public AwaitableEventArgs() :
            base(unsafeSuppressExecutionContextFlow: true)
        {
        }

        protected override void OnCompleted(SocketAsyncEventArgs args)
        {
            if (SocketError != SocketError.Success)
            {
                _source.SetException(new SocketException((int)SocketError));
            }

            try
            {
                _source.SetResult(BytesTransferred);
            }
            catch (InvalidOperationException)
            {
                // client disconnected
            }
        }

        public int GetResult(short token)
        {
            try
            {
                var result = _source.GetResult(token);
                _source.Reset();
                return result;
            }
            catch (SocketException ex)
            {
                if (ex.Message.Contains("An existing connection was forcibly closed by the remote host", StringComparison.InvariantCultureIgnoreCase))
                {
                    // ignore, socket was closed
                    return -1;
                }
                else
                {
                    throw;
                }
            }
        }

        public ValueTaskSourceStatus GetStatus(short token)
        {
            return _source.GetStatus(token);
        }

        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            _source.OnCompleted(continuation, state, token, flags);
        }
    }
}
