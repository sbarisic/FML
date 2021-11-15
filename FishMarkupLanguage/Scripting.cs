using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace FishMarkupLanguage {
	public class Scripting {
		Script<object> FMLScript;

		public void Compile(string SrcCode, Type GlobalsType) {
			FMLScript = CSharpScript.Create<object>(SrcCode, ScriptOptions.Default, GlobalsType);
		}

		public object Run(object Globals) {
			ScriptState<object> State = FMLScript.RunAsync(Globals).Result;

			if (State.Exception != null)
				throw State.Exception;

			return State.ReturnValue;
		}
	}
}
