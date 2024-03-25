namespace Threadlock.Audio
{
	public interface IPoolable<T>
	{
		static abstract T Create(AudioDevice device, Format format);
	}
}
