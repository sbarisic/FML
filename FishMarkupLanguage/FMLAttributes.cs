using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FishMarkupLanguage {
	public class FMLAttributes {
		Dictionary<string, object> Values;

		public int Count {
			get {
				return Values.Count;
			}
		}

		public FMLAttributes() {
			Values = new Dictionary<string, object>();
		}

		public void SetAttribute(string Name, object Value) {
			if (Values.ContainsKey(Name))
				Values.Remove(Name);

			Values.Add(Name, Value);
		}

		public object GetAttribute(string Name) {
			if (Values.ContainsKey(Name))
				return Values[Name];

			return null;
		}

		public T GetAttribute<T>(string Name, T Default) {
			object Attrib = GetAttribute(Name);

			if (Attrib == null)
				return Default;

			if (Attrib is T TAttrib)
				return TAttrib;

			return Default;
		}

		public KeyValuePair<string, object>[] ToArray() {
			return Values.ToArray();
		}

		public override string ToString() {
			return string.Join(" ", Values.Select(KV => string.Format("{0} = {1}", KV.Key, KV.Value)));
		}
	}
}
