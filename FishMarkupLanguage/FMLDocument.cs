using System;
using System.Collections.Generic;
using System.Diagnostics;
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

		public override string ToString() {
			return string.Join(" ", Values.Select(KV => string.Format("{0} = {1}", KV.Key, KV.Value)));
		}
	}

	public class FMLTag {
		public string TagName;
		public FMLAttributes Attributes;

		public FMLTag Parent;
		public List<FMLTag> Children;

		public FMLTag() {
			Attributes = new FMLAttributes();
			Children = new List<FMLTag>();
			Parent = null;
		}

		public FMLTag(string Name) : this() {
			TagName = Name;
		}

		public void AddChild(FMLTag T) {
			if (T.Parent != null)
				T.Parent.RemoveChild(T);

			T.Parent = this;
			Children.Add(T);
		}

		public void RemoveChild(FMLTag T) {
			if (T.Parent == this)
				T.Parent = null;

			if (Children.Contains(T))
				Children.Remove(T);
		}

		public string BuildString() {
			StringBuilder SB = new StringBuilder().Append(TagName).Append(" ").AppendLine("{");

			foreach (var Child in Children)
				SB.AppendLine(Child.BuildString());


			return SB.AppendLine("}").ToString();
		}

		public override string ToString() {
			return string.Format("{0}{1} {{ {2} }}", TagName, Attributes.Count > 0 ? " " + Attributes.ToString() : "", string.Join(" ", Children));
		}
	}

	public class FMLTagSet {
		List<string> Tags;

		public FMLTagSet() {
			Tags = new List<string>();
		}

		public void AddTag(string Name) {
			Tags.Add(Name);
		}

		public void AddTags(params string[] Names) {
			foreach (var T in Names)
				AddTag(T);
		}

		public bool IsValid(string Name) {
			if (Tags.Contains(Name))
				return true;

			return false;
		}

		public string[] GetAllTags() {
			return Tags.ToArray();
		}
	}

	public class FMLDocument {
		public FMLTagSet TagSet;
		public List<FMLTag> Tags;

		public FMLDocument() {
			TagSet = new FMLTagSet();
			Tags = new List<FMLTag>();
		}

		public override string ToString() {
			return string.Join("\n", Tags);
		}
	}

	public class FMLHereDoc {
		public string Content;

		public FMLHereDoc(string Content) {
			this.Content = Content;
		}
	}
}
