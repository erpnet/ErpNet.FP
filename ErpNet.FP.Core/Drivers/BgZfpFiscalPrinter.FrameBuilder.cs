namespace ErpNet.FP.Core.Drivers
{
    using System.IO;
    using System.Text;

    public partial class BgZfpFiscalPrinter
    {
        /// <summary>
        /// Builds a binary protocol payload that mixes encoded text segments with raw bytes.
        /// Avoids the CP1251 round-trip problem for byte values in the U+0080–U+009F range.
        /// </summary>
        protected sealed class FrameBuilder
        {
            private readonly MemoryStream _buffer = new();
            private readonly Encoding _encoding;

            /// <summary>
            /// Creates a new builder that encodes text segments using the
            /// given encoding (typically the printer's CP1251 encoding).
            /// </summary>
            public FrameBuilder(Encoding encoding)
            {
                _encoding = encoding;
            }

            /// <summary>
            /// Appends a text segment, encoded via the configured encoding.
            /// </summary>
            public FrameBuilder Append(string text)
            {
                var bytes = _encoding.GetBytes(text);
                _buffer.Write(bytes, 0, bytes.Length);

                return this;
            }

            /// <summary>
            /// Appends a single character, encoded via the configured encoding.
            /// </summary>
            public FrameBuilder Append(char c)
            {
                var bytes = _encoding.GetBytes([c]);
                _buffer.Write(bytes, 0, bytes.Length);

                return this;
            }

            /// <summary>
            /// Appends a single raw byte, bypassing the encoding entirely.
            /// </summary>
            public FrameBuilder Append(byte b)
            {
                _buffer.WriteByte(b);

                return this;
            }

            /// <summary>
            /// Returns the accumulated payload as a new byte array.
            /// </summary>
            public byte[] ToArray() => _buffer.ToArray();
        }
    }
}
