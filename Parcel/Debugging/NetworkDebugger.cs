using System;
using System.Collections.Generic;

namespace Parcel.Debug
{

    /// <summary>
    /// Attaches to a <see cref="ParcelClient"/> or <see cref="ParcelServer"/> instance and tracks network statistics.
    /// </summary>
    public class NetworkDebugger : IDisposable
    {
        private const string EXCP_POP = "Failed to end frame. You ended more frames than you started";

        private Frame _globalFrame;
        private Stack<Frame> _frames = new Stack<Frame>();
        private Logger _logger;

        /// <summary>
        /// Construct a new instance of NetworkDebugger.
        /// </summary>
        /// <param name="logger">Optionally attach a <see cref="Logger"/> that will log select events such as exceptions and the end of a frame.</param>
        public NetworkDebugger(Logger logger = null)
        {
            this._logger = logger;
            this._globalFrame = new Frame("Global Frame", DateTime.Now);
        }

        public void Dispose()
        {
            DateTime now = DateTime.Now;
            this._logger?.WriteLine($"[{now.ToString("h:mm:ss.ff")}] End of Global Frame. Took {now - this._globalFrame.StartTime}ms.\n{this._globalFrame.ToString()}", Severity.Message);

            if (this._logger != null && this._logger is IDisposable disposable)
                disposable.Dispose();
        }

        /// <summary>
        /// Start a new Frame.
        /// </summary>
        /// <param name="name">The name of the frame.</param>
        /// <remarks>
        /// Frames will track events that occur within them until <see cref="EndFrame"/> is called.
        /// </remarks>
        public void StartFrame(string name)
        {
            DateTime time = DateTime.Now;
            this._frames.Push(new Frame(name, time));
        }

        /// <summary>
        /// End the current Frame.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if there is no current Frame.</exception>
        /// <remarks>
        /// When a Frame ends, if a <see cref="Logger"/> is attached to the NetworkDebugger instance, the Frame's statistics will be dumped to the log.
        /// </remarks>
        public void EndFrame()
        {
            DateTime time = DateTime.Now;
            if (!this._frames.TryPop(out Frame frame))
                throw new InvalidOperationException(EXCP_POP);
            int frameTime = (int)(time - frame.StartTime).TotalMilliseconds;

            this._logger?.WriteLine($"[{time.ToString("h:mm:ss.ff")}] End of Frame {frame.Name}. Took {frameTime}ms.\n{frame.ToString()}", Severity.Message);
        }

        /// <summary>
        /// Add an event indicating the serialization of a packet was completed.
        /// </summary>
        public void AddSerializedPacketEvent()
        {
            if (this._frames.TryPeek(out Frame currentFrame))
                currentFrame.PacketsSerialized++;

            this._globalFrame.PacketsSerialized++;
        }

        /// <summary>
        /// Add an event indicating a Packet was sent.
        /// </summary>
        /// <param name="packetSize">The size in bytes of the Packet.</param>
        public void AddSendPacketEvent(int packetSize)
        {
            if (this._frames.TryPeek(out Frame currentFrame))
            {
                currentFrame.PacketsSent++;
                currentFrame.BytesSent += packetSize;
            }

            this._globalFrame.PacketsSent++;
            this._globalFrame.BytesSent += packetSize;
        }

        /// <summary>
        /// Add an event indicating the deserialization of a packet was completed.
        /// </summary>
        public void AddDeserializedPacketEvent()
        {
            if (this._frames.TryPeek(out Frame currentFrame))
                currentFrame.PacketsDeserialized++;

            this._globalFrame.PacketsDeserialized++;
        }

        /// <summary>
        /// Add an event indicating a Packet was received.
        /// </summary>
        /// <param name="packetSize">The size in bytes of the Packet.</param>
        public void AddReceivePacketEvent(int packetSize)
        {
            if (this._frames.TryPeek(out Frame currentFrame))
            {
                currentFrame.PacketsReceived++;
                currentFrame.BytesReceived += packetSize;
            }

            this._globalFrame.PacketsReceived++;
            this._globalFrame.BytesReceived += packetSize;
        }

        /// <summary>
        /// Add an event indicating a Packet was resent.
        /// </summary>
        /// <param name="sequenceNumber">The sequence number of the Packet.</param>
        public void AddPacketResentEvent(int sequenceNumber)
        {
            if (this._frames.TryPeek(out Frame currentFrame))
                currentFrame.PacketsResent++;

            this._globalFrame.PacketsResent++;
            this._logger?.WriteLine($"Resent packet with sequence number {sequenceNumber}", Severity.Warning);
        }

        /// <summary>
        /// Add an event indicating one or more Packets were lost.
        /// </summary>
        /// <param name="lostCount">The number of Packets lost.</param>
        public void AddPacketLostEvent(int lostCount)
        {
            if (this._frames.TryPeek(out Frame currentFrame))
                currentFrame.PacketsLost++;

            this._globalFrame.PacketsLost++;
            this._logger?.WriteLine($"Lost {lostCount} packets!", Severity.Warning);
        }

