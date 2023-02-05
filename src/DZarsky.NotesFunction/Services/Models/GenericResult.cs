namespace DZarsky.NotesFunction.Services.Models
{
    public sealed class GenericResult<TClass> : GenericResult where TClass : class
    {
        public TClass? Result { get; set; }

        public GenericResult(ResultStatus status, TClass? result = null) : base(status)
        {
            Result = result;
            Status = status;
        }
    }

    public class GenericResult
    {
        public ResultStatus Status { get; set; }

        public GenericResult(ResultStatus status) => Status = status;
    }
}
