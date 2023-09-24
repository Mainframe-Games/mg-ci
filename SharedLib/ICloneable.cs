namespace SharedLib;

public interface ICloneable<out T>
{
	T Clone();
}