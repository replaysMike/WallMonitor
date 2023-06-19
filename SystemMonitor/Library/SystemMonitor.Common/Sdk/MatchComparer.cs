using Flee.PublicTypes;

namespace SystemMonitor.Common.Sdk
{
    public static class MatchComparer
    {
        /// <summary>
        /// Compare the result value to a match type expression
        /// </summary>
        /// <param name="variableName">The name of the variable to use in the expression. Example: HttpResponseCode</param>
        /// <param name="value">The value to compare</param>
        /// <param name="matchTypeExpression">The expression to evaluate</param>
        /// <returns></returns>
        public static bool Compare(string variableName, object value, string matchTypeExpression)
        {
            var context = new ExpressionContext();
            var variables = context.Variables;
            variables.Add(variableName, value);
            var e = context.CompileGeneric<bool>(matchTypeExpression);
            var result = e.Evaluate();
            return result;
        }

        /// <summary>
        /// Compare the result value to a match type expression
        /// </summary>
        /// <param name="variableName1">The name of the variable to use in the expression. Example: HttpResponseCode</param>
        /// <param name="value1">The value to compare</param>
        /// <param name="variableName2">The name of the variable to use in the expression. Example: HttpResponseCode</param>
        /// <param name="value2">The value to compare</param>
        /// <param name="matchTypeExpression">The expression to evaluate</param>
        /// <returns></returns>
        public static bool Compare(string variableName1, object value1, string variableName2, object value2, string matchTypeExpression)
        {
            var context = new ExpressionContext();
            var variables = context.Variables;
            variables.Add(variableName1, value1);
            variables.Add(variableName2, value2);
            var e = context.CompileGeneric<bool>(matchTypeExpression);
            var result = e.Evaluate();
            return result;
        }

        /// <summary>
        /// Compare the result value to a match type expression
        /// </summary>
        /// <param name="collectionName">The name of the collection, usually 'values'</param>
        /// <param name="collection">The key value pairs to compare</param>
        /// <param name="countName"></param>
        /// <param name="countValue"></param>
        /// <param name="matchTypeExpression">The expression to evaluate</param>
        /// <returns></returns>
        public static bool Compare(string collectionName, IEnumerable<string> collection, string countName, int countValue, string matchTypeExpression)
        {
            var context = new ExpressionContext();
            var variables = context.Variables;
            variables.Add(collectionName, collection.ToArray());
            variables.Add(countName, countValue);

            var e = context.CompileGeneric<bool>(matchTypeExpression);
            var result = e.Evaluate();
            return result;
        }
    }
}
