namespace Kinemagic.AppCore.Utils
{
    public interface IInterpolator<T>
    {
        T Interpolate(T startValue, T endValue, float t);
    }
}