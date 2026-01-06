using System.Security.Cryptography;

namespace NubluSoft_Storage.Helpers
{
    /// <summary>
    /// Stream wrapper que calcula hash SHA256 mientras se lee
    /// Permite calcular el hash sin cargar todo el archivo en memoria
    /// </summary>
    public class HashCalculatingStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly SHA256 _sha256;
        private readonly bool _leaveOpen;
        private long _bytesRead;
        private bool _disposed;
        private byte[]? _hashValue;

        public HashCalculatingStream(Stream innerStream, bool leaveOpen = false)
        {
            _innerStream = innerStream ?? throw new ArgumentNullException(nameof(innerStream));
            _sha256 = SHA256.Create();
            _leaveOpen = leaveOpen;
            _bytesRead = 0;
        }

        /// <summary>
        /// Bytes totales leídos
        /// </summary>
        public long BytesRead => _bytesRead;

        /// <summary>
        /// Obtiene el hash calculado como string hexadecimal
        /// Solo disponible después de leer todo el stream
        /// </summary>
        public string GetHashString()
        {
            if (_hashValue == null)
            {
                _sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                _hashValue = _sha256.Hash;
            }

            return _hashValue != null
                ? BitConverter.ToString(_hashValue).Replace("-", "").ToLowerInvariant()
                : string.Empty;
        }

        /// <summary>
        /// Obtiene el hash calculado como byte array
        /// </summary>
        public byte[]? GetHashBytes()
        {
            if (_hashValue == null)
            {
                _sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                _hashValue = _sha256.Hash;
            }
            return _hashValue;
        }

        public override bool CanRead => _innerStream.CanRead;
        public override bool CanSeek => false; // No permitimos seek para mantener integridad del hash
        public override bool CanWrite => false;
        public override long Length => _innerStream.Length;

        public override long Position
        {
            get => _innerStream.Position;
            set => throw new NotSupportedException("Seek no soportado en HashCalculatingStream");
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytesRead = _innerStream.Read(buffer, offset, count);

            if (bytesRead > 0)
            {
                _sha256.TransformBlock(buffer, offset, bytesRead, null, 0);
                _bytesRead += bytesRead;
            }

            return bytesRead;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var bytesRead = await _innerStream.ReadAsync(buffer, offset, count, cancellationToken);

            if (bytesRead > 0)
            {
                _sha256.TransformBlock(buffer, offset, bytesRead, null, 0);
                _bytesRead += bytesRead;
            }

            return bytesRead;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var bytesRead = await _innerStream.ReadAsync(buffer, cancellationToken);

            if (bytesRead > 0)
            {
                _sha256.TransformBlock(buffer.Span.Slice(0, bytesRead).ToArray(), 0, bytesRead, null, 0);
                _bytesRead += bytesRead;
            }

            return bytesRead;
        }

        public override void Flush()
        {
            _innerStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("Seek no soportado en HashCalculatingStream");
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("SetLength no soportado en HashCalculatingStream");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("Write no soportado en HashCalculatingStream");
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Finalizar hash si no se ha hecho
                    if (_hashValue == null)
                    {
                        _sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                        _hashValue = _sha256.Hash;
                    }

                    _sha256.Dispose();

                    if (!_leaveOpen)
                    {
                        _innerStream.Dispose();
                    }
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }
    }
}