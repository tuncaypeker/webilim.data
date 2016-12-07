namespace Webilim.Data
{
    /// <summary>
    /// Result of Data Operation
    /// </summary>
    public class DataResult
    {
        public DataResult(bool isSucceed, string message)
        {
            IsSucceed = isSucceed;
            Message = message;
        }

        /// <summary>
        /// Is Operation Succeed
        /// </summary>
        public bool IsSucceed { get; set; }

        /// <summary>
        /// Error Message if IsSucceed false
        /// </summary>
        public string Message { get; set; }
    }
}
