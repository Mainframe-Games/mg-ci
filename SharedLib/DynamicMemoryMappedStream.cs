using System.IO.MemoryMappedFiles;

namespace SharedLib;

public class DynamicMemoryMappedStream : Stream
{
	private MemoryMappedFile memoryMappedFile;
	private MemoryMappedViewAccessor accessor;

	private long position;
	private long length;

	public DynamicMemoryMappedStream(long initialCapacity)
	{
		memoryMappedFile = MemoryMappedFile.CreateNew("DynamicMemoryMappedStreamFile", initialCapacity);
		accessor = memoryMappedFile.CreateViewAccessor();

		position = 0;
		length = initialCapacity;
	}

	public override bool CanRead => true;
	public override bool CanSeek => true;
	public override bool CanWrite => true;
	public override long Length => length;

	public override long Position
	{
		get => position;
		set => position = Math.Max(0, Math.Min(value, length));
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		if (position + count > length)
			IncreaseCapacity(position + count);

		accessor.WriteArray(position, buffer, offset, count);
		position += count;
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		int bytesRead = (int)Math.Min(count, length - position);
		accessor.ReadArray(position, buffer, offset, bytesRead);
		position += bytesRead;
		return bytesRead;
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		long newPosition = origin switch
		{
			SeekOrigin.Begin => offset,
			SeekOrigin.Current => position + offset,
			SeekOrigin.End => length + offset,
			_ => throw new ArgumentOutOfRangeException(nameof(origin)),
		};

		position = Math.Max(0, Math.Min(newPosition, length));
		return position;
	}

	public override void SetLength(long value)
	{
		throw new NotSupportedException("Setting the length of DynamicMemoryMappedStream is not supported.");
	}

	public override void Flush()
	{
		// Memory-mapped streams do not need explicit flushing.
	}

	private void IncreaseCapacity(long newCapacity)
	{
		long newLength = Math.Max(length * 2, newCapacity);
		MemoryMappedFile newMappedFile = MemoryMappedFile.CreateNew("DynamicMemoryMappedStreamFile", newLength);
		MemoryMappedViewAccessor newAccessor = newMappedFile.CreateViewAccessor();

		byte[] tempBuffer = new byte[length];
		accessor.ReadArray(0, tempBuffer, 0, (int)length);
		newAccessor.WriteArray(0, tempBuffer, 0, (int)length);

		accessor.Dispose();
		memoryMappedFile.Dispose();

		memoryMappedFile = newMappedFile;
		accessor = newAccessor;
		length = newLength;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			accessor.Dispose();
			memoryMappedFile.Dispose();
		}

		base.Dispose(disposing);
	}

	public byte[] ToArray()
	{
		long currentPosition = Position;
		Position = 0;

		byte[] buffer = new byte[Length];
		Read(buffer, 0, (int)Length);

		Position = currentPosition;
		return buffer;
	}
}