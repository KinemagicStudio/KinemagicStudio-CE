using System;
using System.Diagnostics;
using System.Threading;
using uOSC;
using Debug = UnityEngine.Debug;

namespace Kinemagic.Apps.Studio.FeatureCore.MotionDataSource.Infrastructure
{
    public sealed class OscServer : IDisposable
    {
        private static readonly double TimestampsToTicks = TimeSpan.TicksPerSecond / (double) Stopwatch.Frequency;

        private readonly System.Threading.Thread _updateLoopThread;
        private readonly CancellationTokenSource _updateLoopCts;
        private readonly int _targetFrameTimeMilliseconds;

        private int _port;
        private bool _isStarted;
        private uOSC.Udp _udp = new uOSC.DotNet.Udp();
        private uOSC.Thread _oscThread = new uOSC.DotNet.Thread();
        private Parser _parser = new Parser();

        public DataReceiveEvent OnDataReceived = new DataReceiveEvent();
        public ServerStartEvent OnServerStarted = new ServerStartEvent();
        public ServerStopEvent OnServerStopped = new ServerStopEvent();

        public int Port => _port;
        public bool IsRunning => _udp.isRunning;

        public OscServer(int port, int targetFrameRate, bool isBackground = true)
        {
            _port = port;

            _targetFrameTimeMilliseconds = (int)(1000 / (double)targetFrameRate);
            _updateLoopCts = new CancellationTokenSource();
            _updateLoopThread = new System.Threading.Thread(UpdateLoop)
            {
                Name = $"{typeof(OscServer).Name}",
                IsBackground = isBackground,
            };

            _updateLoopThread.Start();
        }

        public void Dispose()
        {
            _updateLoopCts.Cancel();
            _updateLoopCts.Dispose();
            Stop();
        }

        public void Start()
        {
            if (_isStarted) return;

            _udp.StartServer(_port);
            _oscThread.Start(UpdateMessage);

            _isStarted = true;

            OnServerStarted.Invoke(_port);
        }

        public void Stop()
        {
            if (!_isStarted) return;

            _oscThread.Stop();
            _udp.Stop();

            _isStarted = false;

            OnServerStopped.Invoke(_port);
        }

        private void UpdateLoop()
        {
            while (!_updateLoopCts.IsCancellationRequested)
            {
                var begin = Stopwatch.GetTimestamp();
                
                try
                {
                    UpdateReceive();
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
                
                var end = Stopwatch.GetTimestamp();
                var elapsedTicks = (end - begin) * TimestampsToTicks;
                var elapsedMilliseconds = (long)elapsedTicks / TimeSpan.TicksPerMillisecond;

                var waitForNextFrameMilliseconds = (int)(_targetFrameTimeMilliseconds - elapsedMilliseconds);
                if (waitForNextFrameMilliseconds > 0)
                {
                    System.Threading.Thread.Sleep(waitForNextFrameMilliseconds);
                }
            }
        }

        private void UpdateReceive()
        {
            while (_parser.messageCount > 0)
            {
                var message = _parser.Dequeue();
                OnDataReceived.Invoke(message);
            }
        }

        private void UpdateMessage()
        {
            while (_udp.messageCount > 0) 
            {
                var buf = _udp.Receive();
                int pos = 0;
                _parser.Parse(buf, ref pos, buf.Length);
            }
        }
    }
}
