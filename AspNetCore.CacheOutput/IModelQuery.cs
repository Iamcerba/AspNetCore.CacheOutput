namespace AspNetCore.CacheOutput
{
    public interface IModelQuery<in TModel, out TResult>
    {
        TResult Execute(TModel model);
    }
}