        /// <summary>
        /// Add an event indicating a connection to a remote user.
        /// </summary>
        /// <param name="success">The success status of the event.</param>
        /// <param name="remoteToken">The <see cref="ConnectionToken"/> of the remote user.</param>
        public void AddConnectionEvent(bool success, ConnectionToken remoteToken)
        {
            DateTime time = DateTime.Now;
            this._logger?.WriteLine($"[{time.ToString("h:mm:ss.ff")}] {(success ? "Connected" : "Failed to Connect")} to {remoteToken.ToString()}.", Severity.Message);
        }

        /// <summary>
        /// Add an event indicating a disconnection from a remote user.
        /// </summary>
        public void AddDisconnectionEvent()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Add an event indicating an Exception occured.
        /// </summary>
        /// <param name="ex">The exception</param>
        /// <remarks>
        /// When an Exception event occurs, if a <see cref="Logger"/> is attached to the NetworkDebugger instance, the Exception will be dumped to the log.
        /// </remarks>
        public void AddExceptionEvent(Exception ex)
        {
            if (this._frames.TryPeek(out Frame currentFrame))
                currentFrame.ExceptionsCaught++;

            this._globalFrame.ExceptionsCaught++;
            this._logger?.WriteLine($"Encountered an exception\n{ex}", Severity.Error);
        }

        /// <summary>
        /// Write a message to the <see cref="Logger"/>, if it exists.
        /// </summary>
        /// <param name="message">The message to write.</param>
        public void WriteMessage(string message)
        {
            this._logger?.WriteLine(message, Severity.Message);
        }

        /// <summary>
        /// Holds various network statistics during a timeframe.
        /// </summary>
        private class Frame
        {
            /// <summary>
            /// The name of the Frame.
            /// </summary>
            public string Name { get; private set; }

            /// <summary>
            /// The time the Frame started.
            /// </summary>
            public DateTime StartTime { get; private set; }

            /// <summary>
            /// The time the Frame ended.
            /// </summary>
            public DateTime EndTime { get; set; }

            /// <summary>
            /// The number of Packets serialized.
            /// </summary>
            public int PacketsSerialized { get; set; }

            /// <summary>
            /// The number of Packets deserialized.
            /// </summary>
            public int PacketsDeserialized { get; set; }

            /// <summary>
            /// The number of Packets sent.
            /// </summary>
            public int PacketsSent { get; set; }
            
            /// <summary>
            /// The number of Packets received.
            /// </summary>
            public int PacketsReceived { get; set; }

            /// <summary>
            /// The number of bytes sent.
            /// </summary>
            public long BytesSent { get; set; }

            /// <summary>
            /// The number of bytes received.
            /// </summary>
            public long BytesReceived { get; set; }

            /// <summary>
            /// The average compression ratio.
            /// </summary>
            public float AverageCompressionRatio { get; set; }

            /// <summary>
            /// The number of Packets resent.
            /// </summary>
            public int PacketsResent { get; set; }

            /// <summary>
            /// The number of Packets lost.
            /// </summary>
            public int PacketsLost { get; set; }

            /// <summary>
            /// The number of Exceptions caught.
            /// </summary>
            public int ExceptionsCaught { get; set; }

            /// <summary>
            /// Construct a new instance of Frame.
            /// </summary>
            /// <param name="name">The name of the Frame.</param>
            /// <param name="startTime">The time at which the Frame started.</param>
            public Frame(string name, DateTime startTime)
            {
                this.Name = name;
                this.StartTime = startTime;
            }

            public override string ToString()
            {
                return $"==================== {Name} ====================\n" +
                       $"Packets Serialized: {PacketsSerialized}\n" +
                       $"Packets Deserialized: {PacketsDeserialized}\n" +
                       $"Packets Sent: {PacketsSent}\n" +
                       $"Packets Received: {PacketsReceived}\n" +
                       $"Bytes Sent: {BytesSent}\n" +
                       $"Bytes Received: {BytesReceived}\n" +
                       $"Average Compression Ratio: {AverageCompressionRatio}\n" +
                       $"Packets Resent: {PacketsResent}\n" +
                       $"Packets List: {PacketsLost}\n" +
                       $"Exceptions Caught: {ExceptionsCaught}\n" +
                       $"====================={new string('=', Name.Length)}=====================";
            }

        }

        private class TimedRollingSum
        {
            private List<(long, int)> _list = new List<(long, int)>();
            private int _total = 0;
            private int _ttl;

            public int Total
            {
                get
                {
                    long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    List<(long, int)> list = new List<(long, int)>();

                    foreach (var item in _list)
                    {
                        if (now - item.Item1 > _ttl)
                            list.Add(item);
                    }

                    foreach (var item in list)
                    {
                        _total -= item.Item2;
                        _list.Remove(item);
                    }

                    return _total;
                }
            }

            public TimedRollingSum(int ttl)
            {
                _ttl = ttl;
            }

            public void Add(int value)
            {
                long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                _list.Add((now, value));
                _total += value;
            }

        }
    }
}
