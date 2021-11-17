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
		ScriptState<object> State;

		public void Compile(string SrcCode, Type GlobalsType) {
			FMLScript = CSharpScript.Create<object>(SrcCode, ScriptOptions.Default, GlobalsType);
		}

		public void Run(object Globals) {
			State = FMLScript.RunAsync(Globals).Result;

			if (State.Exception != null)
				throw State.Exception;
		}

		public T GetReturnValue<T>() {
			return (T)State.ReturnValue;
		}

		public object GetReturnValue() {
			return State.ReturnValue;
		}
	}
}
