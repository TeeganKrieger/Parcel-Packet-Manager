using System;
using System.IO;

namespace Parcel.Debug
{

    /// <summary>
    /// Writes Logger messages to a file.
    /// </summary>
    public sealed class FileLogger : Logger, IDisposable
    {
        private StreamWriter _filestream;
        private Severity _lastSeverity;

        /// <summary>
        /// Construct a new instance of FileLogger.
        /// </summary>
        /// <param name="filePath">The path to the file to log to.</param>
        /// <param name="writeMessages">Should this Logger write messages.</param>
        /// <param name="writeWarnings">Should this Logger write warnings.</param>
        /// <param name="writeErrors">Should this logger write errors.</param>
        public FileLogger(string filePath, bool writeMessages = true, bool writeWarnings = true, bool writeErrors = true)
        {
            this.WriteMessages = writeMessages;
            this.WriteWarnings = writeWarnings;
            this.WriteErrors = writeErrors;

            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            
            File.Delete(filePath);
            File.Create(filePath).Close();

            this._filestream = new StreamWriter(File.OpenWrite(filePath));
        }

        public void Dispose()
        {
            this._filestream.Close();
        }

        /// <inheritdoc/>
        public override void Write(string message, Severity severity)
        {
            if (this._lastSeverity != severity)
                this._filestream.Write($"[{severity.ToString()}] {message}");
            else
                this._filestream.Write(message);

            this._lastSeverity = severity;
        }

        /// <inheritdoc/>
        public override void WriteLine(string message, Severity severity)
        {
            if ((severity == Severity.Message && WriteMessages) || (severity == Severity.Warning && WriteWarnings) || (severity == Severity.Error && WriteErrors))
                this._filestream.WriteLine($"[{severity.ToString()}] {message}");
        }
    }
}
