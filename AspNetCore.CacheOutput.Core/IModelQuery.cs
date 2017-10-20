namespace AspNetCore.CacheOutput.Core
{
    public interface IModelQuery<in TModel, out TResult>
    {
        TResult Execute(TModel model);
    }
}