using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Reflection;

namespace FishMarkupLanguage {
	static class Utils {
		static BindingFlags BF = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;

		public static FieldInfo[] GetFields(Type T) {
			return T.GetFields(BF);
		}

		public static bool TryCreateInstance(Type T, FMLValueTag ValTag, out object Obj) {
			if (T == typeof(int) && ValTag.Value is int) {
				Obj = ValTag.Value;
				return true;
			}

			Obj = null;
			return false;
		}
	}
}
