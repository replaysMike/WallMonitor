namespace WallMonitor.Common.Models
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class MatchTypeVariablesAttribute : Attribute
    {
        /// <summary>
        /// List of MatchType Variables accepted
        /// </summary>
        public List<string> Variables { get; init; }

        public MatchTypeVariablesAttribute(params string[] variables)
        {
            Variables = variables.ToList();
        }
    }
}
