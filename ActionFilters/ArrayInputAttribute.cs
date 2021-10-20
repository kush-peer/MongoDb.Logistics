using System;
using System.ComponentModel;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MongoDb.Logistics.ActionFilters
{
	public class ArrayInputAttribute : ActionFilterAttribute
	{

		private readonly string[] arrayInputs;
		public string Separator { get; set; }

		public ArrayInputAttribute(params string[] arrayInputs)
		{
			this.arrayInputs = arrayInputs;
			Separator = ",";
		}

		public void ProcessArrayInput(ActionExecutingContext actionContext, string parameterName)
		{
			if (!actionContext.ActionArguments.ContainsKey(parameterName)) return;
			var parameterDescriptor = actionContext.ActionDescriptor.Parameters.FirstOrDefault(p => p.Name == parameterName);

			if (parameterDescriptor == null || !parameterDescriptor.ParameterType.IsArray) return;
			var type = parameterDescriptor.ParameterType.GetElementType();
			var parameters = string.Empty;
			if (actionContext.RouteData.Values.ContainsKey(parameterName))
			{
				parameters = (string)actionContext.RouteData.Values[parameterName];
			}
			else
			{
				var queryString = actionContext.HttpContext.Request.QueryString;
			}

			var values = parameters?.Split(new[] { Separator }, StringSplitOptions.RemoveEmptyEntries)
				.Select(TypeDescriptor.GetConverter(type!).ConvertFromString).ToArray();
			if (values == null) return;
			var typedValues = Array.CreateInstance(type!, values.Length);
			values.CopyTo(typedValues, 0);
			actionContext.ActionArguments[parameterName] = typedValues;
		}

		public override void OnActionExecuting(ActionExecutingContext actionContext)
		{

			foreach (var parameterName in arrayInputs)
			{
				ProcessArrayInput(actionContext, parameterName);
			}
		}

	}
}
