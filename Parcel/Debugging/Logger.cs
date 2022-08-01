using System;
using System.Collections.Generic;
using System.Text;

namespace Parcel.Debug
{

    /// <summary>
    /// The severity of a Logger message.
    /// </summary>
    public enum Severity
    {
        /// <summary>
        /// Indicates an ordinary message.
        /// </summary>
        Message,
        /// <summary>
        /// Indicates a warning.
        /// </summary>
        Warning,
        /// <summary>
        /// Indicates an error.
        /// </summary>
        Error
    }

    /// <summary>
    /// Defines implementation contract for all Loggers.
    /// </summary>
    public abstract class Logger
    {
        /// <summary>
        /// Should this Logger write messages.
        /// </summary>
        public bool WriteMessages { get; set; }

        /// <summary>
        /// Should this Logger write warnings.
        /// </summary>
        public bool WriteWarnings { get; set; }

        /// <summary>
        /// Should this Logger write errors.
        /// </summary>
        public bool WriteErrors { get; set; }

        /// <summary>
        /// Write to the Logger.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="severity">The severity of the message.</param>
        public abstract void Write(string message, Severity severity);

        /// <summary>
        /// Write to the Logger ending with a new line character.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="severity">The severity of the message.</param>
        public abstract void WriteLine(string message, Severity severity);
    }
}
