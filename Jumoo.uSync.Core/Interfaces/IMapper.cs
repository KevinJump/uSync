namespace Jumoo.uSync.Core.Interfaces
{
    public interface IMapper<in TSourceType, out TTargetType>
    {
        TTargetType Map(TSourceType item);
    }
}