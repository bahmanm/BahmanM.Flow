namespace BahmanM.Flow;

public interface IBehaviour<T>
{
    IFlow<T> Apply(IFlow<T> originalFlow);
}
